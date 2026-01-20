using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Avatar_3D_Sentry.Services.Storage
{
    public class AzureBlobStorage : IAssetStorage
    {
        private readonly StorageOptions _opt;
        private readonly BlobServiceClient _svc;

        public AzureBlobStorage(StorageOptions opt)
        {
            _opt = opt;
            _svc = new BlobServiceClient(opt.AzureConnection);
        }

        public async Task<string> UploadAsync(Stream data, string blobPath, string contentType, CancellationToken ct)
        {
            var (containerName, objectName) = ResolvePath(blobPath);

            var container = _svc.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var blob = container.GetBlobClient(objectName);
            var headers = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType };

            await blob.UploadAsync(data, new BlobUploadOptions { HttpHeaders = headers }, ct);

            // Devuelve URL SAS de lectura
            return GetSasUrl(container, objectName, TimeSpan.FromMinutes(_opt.SasExpiryMinutes));
        }

        public string GetPublicUrl(string path, TimeSpan? ttl = null)
        {
            var (containerName, objectName) = ResolvePath(path);

            var container = _svc.GetBlobContainerClient(containerName);
            return GetSasUrl(container, objectName, ttl ?? TimeSpan.FromMinutes(_opt.SasExpiryMinutes));
        }

        public async Task<IReadOnlyList<string>> ListAsync(string pathPrefix, string[]? allowedExtensions, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(pathPrefix))
                throw new ArgumentException("pathPrefix inválido. Debe incluir el alias de contenedor.");

            var (alias, prefix, containerName) = ResolvePrefix(pathPrefix);
            var container = _svc.GetBlobContainerClient(containerName);

            var results = new List<string>();
            await foreach (var blob in container.GetBlobsAsync(prefix: string.IsNullOrWhiteSpace(prefix) ? null : prefix, cancellationToken: ct))
            {
                if (allowedExtensions is { Length: > 0 })
                {
                    var ext = Path.GetExtension(blob.Name);
                    if (string.IsNullOrWhiteSpace(ext) ||
                        !allowedExtensions.Any(x => ext.Equals(x, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

                results.Add($"{alias}/{blob.Name}");
            }

            return results.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public async Task<bool> DeleteAsync(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path inválido.");

            var (containerName, objectName) = ResolvePath(path);

            var container = _svc.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(objectName);
            var result = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
            return result.Value;
        }

        private (string container, string name) ResolvePath(string path)
        {
            var clean = path.Trim().TrimStart('/');
            var idx = clean.IndexOf('/');
            if (idx < 0)
            {
                return (MapContainer("public"), clean);
            }

            var alias = clean.Substring(0, idx);
            var remainder = clean.Substring(idx + 1);
            if (IsKnownAlias(alias))
            {
                return (MapContainer(alias), remainder);
            }

            return (MapContainer("public"), clean);
        }

        private (string alias, string prefix, string container) ResolvePrefix(string path)
        {
            var clean = path.Trim().TrimStart('/');
            var idx = clean.IndexOf('/');
            if (idx < 0)
            {
                var container = MapContainer(IsKnownAlias(clean) ? clean : "public");
                return (IsKnownAlias(clean) ? clean : "public", string.Empty, container);
            }

            var alias = clean.Substring(0, idx);
            var remainder = clean.Substring(idx + 1);
            if (IsKnownAlias(alias))
            {
                return (alias, remainder, MapContainer(alias));
            }

            return ("public", clean, MapContainer("public"));
        }

        private string MapContainer(string alias) => alias.ToLowerInvariant() switch
        {
            "models"      => _opt.Containers.Models,
            "logos"       => _opt.Containers.Logos,
            "backgrounds" => _opt.Containers.Backgrounds,
            "videos"      => _opt.Containers.Videos,
            "audio"       => _opt.Containers.Audio,
            "public"      => _opt.Containers.Logos,
            _             => alias
        };

        private static bool IsKnownAlias(string alias)
        {
            return alias.Equals("models", StringComparison.OrdinalIgnoreCase)
                   || alias.Equals("logos", StringComparison.OrdinalIgnoreCase)
                   || alias.Equals("backgrounds", StringComparison.OrdinalIgnoreCase)
                   || alias.Equals("videos", StringComparison.OrdinalIgnoreCase)
                   || alias.Equals("audio", StringComparison.OrdinalIgnoreCase)
                   || alias.Equals("public", StringComparison.OrdinalIgnoreCase);
        }

        private string GetSasUrl(BlobContainerClient container, string objectName, TimeSpan ttl)
        {
            var blob = container.GetBlobClient(objectName);

            // Si la cuenta permite generar SAS del lado del server:
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = container.Name,
                BlobName = objectName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var uri = blob.GenerateSasUri(sasBuilder);
            return uri.ToString();
        }
    }
}
