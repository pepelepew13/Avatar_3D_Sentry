using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services;
using Avatar_3D_Sentry.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAvatarDataStore _dataStore;
    private readonly PasswordHasher<ApplicationUser> _hasher = new();
    private readonly JwtSettings _jwt;

    public AuthController(IAvatarDataStore dataStore, IOptions<JwtSettings> jwt)
    {
        _dataStore = dataStore;
        _jwt = jwt.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _dataStore.FindUserByEmailAsync(req.Email, ct);
        if (user is null) return Unauthorized("Credenciales inválidas.");

        if (!IsAspNetIdentityHash(user.PasswordHash))
        {
            if (!string.Equals(user.PasswordHash, req.Password, StringComparison.Ordinal))
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var hashedPassword = _hasher.HashPassword(user, req.Password);
            await _dataStore.UpdateUserPasswordHashAsync(user.Id, hashedPassword, ct);
        }
        else
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized("Credenciales inválidas.");
        }

        var (token, exp) = GenerateJwt(user);
        return Ok(new LoginResponse
        {
            Token = token,
            Role = user.Role,
            Empresa = user.Empresa,
            Sede = user.Sede,
            ExpiresAtUtc = exp
        });
    }

    // ⬇️ Solo SUPERADMIN crea usuarios
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("users")]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (!string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(req.Role, "User", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Role debe ser Admin o User.");
        }

        if (await _dataStore.UserEmailExistsAsync(req.Email, ct))
            return Conflict("Ya existe un usuario con ese email.");

        var user = new ApplicationUser
        {
            Email = req.Email.Trim().ToLowerInvariant(),
            Role  = req.Role.Trim(),
            Empresa = string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? null : req.Empresa?.Trim(),
            Sede    = string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? null : req.Sede?.Trim(),
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);
        await _dataStore.CreateUserAsync(user, ct);
        return CreatedAtAction(nameof(GetMe), new { }, null);
    }

    // ⬇️ Solo SUPERADMIN lista usuarios
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("users")]
    public async Task<ActionResult<UserListResponse>> ListUsers([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? q = null, [FromQuery] string? role = null, CancellationToken ct = default)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 10;
        if (take > 100) take = 100;

        var (total, items) = await _dataStore.ListUsersAsync(skip, take, q, role, ct);
        var response = new UserListResponse
        {
            Total = total,
            Items = items.Select(u => new UserItem
            {
                Id = u.Id.ToString(),
                Email = u.Email,
                Role = u.Role,
                Empresa = u.Empresa,
                Sede = u.Sede,
                IsActive = u.IsActive
            }).ToList()
        };

        return Ok(response);
    }

    // ⬇️ Solo SUPERADMIN edita usuarios
    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("users/{id:int}")]
    public async Task<ActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _dataStore.FindUserByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (!string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(req.Role, "User", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Role debe ser Admin o User.");
        }

        if (string.Equals(req.Role, "User", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(req.Empresa))
        {
            return BadRequest("Empresa es requerida para usuarios.");
        }

        user.Role = req.Role.Trim();
        if (string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            user.Empresa = null;
            user.Sede = null;
        }
        else
        {
            user.Empresa = req.Empresa?.Trim();
            user.Sede = req.Sede?.Trim();
        }

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            if (req.NewPassword.Length < 6) return BadRequest("Password debe tener al menos 6 caracteres.");
            user.PasswordHash = _hasher.HashPassword(user, req.NewPassword);
        }

        await _dataStore.UpdateUserAsync(user, ct);
        return NoContent();
    }

    // ⬇️ Solo SUPERADMIN borra usuarios
    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("users/{id:int}")]
    public async Task<ActionResult> DeleteUser([FromRoute] int id, CancellationToken ct)
    {
        var user = await _dataStore.FindUserByIdAsync(id, ct);
        if (user is null) return NotFound();

        await _dataStore.DeleteUserAsync(user, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> GetMe()
    {
        var email   = User.FindFirstValue(ClaimTypes.Email);
        var role    = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        var empresa = User.FindFirst("empresa")?.Value;
        var sede    = User.FindFirst("sede")?.Value;
        return Ok(new { email, role, empresa, sede });
    }

    private (string token, DateTime expiresUtc) GenerateJwt(ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var exp = now.AddHours(12);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.Empresa))
            claims.Add(new Claim("empresa", user.Empresa!.Trim().ToLowerInvariant()));

        if (!string.IsNullOrWhiteSpace(user.Sede))
            claims.Add(new Claim("sede", user.Sede!.Trim().ToLowerInvariant()));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: exp,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), exp);
    }

    private static bool IsAspNetIdentityHash(string? hash)
    {
        return !string.IsNullOrWhiteSpace(hash)
            && hash.StartsWith("AQAAAA", StringComparison.Ordinal);
    }
}
