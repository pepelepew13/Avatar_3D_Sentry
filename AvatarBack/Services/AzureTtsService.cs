using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services
{
    public class SpeechOptions
    {
        public string? Key { get; set; }
        public string? Region { get; set; }
        public string? Endpoint { get; set; }
        public string DefaultVoice { get; set; } = "es-CO-SalomeNeural";
    }

    public class AzureTtsService : ITtsService
    {
        private readonly SpeechOptions _opt;
        private readonly ILogger<AzureTtsService> _logger;

        public AzureTtsService(IOptions<SpeechOptions> opt, ILogger<AzureTtsService> logger)
        {
            _opt = opt.Value;
            _logger = logger;
        }

        public async Task<TtsResultado> GenerarAudioConVisemasAsync(string texto, string idioma, string voz, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("El texto para TTS no puede ser vacío.", nameof(texto));

            var config = SpeechConfig.FromSubscription(_opt.Key!, _opt.Region!);
            config.SpeechSynthesisVoiceName = string.IsNullOrWhiteSpace(voz) ? _opt.DefaultVoice : voz;
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

            // Habilitar notificaciones de visemas (SSML + evento)
            config.SetProperty(PropertyId.SpeechServiceResponse_RequestSentenceBoundary, "true");
            config.SetProperty(PropertyId.SpeechServiceResponse_RequestWordBoundary, "true");

            // Recoger visemas por evento
            var visemas = new List<Visema>();

            using var audioStream = AudioOutputStream.CreatePullStream();
            using var audioConfig = AudioConfig.FromStreamOutput(audioStream);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);

            synthesizer.VisemeReceived += (s, e) =>
            {
                // e.VisemeId (int) y e.AudioOffset (ticks)
                var ms = (int)(e.AudioOffset / TimeSpan.TicksPerMillisecond);
                var shape = MapAzureVisemeToShapeKey(e.VisemeId);
                visemas.Add(new Visema { Id = e.VisemeId, ShapeKey = shape, Tiempo = ms });
            };

            // SSML simple (puedes enriquecer con <prosody>, <break>, etc.)
            var ssml = $"<speak version='1.0' xml:lang='{idioma}'>" +
                       $"<voice name='{config.SpeechSynthesisVoiceName}'>" +
                       $"{System.Security.SecurityElement.Escape(texto)}</voice></speak>";

            var result = await synthesizer.SpeakSsmlAsync(ssml);
            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
                throw new InvalidOperationException($"Azure Speech no pudo sintetizar: {result.Reason} / {result.ErrorDetails}");

            var audioBytes = result.AudioData;
            var durationMs = (int)result.AudioDuration.TotalMilliseconds;

            // Normalizamos visemas por si vienen fuera de orden
            var ordered = visemas.OrderBy(v => v.Tiempo).ToList();

            return new TtsResultado
            {
                AudioBytes = audioBytes,
                DurationMs = durationMs,
                Visemes = ordered
            };
        }

        /// <summary>
        /// Mapeo base: ajusta según tus blendshapes. Si ya tienes `VisemeMapper`, úsalo aquí.
        /// </summary>
        private static string MapAzureVisemeToShapeKey(int visemeId)
        {
            // Azure TTS reporta ~22 visemas. Aquí los agrupamos a tus keys usadas en el visor.
            // Ajusta según tu modelo (ej.: ARKit o tus "viseme_*").
            return visemeId switch
            {
                // Vocales abiertas
                0 => "viseme_aa",
                1 => "viseme_aa",
                2 => "viseme_E",
                3 => "viseme_I",
                4 => "viseme_O",
                5 => "viseme_U",

                // Consonantes frecuentes (aprox)
                6 => "viseme_SS",
                7 => "viseme_RR",
                8 => "viseme_kk",
                9 => "viseme_SS",
                10 => "viseme_RR",
                11 => "viseme_kk",

                // Resto: cae a una boca media (aa)
                _ => "viseme_aa"
            };
        }
    }
}
