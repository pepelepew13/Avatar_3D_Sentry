// Controllers/AvatarController.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvatarController : ControllerBase
{
    private readonly ILogger<AvatarController> _logger;
    private readonly PhraseGenerator _phrases;
    private readonly ITtsService _tts;
    private readonly IWebHostEnvironment _env;

    public AvatarController(ILogger<AvatarController> logger, PhraseGenerator phrases, ITtsService tts, IWebHostEnvironment env)
    {
        _logger = logger;
        _phrases = phrases;
        _tts = tts;
        _env = env;
    }

    /// <summary>
    /// Anuncia un turno y devuelve audio + visemas ARKit.
    /// </summary>
    [HttpPost("announce")]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnnounceAsync([FromBody] SolicitudAnuncio request, [FromQuery] string? idioma = null, [FromQuery] string? voz = null)
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

        // 3) Guardar audio en /resources/audio
        var root = Path.Combine(_env.ContentRootPath, "Resources", "audio");
        Directory.CreateDirectory(root);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp3";
        var fullPath = Path.Combine(root, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, tts.Audio);

        // 4) URL pública (sirves /resources como estático en Program.cs)
        var audioUrl = $"/resources/audio/{fileName}";

        var response = new AnnouncementResponse
        {
            Empresa  = request.Empresa,
            Sede     = request.Sede,
            Texto    = texto,
            AudioUrl = audioUrl,
            Visemas  = tts.Visemas
        };

        return Ok(response);
    }
}