using AvatarSentry.Application.InternalApi.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/kpis")]
[Authorize(Roles = "Admin")]
public class KpisController : ControllerBase
{
    private readonly IInternalKpisClient _client;

    public KpisController(IInternalKpisClient client)
    {
        _client = client;
    }

    [HttpGet("global")]
    public async Task<ActionResult<string>> GetGlobal(CancellationToken ct)
    {
        var data = await _client.GetGlobalAsync(ct);
        return Ok(data ?? "{}");
    }

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult<string>> GetByCompany(int companyId, CancellationToken ct)
    {
        var data = await _client.GetByCompanyAsync(companyId, ct);
        return Ok(data ?? "{}");
    }

    [HttpGet("site/{siteId:int}")]
    public async Task<ActionResult<string>> GetBySite(int siteId, CancellationToken ct)
    {
        var data = await _client.GetBySiteAsync(siteId, ct);
        return Ok(data ?? "{}");
    }
}
