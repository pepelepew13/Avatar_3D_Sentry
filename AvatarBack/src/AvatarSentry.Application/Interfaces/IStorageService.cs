namespace AvatarSentry.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string container, string blobPath, Stream data, string contentType, CancellationToken cancellationToken);
    Task<string> GetReadSasUrlAsync(string container, string blobPath, TimeSpan ttl, CancellationToken cancellationToken);
}
