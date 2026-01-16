using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Security;
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar")]
[Authorize(Policy = "CanEditAvatar")]
public class AvatarEditorController : ControllerBase
{
    private readonly IAvatarDataStore _dataStore;
    private readonly ILogger<AvatarEditorController> _logger;
    private readonly IAssetStorage _storage;
    private readonly ICompanyAccessService _companyAccess;

    public AvatarEditorController(
        IAvatarDataStore dataStore,
        ILogger<AvatarEditorController> logger,
        IAssetStorage storage,
        ICompanyAccessService companyAccess)
    {
        _dataStore = dataStore;
        _logger = logger;
        _storage = storage;
        _companyAccess = companyAccess;
    }

    // ===================== GET CONFIG (RUTA LARGA) =====================
    // GET /api/avatar/{empresa}/{sede}/config
    [HttpGet("{empresa}/{sede}/config")]
    [ProducesResponseType(typeof(AvatarConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvatarConfigDto>> GetConfigByPath(string empresa, string sede, CancellationToken ct)
    {
        if (!_companyAccess.CanAccess(User, empresa, sede))
            return Forbid();

        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== GET CONFIG (ALIAS SIMPLE) =====================
    // GET /api/config?empresa=...&sede=...
    [HttpGet("~/api/config")]
    [ProducesResponseType(typeof(AvatarConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvatarConfigDto>> GetConfig([FromQuery] string empresa, [FromQuery] string sede, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(empresa) || string.IsNullOrWhiteSpace(sede))
            return BadRequest("Debe enviar empresa y sede.");

        if (!_companyAccess.CanAccess(User, empresa, sede))
            return Forbid();

        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== SUBIR LOGO =====================
    [HttpPost("{empresa}/{sede}/logo")]
    public async Task<ActionResult<AvatarConfigDto>> UploadLogo(
        string empresa, string sede, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        if (!_companyAccess.CanAccess(User, empresa, sede))
            return Forbid();
        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);

        var blobPath = BuildBlobPath("logos", empresa, sede, file.FileName, "logos");

        await using var ms = file.OpenReadStream();
        await _storage.UploadAsync(ms, blobPath, file.ContentType ?? "application/octet-stream", ct);

        cfg.LogoPath = blobPath;
        await _dataStore.UpdateAvatarConfigAsync(cfg, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== SUBIR FONDO =====================
    [HttpPost("{empresa}/{sede}/background")]
    public async Task<ActionResult<AvatarConfigDto>> UploadBackground(
        string empresa, string sede, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        if (!_companyAccess.CanAccess(User, empresa, sede))
            return Forbid();
        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);

        var blobPath = BuildBlobPath("backgrounds", empresa, sede, file.FileName, "fondos");

        await using var ms = file.OpenReadStream();
        await _storage.UploadAsync(ms, blobPath, file.ContentType ?? "application/octet-stream", ct);

        cfg.Fondo = blobPath;
        await _dataStore.UpdateAvatarConfigAsync(cfg, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== SUBIR MODELO/VESTIMENTA =====================
    [HttpPost("{empresa}/{sede}/model")]
    public async Task<ActionResult<AvatarConfigDto>> UploadModel(
        string empresa, string sede, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        if (!_companyAccess.CanAccess(User, empresa, sede))
            return Forbid();
        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);

        var blobPath = BuildBlobPath("models", empresa, sede, file.FileName, "modelos");
        var contentType = file.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            ? "model/gltf-binary"
            : file.ContentType ?? "application/octet-stream";

        await using var ms = file.OpenReadStream();
        await _storage.UploadAsync(ms, blobPath, contentType, ct);

        cfg.Vestimenta = blobPath;
        await _dataStore.UpdateAvatarConfigAsync(cfg, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== UTIL PRIVADA =====================
    private async Task<AvatarConfig> GetOrCreateConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var emp = (empresa ?? string.Empty).Trim();
        var sd = (sede ?? string.Empty).Trim();

        var cfg = await _dataStore.FindAvatarConfigAsync(emp, sd, ct);

        if (cfg is null)
        {
            cfg = new AvatarConfig { Empresa = emp, Sede = sd, Idioma = "es" };
            cfg.Normalize();
            await _dataStore.CreateAvatarConfigAsync(cfg, ct);
        }
        return cfg;
    }

    private static string BuildBlobPath(string alias, string company, string site, string fileName, string? subfolder = null)
    {
        static string Sanitize(string input)
        {
            var s = input.Trim().ToLowerInvariant();
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '-');
            return s.Replace(' ', '-');
        }

        var prefix = $"{alias}/{Sanitize(company)}/{Sanitize(site)}";
        if (!string.IsNullOrWhiteSpace(subfolder))
            prefix = $"{prefix}/{subfolder}";

        return $"{prefix}/{Sanitize(fileName)}";
    }
}
