using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Services;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/ecn")]
  public class EcnController : ControllerBase {
    private readonly EcnService _svc;
    public EcnController(EcnService svc){ _svc=svc; }

    [HttpGet, Authorize]
    public IActionResult List() => Ok(_svc.Query().Take(100).ToList());
  }
}