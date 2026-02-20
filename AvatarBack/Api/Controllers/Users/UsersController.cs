using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using AvatarSentry.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IInternalUserClient _internalUserClient;
    private readonly ICompanySiteResolutionService _resolution;

    public UsersController(IInternalUserClient internalUserClient, ICompanySiteResolutionService resolution)
    {
        _internalUserClient = internalUserClient;
        _resolution = resolution;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
        [FromQuery] string? q,
        [FromQuery] string? role,
        [FromQuery] string? empresa,
        [FromQuery] string? sede,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        if (!isGlobalAdmin)
        {
            if (string.IsNullOrWhiteSpace(scopeEmpresa) || string.IsNullOrWhiteSpace(scopeSede))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(empresa) &&
                !string.Equals(empresa, scopeEmpresa, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(sede) &&
                !string.Equals(sede, scopeSede, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        int? companyId = null, siteId = null;
        var emp = isGlobalAdmin ? empresa : scopeEmpresa;
        var sed = isGlobalAdmin ? sede : scopeSede;
        if (!string.IsNullOrWhiteSpace(emp) || !string.IsNullOrWhiteSpace(sed))
        {
            var ids = await _resolution.ResolveToIdsAsync(emp, sed, ct);
            if (ids.HasValue) { companyId = ids.Value.CompanyId; siteId = ids.Value.SiteId; }
        }

        var filter = new UserFilter
        {
            Company = companyId,
            Site = siteId,
            Email = q,
            Role = role,
            Page = page,
            PageSize = pageSize
        };

        var result = await _internalUserClient.GetUsersAsync(filter, ct);
        var response = new PagedResponse<UserDto>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            Items = result.Items.Select(MapToUserDto).ToList()
        };

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
    {
        var user = await _internalUserClient.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }

        if (!CanAccess(user.CompanyName, user.SiteName))
        {
            return Forbid();
        }

        return Ok(MapToUserDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (!CanAccess(request.Empresa, request.Sede))
        {
            return Forbid();
        }

        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        var emp = isGlobalAdmin ? request.Empresa : scopeEmpresa;
        var sed = isGlobalAdmin ? request.Sede : scopeSede;
        var ids = await _resolution.ResolveToIdsAsync(emp, sed, ct);
        if (!ids.HasValue && (!string.IsNullOrWhiteSpace(emp) || !string.IsNullOrWhiteSpace(sed)))
        {
            return BadRequest("Empresa y/o sede no se pudieron resolver (códigos no encontrados).");
        }

        var payload = new CreateInternalUserRequest
        {
            Email = request.Email,
            Password = request.Password,
            Role = request.Role,
            FullName = request.Email,
            CompanyId = ids?.CompanyId,
            SiteId = ids?.SiteId,
            IsActive = request.IsActive
        };

        var created = await _internalUserClient.CreateAsync(payload, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToUserDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var existing = await _internalUserClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        if (!CanAccess(existing.CompanyName, existing.SiteName) || !CanAccess(request.Empresa, request.Sede))
        {
            return Forbid();
        }

        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        var emp = isGlobalAdmin ? request.Empresa : scopeEmpresa;
        var sed = isGlobalAdmin ? request.Sede : scopeSede;
        var ids = await _resolution.ResolveToIdsAsync(emp, sed, ct);
        if (!ids.HasValue && (!string.IsNullOrWhiteSpace(emp) || !string.IsNullOrWhiteSpace(sed)))
        {
            return BadRequest("Empresa y/o sede no se pudieron resolver (códigos no encontrados).");
        }

        var payload = new UpdateInternalUserRequest
        {
            Email = request.Email,
            Password = string.IsNullOrWhiteSpace(request.Password) ? null : request.Password,
            Role = request.Role,
            FullName = request.Email,
            CompanyId = ids?.CompanyId,
            SiteId = ids?.SiteId,
            IsActive = request.IsActive
        };

        var saved = await _internalUserClient.UpdateAsync(id, payload, ct);
        return Ok(MapToUserDto(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var existing = await _internalUserClient.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return NotFound();
            }

            if (!CanAccess(existing.CompanyName, existing.SiteName))
            {
                return Forbid();
            }

            await _internalUserClient.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest)
        {
            // Devuelve el mensaje con el body real de la API interna (clave para debug)
            return BadRequest(new { error = "La API interna rechazó la solicitud.", details = ex.Message });
        }
    }

    private static UserDto MapToUserDto(InternalUserDto user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Role = user.Role,
        Empresa = user.CompanyName,
        Sede = user.SiteName,
        IsActive = user.IsActive
    };

    private (string? Empresa, string? Sede, bool IsGlobalAdmin) GetScope()
    {
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        var empresa = User.FindFirst("empresa")?.Value;
        var sede = User.FindFirst("sede")?.Value;
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var isGlobalAdmin = isAdmin && string.IsNullOrWhiteSpace(empresa) && string.IsNullOrWhiteSpace(sede);

        return (empresa, sede, isGlobalAdmin);
    }

    private bool CanAccess(string? empresa, string? sede)
    {
        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        if (isGlobalAdmin)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(scopeEmpresa) || string.IsNullOrWhiteSpace(scopeSede))
        {
            return false;
        }

        return string.Equals(scopeEmpresa, empresa, StringComparison.OrdinalIgnoreCase)
               && string.Equals(scopeSede, sede, StringComparison.OrdinalIgnoreCase);
    }
}
