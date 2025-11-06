using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Services;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/notify")]
  public class NotifyController : ControllerBase {
    private readonly NotifyService _n;
    public NotifyController(NotifyService n){ _n=n; }
    [HttpPost("email"), Authorize]
    public async Task<IActionResult> Email([FromBody]MailDto dto){
      await _n.SendEmailAsync(dto.To ?? "", dto.Subject ?? "", dto.Body ?? "");
      return Ok(new{ ok=true });
    }
    public record MailDto(string? To, string? Subject, string? Body);
  }
}