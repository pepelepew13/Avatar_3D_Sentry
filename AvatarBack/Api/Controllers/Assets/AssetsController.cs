using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly IAssetStorage _storage;

    public AssetsController(IAssetStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("{empresa}/{sede}/imagen")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<AssetUploadResponse>> UploadImagen(
        [FromRoute] string empresa,
        [FromRoute] string sede,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        return await UploadAssetAsync(empresa, sede, "imagen", request, ct);
    }

    [HttpPost("{empresa}/{sede}/video")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<AssetUploadResponse>> UploadVideo(
        [FromRoute] string empresa,
        [FromRoute] string sede,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        return await UploadAssetAsync(empresa, sede, "video", request, ct);
    }

    [HttpPost("{empresa}/{sede}/modelo3d")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<AssetUploadResponse>> UploadModelo3D(
        [FromRoute] string empresa,
        [FromRoute] string sede,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        return await UploadAssetAsync(empresa, sede, "modelo3d", request, ct);
    }

    private async Task<ActionResult<AssetUploadResponse>> UploadAssetAsync(
        string empresa,
        string sede,
        string categoria,
        AssetUploadRequest request,
        CancellationToken ct)
    {
        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var safeEmpresa = SanitizeSegment(empresa);
        var safeSede = SanitizeSegment(sede);
        var safeFileName = SanitizeFileName(request.File.FileName);

        var blobPath = BuildBlobPath(safeEmpresa, safeSede, categoria, safeFileName);
        var contentType = GetContentType(request.File, categoria);

        await using var stream = request.File.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, contentType, ct);

        return Ok(new AssetUploadResponse(blobPath, url, contentType, request.File.Length));
    }

    private static string BuildBlobPath(string empresa, string sede, string categoria, string fileName)
    {
        var cat = (categoria ?? "").Trim().ToLowerInvariant();

        return cat switch
        {
            // ✅ Alineado con patrón: public/{empresa}/{sede}/branding/...
            "imagen" => $"public/{empresa}/{sede}/branding/assets/{fileName}",

            // ✅ Videos también quedan dentro de branding (por coherencia con tu módulo media)
            "video" => $"public/{empresa}/{sede}/branding/videos/{fileName}",

            // ✅ Alineado con patrón: public/{empresa}/{sede}/models/...
            "modelo3d" => $"public/{empresa}/{sede}/models/{fileName}",

            // fallback
            _ => $"public/{empresa}/{sede}/{SanitizeSegment(cat)}/{fileName}"
        };
    }

    private static string SanitizeSegment(string value)
    {
        var sanitized = (value ?? "").Trim().ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '-');
        }

        return sanitized.Replace(' ', '-');
    }

    private static string SanitizeFileName(string value)
    {
        var sanitized = (value ?? "").Trim().Replace(' ', '-');
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '-');
        }

        return sanitized;
    }

    private static string GetContentType(IFormFile file, string categoria)
    {
        if (categoria.Equals("modelo3d", StringComparison.OrdinalIgnoreCase)
            && file.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            return "model/gltf-binary";
        }

        return file.ContentType ?? "application/octet-stream";
    }
}
