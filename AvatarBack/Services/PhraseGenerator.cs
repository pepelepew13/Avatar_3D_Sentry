using System.Collections.Generic;
using System.Text.Json;

namespace Avatar_3D_Sentry.Services;

public class PhraseGenerator
{
    private readonly Dictionary<string, string[]> _phrases;
    private readonly Random _random = new();

    public PhraseGenerator()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "phrases.json");
        var json = File.ReadAllText(path);
        _phrases = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json) ?? new();
    }

    public string Generate(string language, IDictionary<string, string> fields)
    {
        if (!_phrases.TryGetValue(language, out var templates) || templates.Length == 0)
        {
            throw new UnsupportedLanguageException(language);
        }

        var template = templates[_random.Next(templates.Length)];
        var result = template;
        foreach (var kv in fields)
        {
            result = result.Replace($"{{{kv.Key}}}", kv.Value);
        }
        return result;
    }
}
