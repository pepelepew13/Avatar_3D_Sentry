using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avatar_3D_Sentry.Models;
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

    private static (string alias, string folder, string[]? allowedExtensions) ResolveAssetType(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "logos" => ("logos", "logos", new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg" }),
            "fondos" => ("backgrounds", "fondos", new[] { ".png", ".jpg", ".jpeg", ".webp" }),
            "modelos" => ("models", "modelos", new[] { ".glb", ".gltf" }),
            _ => throw new ArgumentException("Tipo inválido. Usa: logos, fondos o modelos.")
        };
    }

    // =============================
    // POST /api/assets/logo/{company}/{site}
    // =============================
    [HttpPost("logo/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)] // 20MB
    public async Task<ActionResult<AssetUploadResponse>> UploadLogo(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        if (request?.File is null) return BadRequest("Archivo requerido.");
        var file = request.File;
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        var blobPath = BuildBlobPath("logos", company, site, fileName, "logos");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, file.ContentType ?? "application/octet-stream", ct);

        return Ok(new AssetUploadResponse(blobPath, url, file.ContentType, file.Length));
    }

    // =============================
    // POST /api/assets/background/{company}/{site}
    // =============================
    [HttpPost("background/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40_000_000)] // 40MB
    public async Task<ActionResult<AssetUploadResponse>> UploadBackground(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        if (request?.File is null) return BadRequest("Archivo requerido.");
        var file = request.File;
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        var blobPath = BuildBlobPath("backgrounds", company, site, fileName, "fondos");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, file.ContentType ?? "application/octet-stream", ct);

        return Ok(new AssetUploadResponse(blobPath, url, file.ContentType, file.Length));
    }

    // =============================
    // POST /api/assets/model/{company}/{site}
    // =============================
    [HttpPost("model/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(60_000_000)] // 60MB (GLB)
    public async Task<ActionResult<AssetUploadResponse>> UploadModel(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        if (request?.File is null) return BadRequest("Archivo requerido.");
        var file = request.File;
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        // Guardamos modelos dentro de "{company}/{site}/modelos"
        var blobPath = BuildBlobPath("models", company, site, fileName, "modelos");

        // Forzar content-type si es .glb
        var contentType = file.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            ? "model/gltf-binary"
            : file.ContentType ?? "application/octet-stream";

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, contentType, ct);

        return Ok(new AssetUploadResponse(blobPath, url, contentType, file.Length));
    }

    // =============================
    // POST /api/assets/video/{company}/{site}
    // =============================
    [HttpPost("video/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)] // 200MB (video)
    public async Task<ActionResult<AssetUploadResponse>> UploadVideo(
        [FromRoute] string company,
        [FromRoute] string site,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        if (request?.File is null) return BadRequest("Archivo requerido.");
        var file = request.File;
        if (file.Length <= 0) return BadRequest("Archivo vacío.");
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Sanitize(file.FileName)}";
        var blobPath = BuildBlobPath("videos", company, site, fileName, "videos");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(stream, blobPath, file.ContentType ?? "application/octet-stream", ct);

        return Ok(new AssetUploadResponse(blobPath, url, file.ContentType, file.Length));
    }

    // =============================
    // GET /api/assets/library/{type}/{company}/{site}
    // =============================
    [HttpGet("library/{type}/{company}/{site}")]
    [Authorize(Policy = "CanEditAvatar")]
    public async Task<ActionResult<object>> GetLibrary(
        [FromRoute] string type,
        [FromRoute] string company,
        [FromRoute] string site,
        CancellationToken ct)
    {
        var (alias, folder, allowedExtensions) = ResolveAssetType(type);
        var prefix = $"{alias}/{Sanitize(company)}/{Sanitize(site)}/{folder}";

        var items = await _storage.ListAsync(prefix, allowedExtensions, ct);
        var response = items
            .Select(path => new
            {
                path,
                url = _storage.GetPublicUrl(path)
            })
            .ToArray();

        return Ok(new { type, company, site, items = response });
    }

    // =============================
    // DELETE /api/assets/{type}/{company}/{site}/{fileName}
    // =============================
    [HttpDelete("{type}/{company}/{site}/{fileName}")]
    [Authorize(Policy = "CanEditAvatar")]
    public async Task<ActionResult<object>> DeleteAsset(
        [FromRoute] string type,
        [FromRoute] string company,
        [FromRoute] string site,
        [FromRoute] string fileName,
        CancellationToken ct)
    {
        var (alias, folder, _) = ResolveAssetType(type);
        var blobPath = BuildBlobPath(alias, company, site, fileName, folder);

        var deleted = await _storage.DeleteAsync(blobPath, ct);
        if (!deleted)
            return NotFound(new { path = blobPath });

        return Ok(new { path = blobPath, deleted = true });
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
