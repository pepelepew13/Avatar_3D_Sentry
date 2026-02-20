using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/sites")]
[Authorize(Roles = "Admin")]
public class SitesController : ControllerBase
{
    private readonly IInternalSiteClient _client;

    public SitesController(IInternalSiteClient client)
    {
        _client = client;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<InternalSiteDto>>> List(
        [FromQuery] int? companyId,
        [FromQuery] string? code,
        [FromQuery] string? name,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _client.GetSitesAsync(companyId, code, name, isActive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InternalSiteDto>> GetById(int id, CancellationToken ct)
    {
        var site = await _client.GetByIdAsync(id, ct);
        if (site is null) return NotFound();
        return Ok(site);
    }
}
