using System.Collections.Generic;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("[controller]")]
public class AvatarController : ControllerBase
{
    private readonly PhraseGenerator _generator;
    private readonly ITtsService _tts;

    public AvatarController(PhraseGenerator generator, ITtsService tts)
    {
        _generator = generator;
        _tts = tts;
    }

    [HttpPost("anunciar")]
    public async Task<IActionResult> Anunciar([FromQuery] string idioma, [FromBody] SolicitudAnuncio solicitud)
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
        var tts = await _tts.SynthesizeAsync(texto, idioma);
        return Ok(new { texto, audio = tts.Audio, visemas = tts.Visemas });
    }
}
