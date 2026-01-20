using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AvatarSentry.Application.Models;

public class AssetUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

public class AssetUploadResponse
{
    public string BlobPath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
}
