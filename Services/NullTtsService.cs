    using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services;

/// <summary>
/// Implementación de <see cref="ITtsService"/> que permite operar sin credenciales de proveedor externo.
/// Devuelve un catálogo de voces estático y genera excepciones descriptivas al intentar sintetizar audio.
/// </summary>
public class NullTtsService : ITtsService
{
    private static readonly IReadOnlyDictionary<string, List<string>> Voices = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = new() { "Lucia", "Lupe", "Mia" },
        ["pt"] = new() { "Camila" },
        ["en"] = new() { "Joanna", "Matthew" }
    };

    public IReadOnlyDictionary<string, List<string>> GetAvailableVoices() => Voices;

    public Task<TtsResultado> SynthesizeAsync(string texto, string idioma, string voz)
    {
        throw new InvalidOperationException("No hay un proveedor TTS configurado. Agrega credenciales para habilitar la síntesis de voz.");
    }
}
