using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Services;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/ai")]
  public class AiController : ControllerBase {
    private readonly AiAdvisorService _ai;
    public AiController(AiAdvisorService ai){ _ai=ai; }

    [HttpPost("ask"), Authorize]
    public async Task<IActionResult> Ask([FromBody]QDto dto){
      var ans = await _ai.AskAsync(dto.Question ?? "");
      return Ok(new { answer = ans });
    }
    public record QDto(string? Question);
  }
}