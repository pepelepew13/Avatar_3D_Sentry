using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.StaticFiles;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/models")]
[EnableCors("DashboardCorsPolicy")]
public class ModelsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly FileExtensionContentTypeProvider _ctp = new();

    public ModelsController(IWebHostEnvironment env)
    {
        _env = env;
        _ctp.Mappings[".glb"]  = "model/gltf-binary";
        _ctp.Mappings[".gltf"] = "model/gltf+json";
    }

    [HttpGet]
    public IActionResult List()
    {
        var dir = Path.Combine(_env.ContentRootPath, "wwwroot", "models");
        var files = Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(n => n)
                .ToArray()
            : Array.Empty<string>();

        var baseUrl = $"{Request.Scheme}://{Request.Host}/models/";
        return Ok(new { baseUrl, files });
    }

    [HttpGet("{fileName}")]
    public IActionResult Get(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return BadRequest();
        fileName = Path.GetFileName(fileName);

        var path = Path.Combine(_env.ContentRootPath, "wwwroot", "models", fileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        if (!_ctp.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpOptions("{fileName}")]
    public IActionResult OptionsModel()
    {
        // ✅ Usa string plano o Append; NO arrays/colecciones.
        Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5168";
        Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        return NoContent();
    }
}
