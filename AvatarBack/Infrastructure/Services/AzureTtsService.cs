using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Settings;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services
{
    public class AzureTtsService : ITtsService
    {
        private readonly AzureSpeechOptions _opt;
        private readonly ILogger<AzureTtsService> _logger;

        public AzureTtsService(IOptions<AzureSpeechOptions> opt, ILogger<AzureTtsService> logger)
        {
            _opt = opt.Value;
            _logger = logger;
        }

        // ============================================================
        // IMPLEMENTACIÓN DE ITtsService
        // ============================================================

        /// <summary>
        /// Implementación estándar de la interfaz. Delegamos en el método
        /// interno que ya tenías: GenerarAudioConVisemasAsync.
        /// </summary>
        public Task<TtsResultado> SynthesizeAsync(string texto, string idioma, string voz)
        {
            // La interfaz no expone CancellationToken, usamos None.
            return GenerarAudioConVisemasAsync(texto, idioma, voz, CancellationToken.None);
        }

        /// <summary>
        /// De momento devolvemos un diccionario sencillo con la voz por defecto.
        /// Si más adelante quieres, aquí podemos llamar al SDK de Azure para listar voces reales.
        /// </summary>
        public IReadOnlyDictionary<string, List<string>> GetAvailableVoices()
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (_opt.Voices.Count == 0)
            {
                var fallback = ResolveVoice(string.Empty, _opt.DefaultLanguage);
                var localeKey = ExtractLocale(fallback) ?? "es-CO";
                var langKey = localeKey.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .FirstOrDefault()
                              ?? "es";

                dict[localeKey] = new List<string> { fallback };
                dict[langKey] = new List<string> { fallback };
                return dict;
            }

            foreach (var (langKey, voiceName) in _opt.Voices)
            {
                if (string.IsNullOrWhiteSpace(voiceName))
                    continue;

                if (!dict.TryGetValue(langKey, out var voicesForLang))
                {
                    voicesForLang = new List<string>();
                    dict[langKey] = voicesForLang;
                }

                if (!voicesForLang.Contains(voiceName, StringComparer.OrdinalIgnoreCase))
                    voicesForLang.Add(voiceName);

                var localeKey = ExtractLocale(voiceName);
                if (localeKey is null)
                    continue;

                if (!dict.TryGetValue(localeKey, out var voicesForLocale))
                {
                    voicesForLocale = new List<string>();
                    dict[localeKey] = voicesForLocale;
                }

                if (!voicesForLocale.Contains(voiceName, StringComparer.OrdinalIgnoreCase))
                    voicesForLocale.Add(voiceName);
            }

            return dict;
        }

        // ============================================================
        // MÉTODO INTERNO CON CANCELLATIONTOKEN (TU IMPLEMENTACIÓN ORIGINAL)
        // ============================================================

        public async Task<TtsResultado> GenerarAudioConVisemasAsync(
            string texto,
            string idioma,
            string voz,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("El texto para TTS no puede ser vacío.", nameof(texto));

            if (string.IsNullOrWhiteSpace(_opt.SubscriptionKey))
                throw new InvalidOperationException("Falta AzureSpeech:SubscriptionKey para Azure Speech.");

            if (string.IsNullOrWhiteSpace(_opt.Region) && string.IsNullOrWhiteSpace(_opt.Endpoint))
                throw new InvalidOperationException("Configura AzureSpeech:Region o AzureSpeech:Endpoint para Azure Speech.");
            SpeechConfig config;

            if (!string.IsNullOrWhiteSpace(_opt.Endpoint))
            {
                config = SpeechConfig.FromHost(new Uri(_opt.Endpoint), _opt.SubscriptionKey);
            }
            else
            {
                config = SpeechConfig.FromSubscription(_opt.SubscriptionKey, _opt.Region);
            }

            var resolvedVoice = ResolveVoice(voz, idioma);
            config.SpeechSynthesisVoiceName = resolvedVoice;
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

            // Habilitar notificaciones de visemas (eventos)
            config.SetProperty(PropertyId.SpeechServiceResponse_RequestSentenceBoundary, "true");
            config.SetProperty(PropertyId.SpeechServiceResponse_RequestWordBoundary, "true");

            var visemas = new List<Visema>();

            using var audioStream = AudioOutputStream.CreatePullStream();
            using var audioConfig = AudioConfig.FromStreamOutput(audioStream);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);

            synthesizer.VisemeReceived += (s, e) =>
            {
                // e.VisemeId es uint → lo convertimos
                var ms = (int)(e.AudioOffset / TimeSpan.TicksPerMillisecond);
                var visemeId = (int)e.VisemeId;
                var shape = MapAzureVisemeToShapeKey(visemeId);

                visemas.Add(new Visema
                {
                    Id       = visemeId,
                    ShapeKey = shape,
                    Tiempo   = ms
                });
            };

            var ssml =
                $"<speak version='1.0' xml:lang='{idioma}'>" +
                $"<voice name='{config.SpeechSynthesisVoiceName}'>" +
                $"{System.Security.SecurityElement.Escape(texto)}</voice></speak>";

            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                string error = result.Reason.ToString();

                if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    error = $"{cancellation.Reason}: {cancellation.ErrorDetails}";
                }

                throw new InvalidOperationException($"Azure Speech no pudo sintetizar: {error}");
            }

            var audioBytes = result.AudioData;
            var durationMs = (int)result.AudioDuration.TotalMilliseconds;

            var ordered = visemas.OrderBy(v => v.Tiempo).ToList();

            return new TtsResultado
            {
                AudioBytes = audioBytes,
                DurationMs = durationMs,
                Visemes    = ordered
            };
        }

        /// <summary>
        /// Mapeo base: ajusta según tus blendshapes. Si ya tienes `VisemeMapper`, úsalo aquí.
        /// </summary>
        private static string MapAzureVisemeToShapeKey(int visemeId)
        {
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

                // Resto: boca media
                _ => "viseme_aa"
            };
        }

        private static string? ExtractLocale(string voiceName)
        {
            // Formatos típicos: es-CO-SalomeNeural, en-US-JennyNeural
            var parts = voiceName.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0]}-{parts[1]}";
            }

            return null;
        }

        private string ResolveVoice(string voz, string idioma)
        {
            if (!string.IsNullOrWhiteSpace(voz))
                return voz;

            if (!string.IsNullOrWhiteSpace(idioma) &&
                _opt.Voices.TryGetValue(idioma, out var voiceFromLanguage) &&
                !string.IsNullOrWhiteSpace(voiceFromLanguage))
            {
                return voiceFromLanguage;
            }

            if (!string.IsNullOrWhiteSpace(_opt.DefaultLanguage) &&
                _opt.Voices.TryGetValue(_opt.DefaultLanguage, out var defaultVoice) &&
                !string.IsNullOrWhiteSpace(defaultVoice))
            {
                return defaultVoice;
            }

            if (_opt.Voices.Count > 0)
            {
                var firstVoice = _opt.Voices.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                if (!string.IsNullOrWhiteSpace(firstVoice))
                    return firstVoice;
            }

            return "es-CO-SalomeNeural";
        }
    }
}
