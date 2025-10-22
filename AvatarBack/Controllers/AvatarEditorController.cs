using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar")]
public class AvatarEditorController : ControllerBase
{
    private readonly AvatarContext _db;
    private readonly ILogger<AvatarEditorController> _logger;

    public AvatarEditorController(AvatarContext db, ILogger<AvatarEditorController> logger)
    {
        _db = db; _logger = logger;
    }

    // ===================== GET CONFIG (RUTA LARGA) =====================
    // GET /api/avatar/{empresa}/{sede}/config
    [HttpGet("{empresa}/{sede}/config")]
    [ProducesResponseType(typeof(AvatarConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvatarConfigDto>> GetConfigByPath(string empresa, string sede, CancellationToken ct)
    {
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

        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // ===================== SUBIR LOGO =====================
    [HttpPost("{empresa}/{sede}/logo")]
    public async Task<ActionResult<AvatarConfig>> UploadLogo(
        string empresa, string sede, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var asset = new AssetFile
        {
            Empresa = cfg.Empresa,
            Sede = cfg.Sede,
            Tipo = "logo",
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Data = ms.ToArray()
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);

        cfg.LogoPath = $"/assets/{asset.Id}";
        await _db.SaveChangesAsync(ct);
        return Ok(cfg);
    }

    // ===================== SUBIR FONDO =====================
    [HttpPost("{empresa}/{sede}/background")]
    public async Task<ActionResult<AvatarConfig>> UploadBackground(
        string empresa, string sede, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío.");
        var cfg = await GetOrCreateConfigAsync(empresa, sede, ct);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var asset = new AssetFile
        {
            Empresa = cfg.Empresa,
            Sede = cfg.Sede,
            Tipo = "fondo",
            FileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            Data = ms.ToArray()
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);

        cfg.Fondo = $"/assets/{asset.Id}";
        await _db.SaveChangesAsync(ct);
        return Ok(cfg);
    }

    // ===================== UTIL PRIVADA =====================
    private async Task<AvatarConfig> GetOrCreateConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var emp = (empresa ?? string.Empty).Trim();
        var sd = (sede ?? string.Empty).Trim();

        var cfg = await _db.AvatarConfigs
            .FirstOrDefaultAsync(a => a.NormalizedEmpresa == emp.ToLower() &&
                                      a.NormalizedSede == sd.ToLower(), ct);

        if (cfg is null)
        {
            cfg = new AvatarConfig { Empresa = emp, Sede = sd, Idioma = "es" };
            cfg.Normalize();
            _db.AvatarConfigs.Add(cfg);
            await _db.SaveChangesAsync(ct);
        }
        return cfg;
    }
}
