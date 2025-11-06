using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApp.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services {
  public class AuthService {
    private readonly EcnDbContext _db; private readonly IConfiguration _cfg;
    public AuthService(EcnDbContext db, IConfiguration cfg){ _db=db; _cfg=cfg; }
    public string? Login(string username, string password, out object? user){
      user=null;
      var u = _db.Users.FirstOrDefault(x=>x.Id.ToLower()==username.ToLower() || x.Email.ToLower()==username.ToLower());
      if(u==null || !BCrypt.Net.BCrypt.Verify(password, u.PasswordHash)) return null;
      user = new { u.Id, u.Name, u.Email, u.Dept, u.Role };
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? "CHANGE_ME"));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var claims = new [] {
        new Claim(ClaimTypes.NameIdentifier, u.Id),
        new Claim(ClaimTypes.Name, u.Name),
        new Claim("dept", u.Dept),
        new Claim(ClaimTypes.Role, u.Role)
      };
      var token = new JwtSecurityToken(_cfg["Jwt:Issuer"], _cfg["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
      return new JwtSecurityTokenHandler().WriteToken(token);
    }
  }
}