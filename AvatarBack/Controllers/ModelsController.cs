using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Linq;
using Avatar_3D_Sentry.Services.Storage;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/models")]
[EnableCors("AllowDashboard")]
public class ModelsController : ControllerBase
{
    private readonly IAssetStorage _storage;

    public ModelsController(IAssetStorage storage)
    {
        _storage = storage;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? prefix = null, [FromQuery] int? ttlSeconds = null, CancellationToken ct = default)
    {
        var allowedExtensions = new[] { ".glb", ".gltf" };
        var cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "models" : $"models/{prefix.Trim().TrimStart('/')}";
        var paths = await _storage.ListAsync(cleanPrefix, allowedExtensions, ct);
        var ttl = ttlSeconds.HasValue ? TimeSpan.FromSeconds(ttlSeconds.Value) : (TimeSpan?)null;

        var items = paths
            .Select(path => new
            {
                path,
                fileName = Path.GetFileName(path),
                url = _storage.GetPublicUrl(path, ttl)
            })
            .ToArray();

        return Ok(new { items });
    }

    [HttpGet("{*path}")]
    public IActionResult Get(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return BadRequest();
        path = path.Trim().TrimStart('/');
        path = path.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? path : $"models/{path}";

        var url = _storage.GetPublicUrl(path);
        return Redirect(url);
    }

    [HttpOptions("{*path}")]
    public IActionResult OptionsModel()
    {
        // ✅ Usa string plano o Append; NO arrays/colecciones.
        Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5168";
        Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        return NoContent();
    }
}
