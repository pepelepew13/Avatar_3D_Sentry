using System.Collections.Generic;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services;

public interface ITtsService
{
    /// <summary>
    /// Genera audio y visemas para el texto indicado utilizando la voz proporcionada.
    /// </summary>
    Task<TtsResultado> SynthesizeAsync(string texto, string idioma, string voz);

    /// <summary>
    /// Obtiene las voces neurales disponibles agrupadas por idioma.
    /// </summary>
    IReadOnlyDictionary<string, List<string>> GetAvailableVoices();
}
