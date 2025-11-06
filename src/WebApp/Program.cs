
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Data.SQLite;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var key = cfg["Jwt:Key"] ?? "CHANGE_ME_TO_A_LONG_RANDOM_SECRET________________________________________________";
var issuer = "ECNManager"; var audience = "ECNClients";
var conn = cfg.GetConnectionString("EcnDb") ?? "Data Source=ecn.db;Version=3;";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o=>{o.TokenValidationParameters=new TokenValidationParameters{
    ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,
    ValidIssuer=issuer,ValidAudience=audience,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))};});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseDefaultFiles(); app.UseStaticFiles();
app.UseAuthentication(); app.UseAuthorization();

void EnsureDb(){
  using var cs=new SQLiteConnection(conn); cs.Open();
  var cmd=cs.CreateCommand(); cmd.CommandText=@"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS Users(Id TEXT PRIMARY KEY,Name TEXT,Email TEXT,Dept TEXT,Role TEXT,PasswordHash TEXT,IsActive INT DEFAULT 1);
CREATE TABLE IF NOT EXISTS FeatureFlags(Id INTEGER PRIMARY KEY,EmailEnabled INT DEFAULT 0,DesktopToastEnabled INT DEFAULT 0,UseChatGPT INT DEFAULT 0);
CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY,Value TEXT);
CREATE TABLE IF NOT EXISTS ApiKeys(Id INTEGER PRIMARY KEY AUTOINCREMENT,Name TEXT,Key TEXT,CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP);
CREATE TABLE IF NOT EXISTS ECNMaster(Id INTEGER PRIMARY KEY AUTOINCREMENT,EcnNo TEXT,SubEcn TEXT,Model TEXT,Title TEXT,Before TEXT,After TEXT,Effective TEXT,ValidBOM TEXT,Status TEXT,Dept TEXT,CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,UpdatedAt TEXT);
CREATE TABLE IF NOT EXISTS ECNDeptTasks(Id INTEGER PRIMARY KEY AUTOINCREMENT,EcnId INTEGER,Dept TEXT,OwnerId TEXT,Status TEXT,DueDate TEXT,UpdatedAt TEXT);
CREATE TABLE IF NOT EXISTS AI_KB(Id INTEGER PRIMARY KEY AUTOINCREMENT,Kind TEXT,RefId TEXT,Content TEXT);
"; cmd.ExecuteNonQuery();
  cmd.CommandText="SELECT COUNT(*) FROM Users"; var n=System.Convert.ToInt32(cmd.ExecuteScalar());
  if(n==0){
    var ins=cs.CreateCommand();
    ins.CommandText="INSERT INTO Users(Id,Name,Email,Dept,Role,PasswordHash) VALUES"+
    "('U004','Bao Pham','bao.pham@company.com','QC','Admin',@P0),"+
    "('U001','Minh Nguyen','minh.nguyen@company.com','SMT','Editor',@P1),"+
    "('U002','Lan Tran','lan.tran@company.com','PE','Approver',@P2),"+
    "('U003','Quang Le','quang.le@company.com','FE','Viewer',@P3)";
    ins.Parameters.AddWithValue("@P0", BCrypt.Net.BCrypt.HashPassword("bao"));
    ins.Parameters.AddWithValue("@P1", BCrypt.Net.BCrypt.HashPassword("minh"));
    ins.Parameters.AddWithValue("@P2", BCrypt.Net.BCrypt.HashPassword("lan"));
    ins.Parameters.AddWithValue("@P3", BCrypt.Net.BCrypt.HashPassword("quang")); ins.ExecuteNonQuery();
    cs.CreateCommand().ExecuteNonQuery();
    var ff=cs.CreateCommand(); ff.CommandText="INSERT INTO FeatureFlags(Id,EmailEnabled,DesktopToastEnabled,UseChatGPT) VALUES(1,0,0,0)"; ff.ExecuteNonQuery();
    var e=cs.CreateCommand(); e.CommandText="INSERT INTO ECNMaster(EcnNo,SubEcn,Model,Title,Before,After,Effective,ValidBOM,Status,Dept) VALUES"+
      "('ECN-001','A','VL-V572LU-S','Change cap','ZA','ZB','2025-11-01','BOM-OK','InProgress','SMT'),"+
      "('ECN-002','B','VL-V572LU-S','Change resistor','XC','XD','2025-11-15',NULL,'Pending','PE')"; e.ExecuteNonQuery();
    var kb=cs.CreateCommand(); kb.CommandText="INSERT INTO AI_KB(Kind,RefId,Content) VALUES"+
      "('policy','howto','Dept chỉ sửa trong phạm vi Dept. Approver được duyệt. Admin quản trị users & settings.'),"+
      "('glossary','beforeafter','Before/After: linh kiện cũ/mới. ValidBOM lấy từ SAP export nếu ECN/Model/Before/After khớp.')"; kb.ExecuteNonQuery();
  }
}
EnsureDb();

string Jwt(string id,string name,string dept,string role){
  var claims=new[]{ new Claim(ClaimTypes.NameIdentifier,id), new Claim(ClaimTypes.Name,name), new Claim("dept",dept), new Claim(ClaimTypes.Role,role) };
  var sec=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)); var cred=new SigningCredentials(sec,SecurityAlgorithms.HmacSha256);
  var t=new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer,audience,claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:cred);
  return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(t);
}

app.MapPost("/auth/login",(LoginDto d)=>{
  using var cs=new SQLiteConnection(conn); cs.Open();
  var c=cs.CreateCommand(); c.CommandText="SELECT Id,Name,Email,Dept,Role,PasswordHash FROM Users WHERE lower(Id)=lower(@u) OR lower(Email)=lower(@u)";
  c.Parameters.AddWithValue("@u", d.Username??""); using var rd=c.ExecuteReader(); if(!rd.Read()) return Results.Unauthorized();
  var id=rd.GetString(0); var name=rd.GetString(1); var dept=rd.GetString(3); var role=rd.GetString(4); var hash=rd.GetString(5);
  if(!BCrypt.Net.BCrypt.Verify(d.Password??"",hash)) return Results.Unauthorized();
  var jwt=Jwt(id,name,dept,role); return Results.Ok(new { accessToken=jwt, user=new{ id,name,dept,role } });
});

app.MapGet("/api/me",[Authorize](ClaimsPrincipal u)=>{
  var id=u.FindFirstValue(ClaimTypes.NameIdentifier)??""; var name=u.FindFirstValue(ClaimTypes.Name)??"";
  var dept=u.FindFirst("dept")?.Value??""; var role=u.FindFirstValue(ClaimTypes.Role)??"";
  return Results.Ok(new { id,name,dept,role });
});

app.MapGet("/api/admin/users",[Authorize(Roles:"Admin")]()=>{
  using var cs=new SQLiteConnection(conn); cs.Open();
  var c=cs.CreateCommand(); c.CommandText="SELECT Id,Name,Email,Dept,Role,IsActive FROM Users";
  using var rd=c.ExecuteReader(); var list=new List<object>();
  while(rd.Read()) list.Add(new{ Id=rd.GetString(0), Name=rd.GetString(1), Email=rd.GetString(2), Dept=rd.GetString(3), Role=rd.GetString(4), IsActive=rd.GetInt32(5)==1 });
  return Results.Ok(list);
});

app.MapGet("/api/ecn",[Authorize](ClaimsPrincipal u,string? dept,string? status)=>{
  var role=u.FindFirstValue(ClaimTypes.Role)??""; var my=u.FindFirst("dept")?.Value??""; var q=(role=="Admin")?(dept??""):my;
  using var cs=new SQLiteConnection(conn); cs.Open();
  var c=cs.CreateCommand(); c.CommandText="SELECT Id,EcnNo,SubEcn,Model,Title,Before,After,Effective,ValidBOM,Status,Dept FROM ECNMaster WHERE 1=1"+
    (string.IsNullOrWhiteSpace(q)?"":" AND Dept=@D")+(string.IsNullOrWhiteSpace(status)?"":" AND Status=@S");
  if(!string.IsNullOrWhiteSpace(q)) c.Parameters.AddWithValue("@D",q);
  if(!string.IsNullOrWhiteSpace(status)) c.Parameters.AddWithValue("@S",status);
  using var rd=c.ExecuteReader(); var list=new List<object>();
  while(rd.Read()) list.Add(new{ Id=rd.GetInt32(0), EcnNo=rd.GetString(1), SubEcn=rd.GetString(2), Model=rd.GetString(3), Title=rd.GetString(4),
    Before=rd.GetString(5), After=rd.GetString(6), Effective=rd.IsDBNull(7)?null:rd.GetString(7), ValidBOM=rd.IsDBNull(8)?null:rd.GetString(8), Status=rd.GetString(9), Dept=rd.GetString(10)});
  return Results.Ok(list);
});

app.MapPost("/api/ai/ask",[Authorize](AiAsk q)=>{
  using var cs=new SQLiteConnection(conn); cs.Open();
  var text=(q.Question??"").Trim().ToLowerInvariant();
  var kb=cs.CreateCommand(); kb.CommandText="SELECT Kind,Content FROM AI_KB WHERE lower(Content) LIKE @q LIMIT 5"; kb.Parameters.AddWithValue("@q","%"+text+"%");
  var parts=new List<string>(); using(var rd=kb.ExecuteReader()){ while(rd.Read()) parts.Add($"[{rd.GetString(0)}] {rd.GetString(1)}"); }
  var e=cs.CreateCommand(); e.CommandText="SELECT EcnNo,Model,Before,After,ValidBOM,Status,Dept FROM ECNMaster WHERE lower(EcnNo) LIKE @q OR lower(Model) LIKE @q OR lower(Before) LIKE @q OR lower(After) LIKE @q LIMIT 10"; e.Parameters.AddWithValue("@q","%"+text+"%");
  var rows=new List<string>(); using(var rd=e.ExecuteReader()){ while(rd.Read()){ var v=rd.IsDBNull(4)?"N/A":rd.GetString(4); rows.add($"{rd.GetString(0)} • {rd.GetString(1)} • {rd.GetString(2)}->{rd.GetString(3)} • ValidBOM={v} • {rd.GetString(5)} • {rd.GetString(6)}"); } }
  var ans="ECN AI Advisor:\n"; if(parts.Count>0) ans += "- Notes:\n  - "+string.Join("\n  - ",parts)+"\n"; if(rows.Count>0) ans+="- Related:\n  - "+string.Join("\n  - ",rows)+"\n";
  if(parts.Count==0 && rows.Count==0) ans+="Không tìm thấy dữ liệu. Hãy nêu ECN/Model/Before/After cụ thể.";
  return Results.Ok(new{ answer=ans });
});

app.MapGet("/api/health",()=>Results.Ok(new{ok=true,ts=DateTime.UtcNow}));
app.Run();

record LoginDto(string? Username,string? Password);
record AiAsk(string? Question);
