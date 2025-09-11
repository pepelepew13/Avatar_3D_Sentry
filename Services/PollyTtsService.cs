using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.Extensions.Configuration;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services;

public class PollyTtsService : ITtsService
{
    private readonly AmazonPollyClient _client;
    private static readonly Dictionary<string, VoiceId> _voices = new()
    {
        ["es"] = VoiceId.Lucia,
        ["en"] = VoiceId.Joanna
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
        _client = new AmazonPollyClient(RegionEndpoint.GetBySystemName(regionName));
    }

    public async Task<TtsResultado> SynthesizeAsync(string texto, string idioma)
    {
        if (!_voices.TryGetValue(idioma, out var voice))
        {
            throw new ArgumentException($"Idioma no soportado: {idioma}", nameof(idioma));
        }

        var audioRequest = new SynthesizeSpeechRequest
        {
            Text = texto,
            VoiceId = voice,
            OutputFormat = OutputFormat.Mp3
        };
        var audioResponse = await _client.SynthesizeSpeechAsync(audioRequest);
        using var ms = new MemoryStream();
        await audioResponse.AudioStream.CopyToAsync(ms);
        var audioBytes = ms.ToArray();

        var marksRequest = new SynthesizeSpeechRequest
        {
            Text = texto,
            VoiceId = voice,
            OutputFormat = OutputFormat.Json,
            SpeechMarkTypes = new List<string> { "viseme" }
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
}
