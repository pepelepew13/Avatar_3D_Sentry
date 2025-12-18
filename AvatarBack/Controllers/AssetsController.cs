using System.ComponentModel.DataAnnotations;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetStorage _storage;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetStorage storage, ILogger<AssetsController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    // -----------------------------
    // DTOs de respuesta
    // -----------------------------
    public record AssetUploadResponse(
        string BlobPath,
        string Url,
        string? ContentType,
        long SizeBytes
    );

    // -----------------------------
    // Helpers
    // -----------------------------
    private static string Sanitize(string input)
    {
        // Limpia caracteres problemáticos en nombres
        var s = input.Trim().ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '-');
        return s.Replace(' ', '-');
    }

    private static string BuildBlobPath(string alias, string company, string site, string fileName, string? subfolder = null)
    {
        var prefix = $"{alias}/{Sanitize(company)}/{Sanitize(site)}";
        if (!string.IsNullOrWhiteSpace(subfolder))
            prefix = $"{prefix}/{subfolder}";

        return $"{prefix}/{Sanitize(fileName)}";
    }

    // =============================
    // POST /api/assets/logo/{company}/{site}
    // =============================
    [HttpPost("logo/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [RequestSizeLimit(20_000_000)] // 20MB
    public async Task<ActionResult<AssetUploadResponse>> UploadLogo(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm, Required] IFormFile file,
        CancellationToken ct)
    {
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        var blobPath = BuildBlobPath("logos", company, site, fileName, "branding");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, file.ContentType ?? "application/octet-stream", ct);

        return Ok(new AssetUploadResponse(blobPath, url, file.ContentType, file.Length));
    }

    // =============================
    // POST /api/assets/background/{company}/{site}
    // =============================
    [HttpPost("background/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [RequestSizeLimit(40_000_000)] // 40MB
    public async Task<ActionResult<AssetUploadResponse>> UploadBackground(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm, Required] IFormFile file,
        CancellationToken ct)
    {
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        var blobPath = BuildBlobPath("backgrounds", company, site, fileName, "branding");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, file.ContentType ?? "application/octet-stream", ct);

        return Ok(new AssetUploadResponse(blobPath, url, file.ContentType, file.Length));
    }

    // =============================
    // POST /api/assets/model/{company}/{site}
    // =============================
    [HttpPost("model/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [RequestSizeLimit(60_000_000)] // 60MB (GLB)
    public async Task<ActionResult<AssetUploadResponse>> UploadModel(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm, Required] IFormFile file,
        CancellationToken ct)
    {
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        // Guardamos modelos dentro de "models/{company}/{site}/"
        var blobPath = BuildBlobPath("models", company, site, fileName, "models");

        // Forzar content-type si es .glb
        var contentType = file.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            ? "model/gltf-binary"
            : file.ContentType ?? "application/octet-stream";

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, contentType, ct);

        return Ok(new AssetUploadResponse(blobPath, url, contentType, file.Length));
    }

    // =============================
    // GET /api/assets/url?path=logos/x/y/file.png&ttlSeconds=600
    // =============================
    [HttpGet("url")]
    [AllowAnonymous] // puede ser público si sólo devuelve SAS/URL de lectura
    public ActionResult<object> GetPublicUrl([FromQuery, Required] string path, [FromQuery] int? ttlSeconds = null)
    {
        var ttl = ttlSeconds.HasValue ? TimeSpan.FromSeconds(ttlSeconds.Value) : (TimeSpan?)null;
        var url = _storage.GetPublicUrl(path, ttl);
        return Ok(new { path, url });
    }
}
