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
    private static readonly Dictionary<string, List<string>> _availableVoices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = new() { "Lucia", "Lupe", "Mia" },
        ["pt"] = new() { "Camila" },
        ["en"] = new() { "Joanna", "Matthew" }
    };

    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _visemeToShapes
        = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["p"] = new[] { "P", "B", "M" },
            ["t"] = new[] { "L", "A" },
            ["S"] = new[] { "I", "E" },
            ["T"] = new[] { "L", "E" },
            ["k"] = new[] { "O", "U" },
            ["f"] = new[] { "F" },
            ["u"] = new[] { "U" },
            ["i"] = new[] { "I" },
            ["a"] = new[] { "A" },
            ["r"] = new[] { "A", "O" },
            ["e"] = new[] { "E" },
            ["o"] = new[] { "O" }
        };

    internal static IReadOnlyList<Visema> MapVisemeToShapes(string viseme, int time)
    {
        if (string.IsNullOrWhiteSpace(viseme))
        {
            return Array.Empty<Visema>();
        }

        if (!_visemeToShapes.TryGetValue(viseme, out var shapes) || shapes.Count == 0)
        {
            return Array.Empty<Visema>();
        }

        var list = new List<Visema>(shapes.Count);
        foreach (var shape in shapes)
        {
            if (!string.IsNullOrWhiteSpace(shape))
            {
                list.Add(new Visema { ShapeKey = shape, Tiempo = time });
            }
        }

        return list;
    }

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

        AWSCredentials credentials;
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            credentials = new BasicAWSCredentials(accessKey, secretKey);
        }
        else
        {
            credentials = new SessionAWSCredentials(accessKey, secretKey, sessionToken);
        }

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
                var mapped = MapVisemeToShapes(viseme, time);
                if (mapped.Count > 0)
                {
                    visemas.AddRange(mapped);
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
