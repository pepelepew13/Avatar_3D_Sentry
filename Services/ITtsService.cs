using System.Threading.Tasks;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Services;

public interface ITtsService
{
    Task<TtsResultado> SynthesizeAsync(string texto, string idioma);
}
