using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Avatar_3D_Sentry.Services.Storage
{
    public class LocalFileStorage : IAssetStorage
    {
        private readonly IWebHostEnvironment _env;
        private readonly StorageOptions _opt;

        public LocalFileStorage(IWebHostEnvironment env, StorageOptions opt)
        {
            _env = env;
            _opt = opt;
        }

        public async Task<string> UploadAsync(Stream data, string blobPath, string contentType, CancellationToken ct)
        {
            var (alias, relative) = Split(blobPath);

            var targetDir = alias.ToLowerInvariant() switch
            {
                "models"      => _opt.Local.ModelsPath,
                "logos"       => _opt.Local.LogosPath,
                "backgrounds" => _opt.Local.BackgroundsPath,
                "audio"       => _opt.Local.AudioPath,
                _             => _opt.Local.Root
            };

            // En audio queremos que cuelgue de Resources/audio para servir con /resources
            var isAudio = alias.Equals("audio", StringComparison.OrdinalIgnoreCase);

            var root = Path.IsPathRooted(targetDir)
                ? targetDir
                : Path.Combine(_env.ContentRootPath, targetDir);

            var fullPath = Path.Combine(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            // Escribe el stream
            using (var fs = File.Create(fullPath))
                await data.CopyToAsync(fs, ct);

            // Devuelve URL pública local
            if (isAudio)
            {
                // Mapear a /resources/audio/...
                var resourcesRoot = Path.Combine(_env.ContentRootPath, "Resources");
                var rel = Path.GetRelativePath(resourcesRoot, fullPath).Replace('\\','/');
                return $"/resources/{rel}";
            }
            else if (alias.Equals("models", StringComparison.OrdinalIgnoreCase))
            {
                // /models/...
                var modelsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "models");
                var rel = Path.GetRelativePath(modelsRoot, fullPath).Replace('\\','/');
                return $"/models/{rel}";
            }
            else if (alias.Equals("logos", StringComparison.OrdinalIgnoreCase))
            {
                var logosRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "logos");
                var rel = Path.GetRelativePath(logosRoot, fullPath).Replace('\\','/');
                return $"/logos/{rel}";
            }
            else if (alias.Equals("backgrounds", StringComparison.OrdinalIgnoreCase))
            {
                var bRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "backgrounds");
                var rel = Path.GetRelativePath(bRoot, fullPath).Replace('\\','/');
                return $"/backgrounds/{rel}";
            }

            // fallback: /resources
            var resRoot = Path.Combine(_env.ContentRootPath, "Resources");
            var rel2 = Path.GetRelativePath(resRoot, fullPath).Replace('\\','/');
            return $"/resources/{rel2}";
        }

        public string GetPublicUrl(string path, TimeSpan? ttl = null)
        {
            // En local es una ruta pública ya expuesta por StaticFiles (no usamos TTL)
            if (!path.StartsWith("/"))
            {
                // Si nos piden "logos/empresa/x.png" -> lo mapeamos similar a UploadAsync
                var (alias, _) = Split(path);
                return alias.ToLowerInvariant() switch
                {
                    "audio"       => $"/resources/{TrimFirst(path)}",
                    "models"      => $"/models/{TrimFirst(path, 1)}",
                    "logos"       => $"/logos/{TrimFirst(path, 1)}",
                    "backgrounds" => $"/backgrounds/{TrimFirst(path, 1)}",
                    _             => $"/resources/{path}"
                };
            }
            return path;
        }

        private static (string alias, string relative) Split(string path)
        {
            var clean = path.Trim().TrimStart('/');
            var idx = clean.IndexOf('/');
            if (idx < 0) return (clean, "");
            return (clean.Substring(0, idx), clean.Substring(idx + 1));
        }

        private static string TrimFirst(string p, int segments = 0)
        {
            var clean = p.Trim().TrimStart('/');
            var arr = clean.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments <= 0 || segments >= arr.Length) return string.Join('/', arr);
            return string.Join('/', arr[segments..]);
        }
    }
}
