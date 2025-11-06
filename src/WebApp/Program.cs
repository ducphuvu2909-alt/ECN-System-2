using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WebApp.Services;
using WebApp.Data;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Config
var cfg = builder.Configuration;
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Jwt
var jwtKey = cfg["Jwt:Key"] ?? "CHANGE_ME_TO_A_LONG_RANDOM_SECRET";
var issuer = cfg["Jwt:Issuer"] ?? "ECNManager";
var audience = cfg["Jwt:Audience"] ?? "ECNClients";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
      ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
      ValidIssuer = issuer, ValidAudience = audience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
  });
builder.Services.AddAuthorization();

// Db (SQLite)
var connStr = cfg.GetConnectionString("EcnDb") ?? "Data Source=ecn.db;Version=3;";
builder.Services.AddDbContext<EcnDbContext>(o => o.UseSqlite(connStr));

// DI services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EcnService>();
builder.Services.AddScoped<DeptService>();
builder.Services.AddScoped<NotifyService>();
builder.Services.AddScoped<SapIngestService>();
builder.Services.AddScoped<AiAdvisorService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// init db seed
using (var scope = app.Services.CreateScope()){
  var ctx = scope.ServiceProvider.GetRequiredService<EcnDbContext>();
  ctx.Database.EnsureCreated();
  Seed.Run(ctx);
}

app.Run();