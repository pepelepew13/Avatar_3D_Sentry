using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Avatar_3D_Sentry.Services.Storage
{
    public interface IAssetStorage
    {
        /// <summary>
        /// Sube un stream a una ruta "lógica" (por ejemplo: "logos/empresa/sede/archivo.png").
        /// </summary>
        Task<string> UploadAsync(Stream data, string blobPath, string contentType, CancellationToken ct);

        /// <summary>
        /// Devuelve una URL pública (SAS si es Azure, o ruta pública local) para leer el recurso.
        /// </summary>
        string GetPublicUrl(string path, TimeSpan? ttl = null);

        /// <summary>
        /// Lista rutas lógicas bajo un prefijo (por ejemplo: "models/empresa/sede").
        /// </summary>
        Task<IReadOnlyList<string>> ListAsync(string pathPrefix, string[]? allowedExtensions, CancellationToken ct);
    }
}
