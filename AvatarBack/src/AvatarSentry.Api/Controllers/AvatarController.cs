using AvatarSentry.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvatarSentry.Api.Controllers;

[ApiController]
[Route("api/avatar")]
public class AvatarController : ControllerBase
{
    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<AvatarConfigPublicDto> GetConfig([FromQuery] string empresa, [FromQuery] string sede)
    {
        return Ok(new AvatarConfigPublicDto { Empresa = empresa, Sede = sede });
    }

    [HttpPost("announce")]
    [AllowAnonymous]
    public ActionResult<AnnouncementResponse> Announce([FromBody] AnnouncementRequest request)
    {
        return Ok(new AnnouncementResponse { Empresa = request.Empresa, Sede = request.Sede });
    }
}
