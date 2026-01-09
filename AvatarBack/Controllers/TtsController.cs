using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Security;
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Avatar_3D_Sentry.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TtsController : ControllerBase
    {
        private readonly PhraseGenerator _phrases;
        private readonly ITtsService _tts;
        private readonly IAssetStorage _storage;
        private readonly ILogger<TtsController> _logger;

        public TtsController(
            PhraseGenerator phrases,
            ITtsService tts,
            IAssetStorage storage,
            ILogger<TtsController> logger)
        {
            _phrases = phrases;
            _tts = tts;
            _storage = storage;
            _logger = logger;
        }

        // DTOs de E/S
        public class AnuncioRequest
        {
            [Required] public string company { get; set; } = default!;
            [Required] public string site    { get; set; } = default!;
            [Required] public string module  { get; set; } = default!;
            [Required] public string ticket  { get; set; } = default!;
            public string? name    { get; set; }
            public string language { get; set; } = "es";
            public string? voice   { get; set; }
        }

        public record VisemeOut(string shapeKey, int tiempo);

        public record TtsResponse(string audioUrl, int durationMs, List<VisemeOut> visemes);

        // POST /api/tts/announce
        [HttpPost("announce")]
        [AllowAnonymous]
        [RequirePublicApiKey]
        public async Task<ActionResult<TtsResponse>> Announce(
            [FromBody] AnuncioRequest req,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // 1) Texto de anuncio (localizable)
            var text = _phrases.Build(
                module:   req.module,
                ticket:   req.ticket,
                name:     req.name,
                language: req.language
            );

            // 2) TTS → bytes + visemas
            var result = await _tts.SynthesizeAsync(text, req.language, req.voice ?? string.Empty);

            // 3) Subida al storage
            //    tts/{company}/{site}/{yyyy}/{MM}/{dd}/(id).mp3 (contenedor real: tts)
            var datePrefix = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var blobPath = $"audio/{req.company.ToLowerInvariant()}/{req.site.ToLowerInvariant()}/{datePrefix}/{Guid.NewGuid():N}.mp3";

            await using var ms = new MemoryStream(result.AudioBytes);
            var url = await _storage.UploadAsync(ms, blobPath, "audio/mpeg", ct);

            // 4) Salida compatible con el visor: { shapeKey, tiempo }
            var vis = result.Visemes
                .Select(v => new VisemeOut(v.ShapeKey, v.Tiempo))
                .ToList();

            return Ok(new TtsResponse(url, result.DurationMs, vis));
        }

        // (Opcional) GET /api/tts/voices
        [HttpGet("voices")]
        [AllowAnonymous]
        public ActionResult<Dictionary<string, List<string>>> GetVoices()
        {
            var voices = _tts.GetAvailableVoices();
            return Ok(new Dictionary<string, List<string>>(voices, StringComparer.OrdinalIgnoreCase));
        }
    }
}
