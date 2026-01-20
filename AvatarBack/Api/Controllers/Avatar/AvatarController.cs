using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Security;
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvatarController : ControllerBase
{
    private readonly ILogger<AvatarController> _logger;
    private readonly PhraseGenerator _phrases;
    private readonly ITtsService _tts;
    private readonly IAssetStorage _storage;

    public AvatarController(
        ILogger<AvatarController> logger,
        PhraseGenerator phrases,
        ITtsService tts,
        IAssetStorage storage)
    {
        _logger = logger;
        _phrases = phrases;
        _tts = tts;
        _storage = storage;
    }

    /// <summary>
    /// Anuncia un turno y devuelve audio + visemas.
    /// Audio se persiste en Blob (contenedor tts) y se retorna URL SAS.
    /// </summary>
    [HttpPost("announce")]
    [AllowAnonymous]
    [RequirePublicApiKey]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnnounceAsync(
        [FromBody] SolicitudAnuncio request,
        [FromQuery] string? idioma = null,
        [FromQuery] string? voz = null,
        CancellationToken ct = default)
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

        // 3) Persistir audio en Blob: tts/{empresa}/{sede}/{yyyy}/{MM}/{dd}/.../audio.mp3
        string audioUrl;
        try
        {
            var safeEmpresa = SanitizeSegment(request.Empresa);
            var safeSede = SanitizeSegment(request.Sede);
            var now = DateTime.UtcNow;

            // subcarpeta por hora para evitar sobrescritura dentro del día
            var blobPath = $"tts/{safeEmpresa}/{safeSede}/{now:yyyy}/{now:MM}/{now:dd}/{now:HHmmssfff}/audio.mp3";

            await using var ms = new MemoryStream(tts.AudioBytes, writable: false);
            audioUrl = await _storage.UploadAsync(ms, blobPath, "audio/mpeg", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo persistir el audio en Blob Storage");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "No fue posible almacenar el audio." });
        }

        var response = new AnnouncementResponse
        {
            Empresa  = request.Empresa,
            Sede     = request.Sede,
            Texto    = texto,
            AudioUrl = audioUrl,     // ✅ URL SAS
            Visemas  = tts.Visemes
        };

        return Ok(response);
    }

    private static string SanitizeSegment(string value)
    {
        var sanitized = (value ?? "").Trim().ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '-');
        }

        return sanitized.Replace(' ', '-');
    }
}
