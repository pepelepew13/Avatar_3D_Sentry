namespace Avatar_3D_Sentry.Models;

public record AssetUploadResponse(
    string BlobPath,
    string Url,
    string? ContentType,
    long SizeBytes
);
