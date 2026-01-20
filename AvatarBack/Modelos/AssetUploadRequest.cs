using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Avatar_3D_Sentry.Models;

public class AssetUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;
}
