using Microsoft.AspNetCore.Mvc;
namespace WebApp.Controllers {
  [ApiController]
  [Route("api/dept")]
  public class DeptController : ControllerBase {
    [HttpGet] public IActionResult List() => Ok(new[]{ "SMT","PE","FE","QC","DIP","COS" });
  }
}