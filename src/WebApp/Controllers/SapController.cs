using Microsoft.AspNetCore.Mvc;
namespace WebApp.Controllers {
  [ApiController]
  [Route("api/sap")]
  public class SapController : ControllerBase {
    [HttpPost("import")]
    public IActionResult Import(){ return Ok(new{ ok=true, note="stub: ingest SAP file/link 1 chiều" }); }
  }
}