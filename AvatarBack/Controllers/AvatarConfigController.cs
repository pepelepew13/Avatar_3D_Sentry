using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace Avatar_3D_Sentry.Controllers;

[Authorize(Policy = "CanEditAvatar")]
[ApiController]
[Route("api/avatar")]
public class AvatarConfigController : ControllerBase
{
    private readonly AvatarContext _db;
    private readonly ILogger<AvatarConfigController> _logger;

    public AvatarConfigController(AvatarContext db, ILogger<AvatarConfigController> logger)
    {
        _db = db; _logger = logger;
    }

    // GET /api/avatar/{empresa}/{sede}
    [HttpGet("{empresa}/{sede}")]
    public async Task<ActionResult<AvatarConfigDto>> Get(string empresa, string sede, CancellationToken ct)
    {
        var cfg = await FindConfigAsync(empresa, sede, ct)
                  ?? await CreateConfigAsync(empresa, sede, ct);

        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // PUT /api/avatar/{empresa}/{sede}
    // Body: AvatarConfigUpdate
    [HttpPut("{empresa}/{sede}")]
    public async Task<ActionResult<AvatarConfigDto>> Update(
        string empresa, string sede, [FromBody] AvatarConfigUpdate update, CancellationToken ct)
    {
        var cfg = await FindConfigAsync(empresa, sede, ct)
                  ?? await CreateConfigAsync(empresa, sede, ct);

        // Normaliza entradas (valida suave; tu panel ya controla el catálogo)
        if (!string.IsNullOrWhiteSpace(update.Vestimenta))
            cfg.Vestimenta = update.Vestimenta!.Trim();

        if (!string.IsNullOrWhiteSpace(update.Fondo))
            cfg.Fondo = update.Fondo!.Trim();

        if (!string.IsNullOrWhiteSpace(update.ProveedorTts))
            cfg.ProveedorTts = update.ProveedorTts!.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(update.Voz))
            cfg.Voz = update.Voz!.Trim();

        if (!string.IsNullOrWhiteSpace(update.Idioma))
            cfg.Idioma = update.Idioma!.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(update.ColorCabello))
            cfg.ColorCabello = update.ColorCabello!.Trim().ToLowerInvariant();

        await _db.SaveChangesAsync(ct);
        return Ok(AvatarConfigDto.FromEntity(cfg));
    }

    // Helpers
    private Task<AvatarConfig?> FindConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var ne = empresa.Trim().ToLowerInvariant();
        var ns = sede.Trim().ToLowerInvariant();
        return _db.AvatarConfigs.FirstOrDefaultAsync(a =>
            a.NormalizedEmpresa == ne && a.NormalizedSede == ns, ct);
    }

    private async Task<AvatarConfig> CreateConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var cfg = new AvatarConfig
        {
            Empresa = empresa.Trim(),
            Sede    = sede.Trim(),
            Idioma  = "es",
            ProveedorTts = "polly",
            Vestimenta   = "predeterminado",
            ColorCabello = "predeterminado",
            Fondo        = "oficina"
        };
        cfg.Normalize();
        _db.AvatarConfigs.Add(cfg);
        await _db.SaveChangesAsync(ct);
        return cfg;
    }
}
