using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
            var (containerName, objectName) = Split(blobPath);

            // Permite mapear alias "logos|backgrounds|models|audio" a nombres reales
            containerName = MapContainer(containerName);

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
            var (containerName, objectName) = Split(path);
            containerName = MapContainer(containerName);

            var container = _svc.GetBlobContainerClient(containerName);
            return GetSasUrl(container, objectName, ttl ?? TimeSpan.FromMinutes(_opt.SasExpiryMinutes));
        }

        private static (string container, string name) Split(string path)
        {
            var clean = path.Trim().TrimStart('/');
            var idx = clean.IndexOf('/');
            if (idx < 0) throw new ArgumentException("blobPath inválido. Debe ser 'container/objeto/...'");

            var c = clean.Substring(0, idx);
            var n = clean.Substring(idx + 1);
            return (c, n);
        }

        private string MapContainer(string alias) => alias.ToLowerInvariant() switch
        {
            "models"      => _opt.Containers.Models,
            "logos"       => _opt.Containers.Logos,
            "backgrounds" => _opt.Containers.Backgrounds,
            "audio"       => _opt.Containers.Audio,
            _             => alias
        };

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
