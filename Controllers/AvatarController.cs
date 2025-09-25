using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Runtime;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;
using Amazon.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("[controller]")]
public class AvatarController : ControllerBase
{
    private readonly PhraseGenerator _generator;
    private readonly ITtsService _tts;
    private readonly AvatarContext _context;

    public AvatarController(PhraseGenerator generator, ITtsService tts, AvatarContext context)
    {
        _generator = generator;
        _tts = tts;
        _context = context;
    }

    [HttpPost("anunciar")]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementResponse>> Anunciar([FromQuery] string? idioma, [FromBody] SolicitudAnuncio solicitud)
    {
        var campos = new Dictionary<string, string>
        {
            ["empresa"] = solicitud.Empresa,
            ["sede"] = solicitud.Sede,
            ["modulo"] = solicitud.Modulo,
            ["turno"] = solicitud.Turno,
            ["nombre"] = solicitud.Nombre
        };

        var config = await _context.AvatarConfigs
            .FirstOrDefaultAsync(c => c.Empresa == solicitud.Empresa && c.Sede == solicitud.Sede);

        var idiomaSeleccionado = string.IsNullOrWhiteSpace(idioma)
            ? (config?.Idioma ?? "es")
            : idioma;

        string texto;
        try
        {
            texto = _generator.Generate(idiomaSeleccionado, campos);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var availableVoices = _tts.GetAvailableVoices();
        availableVoices.TryGetValue(idiomaSeleccionado, out var vocesIdioma);
        var voice = config?.Voz ?? vocesIdioma?.FirstOrDefault();
        if (voice is null)
        {
            return BadRequest($"No hay voz disponible para el idioma {idiomaSeleccionado}.");
        }

        TtsResultado tts;
        try
        {
            tts = await _tts.SynthesizeAsync(texto, idiomaSeleccionado, voice);
        }
        catch (AmazonServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                $"El proveedor TTS no está disponible: {ex.Message}");
        }
        catch (AmazonClientException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                $"No fue posible comunicarse con el servicio TTS: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }

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
