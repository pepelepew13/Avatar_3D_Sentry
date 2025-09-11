using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("[controller]")]
public class AvatarController : ControllerBase
{
    private readonly PhraseGenerator _generator;

    public AvatarController(PhraseGenerator generator)
    {
        _generator = generator;
    }

    [HttpPost("anunciar")]
    public IActionResult Anunciar([FromQuery] string idioma, [FromBody] SolicitudAnuncio solicitud)
    {
        var campos = new Dictionary<string, string>
        {
            ["empresa"] = solicitud.Empresa,
            ["sede"] = solicitud.Sede,
            ["modulo"] = solicitud.Modulo,
            ["turno"] = solicitud.Turno,
            ["nombre"] = solicitud.Nombre
        };

        var texto = _generator.Generate(idioma, campos);
        return Ok(new { texto });
    }
}
