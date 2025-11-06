using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers {
  [ApiController]
  [Route("auth")]
  public class AuthController : ControllerBase {
    private readonly AuthService _auth;
    public AuthController(AuthService auth){ _auth=auth; }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto){
      var token = _auth.Login(dto.Username ?? "", dto.Password ?? "", out var user);
      if(string.IsNullOrEmpty(token)) return Unauthorized();
      return Ok(new { accessToken = token, user });
    }
  }
  public record LoginDto(string? Username, string? Password);
}