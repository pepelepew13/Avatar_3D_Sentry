using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using AvatarSentry.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IInternalUserClient _internalUserClient;

    public UsersController(IInternalUserClient internalUserClient)
    {
        _internalUserClient = internalUserClient;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
        [FromQuery] string? q,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var empresa = User.FindFirst("empresa")?.Value;
        var sede = User.FindFirst("sede")?.Value;

        var filter = new UserFilter
        {
            Empresa = empresa,
            Sede = sede,
            Q = q,
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

        return Ok(MapToUserDto(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var payload = new InternalUserDto
        {
            Email = request.Email,
            PasswordHash = request.Password,
            Role = request.Role,
            Empresa = request.Empresa,
            Sede = request.Sede,
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

        var updated = new InternalUserDto
        {
            Id = existing.Id,
            Email = request.Email,
            PasswordHash = string.IsNullOrWhiteSpace(request.Password) ? existing.PasswordHash : request.Password,
            Role = request.Role,
            Empresa = request.Empresa,
            Sede = request.Sede,
            IsActive = request.IsActive
        };

        var saved = await _internalUserClient.UpdateAsync(id, updated, ct);
        return Ok(MapToUserDto(saved));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _internalUserClient.DeleteAsync(id, ct);
        return NoContent();
    }

    private static UserDto MapToUserDto(InternalUserDto user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Role = user.Role,
        Empresa = user.Empresa,
        Sede = user.Sede,
        IsActive = user.IsActive
    };
}
