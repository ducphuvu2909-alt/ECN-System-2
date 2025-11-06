
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Data.SQLite;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;
var jwtKey = cfg["Jwt:Key"] ?? "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET______________________________________________";
var issuer = cfg["Jwt:Issuer"] ?? "ECNManager";
var audience = cfg["Jwt:Audience"] ?? "ECNClients";
var connStr = cfg.GetConnectionString("EcnDb") ?? "Data Source=ecn.db;Version=3;";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
      ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
      ValidIssuer = issuer, ValidAudience = audience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
  });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

void EnsureDb(){
  using var cs = new SQLiteConnection(connStr);
  cs.Open();
  var cmd = cs.CreateCommand();
  cmd.CommandText = @"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS Users(
  Id TEXT PRIMARY KEY, Name TEXT, Email TEXT, Dept TEXT, Role TEXT, PasswordHash TEXT, IsActive INTEGER DEFAULT 1
);
CREATE TABLE IF NOT EXISTS FeatureFlags(
  Id INTEGER PRIMARY KEY, EmailEnabled INTEGER DEFAULT 0, DesktopToastEnabled INTEGER DEFAULT 0, UseChatGPT INTEGER DEFAULT 0
);
CREATE TABLE IF NOT EXISTS Settings(
  Key TEXT PRIMARY KEY, Value TEXT
);
CREATE TABLE IF NOT EXISTS ApiKeys(
  Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Key TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS ECNMaster(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EcnNo TEXT, SubEcn TEXT, Model TEXT, Title TEXT, Before TEXT, After TEXT,
  Effective TEXT, ValidBOM TEXT, Status TEXT, Dept TEXT,
  CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT
);
CREATE TABLE IF NOT EXISTS ECNDeptTasks(
  Id INTEGER PRIMARY KEY AUTOINCREMENT, EcnId INTEGER, Dept TEXT, OwnerId TEXT, Status TEXT, DueDate TEXT, UpdatedAt TEXT
);
CREATE TABLE IF NOT EXISTS AI_KB(
  Id INTEGER PRIMARY KEY AUTOINCREMENT, Kind TEXT, RefId TEXT, Content TEXT
);
";
  cmd.ExecuteNonQuery();
  // seed users + flags + demo ECN
  cmd.CommandText = "SELECT COUNT(*) FROM Users";
  var cnt = Convert.ToInt32(cmd.ExecuteScalar());
  if(cnt==0){
    var ins = cs.CreateCommand();
    ins.CommandText = "INSERT INTO Users(Id,Name,Email,Dept,Role,PasswordHash) VALUES" +
      "('U004','Bao Pham','bao.pham@company.com','QC','Admin',@P0)," +
      "('U001','Minh Nguyen','minh.nguyen@company.com','SMT','Editor',@P1)," +
      "('U002','Lan Tran','lan.tran@company.com','PE','Approver',@P2)," +
      "('U003','Quang Le','quang.le@company.com','FE','Viewer',@P3)";
    ins.Parameters.AddWithValue("@P0", BCrypt.Net.BCrypt.HashPassword("bao"));
    ins.Parameters.AddWithValue("@P1", BCrypt.Net.BCrypt.HashPassword("minh"));
    ins.Parameters.AddWithValue("@P2", BCrypt.Net.BCrypt.HashPassword("lan"));
    ins.Parameters.AddWithValue("@P3", BCrypt.Net.BCrypt.HashPassword("quang"));
    ins.ExecuteNonQuery();

    var ff = cs.CreateCommand();
    ff.CommandText = "INSERT INTO FeatureFlags(Id,EmailEnabled,DesktopToastEnabled,UseChatGPT) VALUES(1,0,0,0)";
    ff.ExecuteNonQuery();

    var ecn = cs.CreateCommand();
    ecn.CommandText = "INSERT INTO ECNMaster(EcnNo,SubEcn,Model,Title,Before,After,Effective,ValidBOM,Status,Dept) VALUES" +
      "('ECN-001','A','VL-V572LU-S','Change cap','ZA','ZB','2025-11-01','BOM-OK','InProgress','SMT')," +
      "('ECN-002','B','VL-V572LU-S','Change resistor','XC','XD','2025-11-15',NULL,'Pending','PE')";
    ecn.ExecuteNonQuery();

    var kb = cs.CreateCommand();
    kb.CommandText = "INSERT INTO AI_KB(Kind,RefId,Content) VALUES" +
      "('policy','howto','ECN Manager policy: Dept users can update tasks only within own Dept. Approver can approve. Admin controls users and settings.')," +
      "('glossary','beforeafter','Before/After denote old/new parts. ValidBOM is taken from SAP export if ECN/Model/Before/After match.')";
    kb.ExecuteNonQuery();
  }
}
EnsureDb();

string Jwt(string id,string name,string dept,string role){
  var claims = new[]{
    new Claim(ClaimTypes.NameIdentifier,id), new Claim(ClaimTypes.Name,name),
    new Claim("dept",dept), new Claim(ClaimTypes.Role,role)
  };
  var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
  var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
  var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer,audience,claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:creds);
  return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

// Auth
app.MapPost("/auth/login", (LoginDto dto) => {
  using var cs = new SQLiteConnection(connStr);
  cs.Open();
  var c = cs.CreateCommand();
  c.CommandText = "SELECT Id,Name,Email,Dept,Role,PasswordHash FROM Users WHERE lower(Id)=lower(@U) OR lower(Email)=lower(@U)";
  c.Parameters.AddWithValue("@U", dto.Username ?? "");
  using var rd = c.ExecuteReader();
  if(!rd.Read()) return Results.Unauthorized();
  var id = rd.GetString(0);
  var name = rd.GetString(1);
  var email = rd.GetString(2);
  var dept = rd.GetString(3);
  var role = rd.GetString(4);
  var hash = rd.GetString(5);
  if(!BCrypt.Net.BCrypt.Verify(dto.Password ?? "", hash)) return Results.Unauthorized();
  var jwt = Jwt(id,name,dept,role);
  return Results.Ok(new { accessToken = jwt, user = new { id, name, email, dept, role } });
});

app.MapGet("/api/me", [Authorize] (ClaimsPrincipal u) => {
  var id = u.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
  var name = u.FindFirstValue(ClaimTypes.Name) ?? "";
  var dept = u.FindFirst("dept")?.Value ?? "";
  var role = u.FindFirstValue(ClaimTypes.Role) ?? "";
  return Results.Ok(new { id,name,dept,role });
});

// Admin: users (FIX: Roles = "Admin")
app.MapGet("/api/admin/users", [Authorize(Roles = "Admin")] () => {
  using var cs = new SQLiteConnection(connStr); cs.Open();
  var cmd = cs.CreateCommand(); cmd.CommandText = "SELECT Id,Name,Email,Dept,Role,IsActive FROM Users";
  using var rd = cmd.ExecuteReader();
  var list = new List<object>();
  while(rd.Read()) list.Add(new {
    Id=rd.GetString(0), Name=rd.GetString(1), Email=rd.GetString(2), Dept=rd.GetString(3),
    Role=rd.GetString(4), IsActive = rd.GetInt32(5)==1
  });
  return Results.Ok(list);
});

// ECN query (dept-aware)
app.MapGet("/api/ecn", [Authorize] (ClaimsPrincipal user, string? dept, string? status) => {
  var role = user.FindFirstValue(ClaimTypes.Role) ?? "";
  var myDept = user.FindFirst("dept")?.Value ?? "";
  var qDept = (role=="Admin") ? (dept ?? "") : myDept;
  using var cs = new SQLiteConnection(connStr); cs.Open();
  var cmd = cs.CreateCommand();
  cmd.CommandText = "SELECT Id,EcnNo,SubEcn,Model,Title,Before,After,Effective,ValidBOM,Status,Dept FROM ECNMaster WHERE 1=1" +
    (string.IsNullOrWhiteSpace(qDept) ? "" : " AND Dept=@D") +
    (string.IsNullOrWhiteSpace(status) ? "" : " AND Status=@S");
  if(!string.IsNullOrWhiteSpace(qDept)) cmd.Parameters.AddWithValue("@D", qDept);
  if(!string.IsNullOrWhiteSpace(status)) cmd.Parameters.AddWithValue("@S", status);
  using var rd = cmd.ExecuteReader();
  var list = new List<object>();
  while(rd.Read()) list.Add(new {
    Id=rd.GetInt32(0), EcnNo=rd.GetString(1), SubEcn=rd.GetString(2), Model=rd.GetString(3),
    Title=rd.GetString(4), Before=rd.GetString(5), After=rd.GetString(6),
    Effective=rd.IsDBNull(7)?null:rd.GetString(7), ValidBOM=rd.IsDBNull(8)?null:rd.GetString(8),
    Status=rd.GetString(9), Dept=rd.GetString(10)
  });
  return Results.Ok(list);
});

// AI Advisor (simple RAG over AI_KB + ECNMaster) (FIX: rows.Add)
app.MapPost("/api/ai/ask", [Authorize] async (ClaimsPrincipal user, AiAsk dto) => {
  using var cs = new SQLiteConnection(connStr); cs.Open();
  string q = (dto.Question ?? "").Trim().ToLowerInvariant();
  // search KB
  var kbCmd = cs.CreateCommand();
  kbCmd.CommandText = "SELECT Kind, Content FROM AI_KB WHERE lower(Content) LIKE @Q LIMIT 5";
  kbCmd.Parameters.AddWithValue("@Q", "%"+q+"%");
  var kbList = new List<string>();
  using(var rd = kbCmd.ExecuteReader()){
    while(rd.Read()) kbList.Add($"[{rd.GetString(0)}] {rd.GetString(1)}");
  }
  // search ECN
  var ecnCmd = cs.CreateCommand();
  ecnCmd.CommandText = @"SELECT EcnNo, Model, Before, After, ValidBOM, Status, Dept
                         FROM ECNMaster
                         WHERE lower(EcnNo) LIKE @Q OR lower(Model) LIKE @Q OR lower(Before) LIKE @Q OR lower(After) LIKE @Q
                         LIMIT 10";
  ecnCmd.Parameters.AddWithValue("@Q", "%"+q+"%");
  var ecnList = new List<string>();
  using(var rd = ecnCmd.ExecuteReader()){
    while(rd.Read()){
      var ecnNo = rd.GetString(0);
      var model = rd.GetString(1);
      var before = rd.GetString(2);
      var after = rd.GetString(3);
      var v = rd.IsDBNull(4)? "N/A" : rd.GetString(4);
      var st = rd.GetString(5);
      var d = rd.GetString(6);
      ecnList.Add($"{ecnNo} • {model} • {before}->{after} • ValidBOM={v} • {st} • {d}");
    }
  }
  var answer = "ECN AI Advisor:\n";
  if(kbList.Count>0){ answer += "- Policies/Notes:\n  - " + string.Join("\n  - ", kbList) + "\n"; }
  if(ecnList.Count>0){ answer += "- Related ECNs:\n  - " + string.Join("\n  - ", ecnList) + "\n"; }
  if(kbList.Count==0 && ecnList.Count==0){ answer += "Không tìm thấy dữ liệu khớp. Hãy hỏi cụ thể ECN No/Model/Before/After."; }
  return Results.Ok(new { answer });
});

app.MapGet("/api/health", () => Results.Ok(new { ok=true, ts=DateTime.UtcNow }));

app.Run();

record LoginDto(string? Username, string? Password);
record AiAsk(string? Question);
