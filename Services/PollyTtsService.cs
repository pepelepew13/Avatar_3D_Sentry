using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services;

public class PollyTtsService : ITtsService
{
    private readonly AmazonPollyClient _client;
    private static readonly Dictionary<string, List<string>> _availableVoices = new()
    {
        ["es"] = new() { "Lucia", "Lupe", "Mia" },
        ["pt"] = new() { "Camila" },
        ["en"] = new() { "Joanna", "Matthew" }
    };

    private static readonly Dictionary<string, string> _visemeToShape = new()
    {
        ["p"] = "viseme_PP",
        ["t"] = "viseme_T",
        ["S"] = "viseme_SS",
        ["T"] = "viseme_TH",
        ["k"] = "viseme_KK",
        ["f"] = "viseme_FF",
        ["u"] = "viseme_U",
        ["i"] = "viseme_I",
        ["a"] = "viseme_AA",
        ["r"] = "viseme_RR",
        ["e"] = "viseme_E",
        ["o"] = "viseme_O"
    };

    public PollyTtsService(IConfiguration configuration)
    {
        var regionName = configuration["AWS:Region"] ?? RegionEndpoint.USEast1.SystemName;
        var accessKey = configuration["AWS:AccessKeyId"];
        var secretKey = configuration["AWS:SecretAccessKey"];
        var sessionToken = configuration["AWS:SessionToken"];

        if (string.IsNullOrWhiteSpace(accessKey))
        {
            accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        }

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Faltan credenciales de AWS Polly. Define AWS:AccessKeyId y AWS:SecretAccessKey en la configuración o utiliza las variables de entorno AWS_ACCESS_KEY_ID y AWS_SECRET_ACCESS_KEY.");
        }

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
        }

        var credentials = string.IsNullOrWhiteSpace(sessionToken)
            ? new BasicAWSCredentials(accessKey, secretKey)
            : new SessionAWSCredentials(accessKey, secretKey, sessionToken);

        _client = new AmazonPollyClient(credentials, RegionEndpoint.GetBySystemName(regionName));
    }

    public async Task<TtsResultado> SynthesizeAsync(string texto, string idioma, string voz)
    {
        if (!_availableVoices.TryGetValue(idioma, out var voices) || !voices.Contains(voz))
        {
            throw new ArgumentException($"Voz no soportada: {voz} para idioma {idioma}", nameof(voz));
        }

        var voiceId = VoiceId.FindValue(voz);

        var audioRequest = new SynthesizeSpeechRequest
        {
            Text = texto,
            VoiceId = voiceId,
            OutputFormat = OutputFormat.Mp3,
            Engine = Engine.Neural
        };
        var audioResponse = await _client.SynthesizeSpeechAsync(audioRequest);
        using var ms = new MemoryStream();
        await audioResponse.AudioStream.CopyToAsync(ms);
        var audioBytes = ms.ToArray();

        var marksRequest = new SynthesizeSpeechRequest
        {
            Text = texto,
            VoiceId = voiceId,
            OutputFormat = OutputFormat.Json,
            SpeechMarkTypes = new List<string> { "viseme" },
            Engine = Engine.Neural
        };
        var marksResponse = await _client.SynthesizeSpeechAsync(marksRequest);
        var visemas = new List<Visema>();
        using var reader = new StreamReader(marksResponse.AudioStream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("value", out var valueEl) && root.TryGetProperty("time", out var timeEl))
            {
                var viseme = valueEl.GetString() ?? string.Empty;
                var time = timeEl.GetInt32();
                if (_visemeToShape.TryGetValue(viseme, out var shape))
                {
                    visemas.Add(new Visema { ShapeKey = shape, Tiempo = time });
                }
            }
        }

        return new TtsResultado
        {
            Audio = audioBytes,
            Visemas = visemas
        };
    }

    public IReadOnlyDictionary<string, List<string>> GetAvailableVoices() => _availableVoices;
}
