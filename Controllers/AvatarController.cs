using System;
using System.Collections.Generic;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;
using Microsoft.AspNetCore.Http;
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
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementResponse>> Anunciar([FromQuery] string idioma, [FromBody] SolicitudAnuncio solicitud)
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
        var audioUrl = $"data:audio/mpeg;base64,{Convert.ToBase64String(tts.Audio)}";

        var response = new AnnouncementResponse
        {
            Empresa = solicitud.Empresa,
            Sede = solicitud.Sede,
            Texto = texto,
            AudioUrl = audioUrl,
            Visemas = tts.Visemas
        };

        return Ok(response);
    }
}
