using AvatarSentry.Application.Interfaces;

namespace AvatarSentry.Infrastructure;

public class StorageService : IStorageService
{
    public Task<string> UploadAsync(string container, string blobPath, Stream data, string contentType, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }

    public Task<string> GetReadSasUrlAsync(string container, string blobPath, TimeSpan ttl, CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }
}
