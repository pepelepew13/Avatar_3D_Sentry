using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Security;
using Avatar_3D_Sentry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvatarController : ControllerBase
{
    private readonly ILogger<AvatarController> _logger;
    private readonly PhraseGenerator _phrases;
    private readonly ITtsService _tts;

    public AvatarController(
        ILogger<AvatarController> logger,
        PhraseGenerator phrases,
        ITtsService tts)
    {
        _logger = logger;
        _phrases = phrases;
        _tts = tts;
    }

    /// <summary>
    /// Anuncia un turno y devuelve audio + visemas ARKit.
    /// </summary>
    [HttpPost("announce")]
    [AllowAnonymous]
    [RequirePublicApiKey]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnnounceAsync(
        [FromBody] SolicitudAnuncio request,
        [FromQuery] string? idioma = null,
        [FromQuery] string? voz = null)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // 1) Texto de anuncio
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["empresa"] = request.Empresa,
            ["sede"]    = request.Sede,
            ["modulo"]  = request.Modulo,
            ["turno"]   = request.Turno,
            ["nombre"]  = request.Nombre
        };

        var lang = string.IsNullOrWhiteSpace(idioma) ? "es" : idioma;
        string texto;
        try
        {
            texto = _phrases.Generate(lang, fields);
        }
        catch (UnsupportedLanguageException)
        {
            texto = $"Turno {request.Turno}, por favor dirígete al módulo {request.Modulo}.";
        }

        // 2) TTS (audio + visemas)
        var voice = string.IsNullOrWhiteSpace(voz)
            ? (_tts.GetAvailableVoices().TryGetValue(lang, out var voices) && voices.Count > 0 ? voices[0] : null)
            : voz;

        if (voice is null)
            return BadRequest(new { error = $"No hay voces disponibles para el idioma '{lang}'." });

        TtsResultado tts;
        try
        {
            tts = await _tts.SynthesizeAsync(texto, lang, voice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en TTS");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "No fue posible generar la locución." });
        }

        // 3) Armar data URL (no persistir audio por ahora)
        var audioBase64 = Convert.ToBase64String(tts.AudioBytes);
        var audioUrl = $"data:audio/mpeg;base64,{audioBase64}";

        var response = new AnnouncementResponse
        {
            Empresa  = request.Empresa,
            Sede     = request.Sede,
            Texto    = texto,
            AudioUrl = audioUrl,
            Visemas  = tts.Visemes  // modelo de salida ya es List<Visema>
        };

        return Ok(response);
    }
}
