using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Avatar_3D_Sentry.Controllers;

/// <summary>
/// Permite configurar la apariencia del avatar con logo, vestimenta y fondo.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AvatarEditorController : ControllerBase
{
    private readonly AvatarContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly long _fileSizeLimit = 2 * 1024 * 1024; // 2MB
    private readonly string[] _permittedExtensions = [".jpg", ".jpeg", ".png"];

    public AvatarEditorController(AvatarContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Obtiene la configuración del avatar para una empresa y sede.
    /// </summary>
    [HttpGet("{empresa}/{sede}")]
    public async Task<ActionResult<AvatarConfig>> GetConfig(string empresa, string sede)
    {
        var config = await _context.AvatarConfigs
            .FirstOrDefaultAsync(c => c.Empresa == empresa && c.Sede == sede);

        return config is null ? NotFound() : Ok(config);
    }

    /// <summary>
    /// Sube un logo y lo asigna al avatar.
    /// </summary>
    [HttpPost("{empresa}/{sede}/logo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult> UploadLogo(string empresa, string sede, IFormFile logo)
    {
        if (logo is null || logo.Length == 0)
        {
            return BadRequest("Archivo vacío.");
        }

        if (logo.Length > _fileSizeLimit)
        {
            return BadRequest("El archivo excede el tamaño permitido.");
        }

        var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
        if (!_permittedExtensions.Contains(ext))
        {
            return BadRequest("Formato de imagen no válido.");
        }

        var logosPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "logos");
        Directory.CreateDirectory(logosPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(logosPath, fileName);
        using (var stream = System.IO.File.Create(filePath))
        {
            await logo.CopyToAsync(stream);
        }

        var config = await _context.AvatarConfigs
            .FirstOrDefaultAsync(c => c.Empresa == empresa && c.Sede == sede);
        if (config is null)
        {
            config = new AvatarConfig { Empresa = empresa, Sede = sede };
            _context.AvatarConfigs.Add(config);
        }

        config.LogoPath = $"/logos/{fileName}";
        await _context.SaveChangesAsync();

        return Ok(new { config.LogoPath });
    }

    /// <summary>
    /// Actualiza vestimenta y fondo del avatar.
    /// </summary>
    [HttpPost("{empresa}/{sede}")]
    public async Task<ActionResult> UpdateConfig(string empresa, string sede, [FromBody] UpdateRequest request)
    {
        var config = await _context.AvatarConfigs
            .FirstOrDefaultAsync(c => c.Empresa == empresa && c.Sede == sede);

        if (config is null)
        {
            config = new AvatarConfig { Empresa = empresa, Sede = sede };
            _context.AvatarConfigs.Add(config);
        }

        config.Vestimenta = request.Vestimenta;
        config.Fondo = request.Fondo;

        await _context.SaveChangesAsync();

        return Ok(config);
    }

    /// <summary>
    /// Petición de actualización de configuración visual.
    /// </summary>
    public class UpdateRequest
    {
        public string? Vestimenta { get; set; }
        public string? Fondo { get; set; }
    }
}

