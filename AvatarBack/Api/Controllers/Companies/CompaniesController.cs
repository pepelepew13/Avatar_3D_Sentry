using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Roles = "Admin")]
public class CompaniesController : ControllerBase
{
    private readonly IInternalCompanyClient _client;

    public CompaniesController(IInternalCompanyClient client)
    {
        _client = client;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<InternalCompanyDto>>> List(
        [FromQuery] string? code,
        [FromQuery] string? name,
        [FromQuery] string? sector,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _client.GetCompaniesAsync(code, name, sector, isActive, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InternalCompanyDto>> GetById(int id, CancellationToken ct)
    {
        var company = await _client.GetByIdAsync(id, ct);
        if (company is null) return NotFound();
        return Ok(company);
    }
}
