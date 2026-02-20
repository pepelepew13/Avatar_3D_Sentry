using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IInternalUserClient _internalUserClient;
    private readonly IInternalAvatarConfigClient _internalAvatarConfigClient;
    private readonly ICompanySiteResolutionService _resolution;
    private readonly JwtSettings _jwt;

    public AuthController(
        IInternalUserClient internalUserClient,
        IInternalAvatarConfigClient internalAvatarConfigClient,
        ICompanySiteResolutionService resolution,
        IOptions<JwtSettings> jwt)
    {
        _internalUserClient = internalUserClient;
        _internalAvatarConfigClient = internalAvatarConfigClient;
        _resolution = resolution;
        _jwt = jwt.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(req.Password))
        {
            return Unauthorized(new { error = "Credenciales inválidas." });
        }

        InternalUserByEmailDto? user;
        try
        {
            user = await _internalUserClient.GetByEmailAsync(normalizedEmail, ct);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "No se pudo validar el usuario en la API interna." });
        }
        if (user is null)
        {
            return Unauthorized(new { error = "Credenciales inválidas." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { error = "Usuario inactivo." });
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !string.Equals(user.PasswordHash, req.Password, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Credenciales inválidas." });
        }

        var role = NormalizeRole(user.Role);
        string? empresa = null;
        string? sede = null;

        // SuperAdmin/Admin: sin empresa ni sede (acceso global).
        if (IsGlobalAdminRole(role))
        {
            // No validar avatar config ni resolver nombres; empresa y sede quedan null.
        }
        // CompanyAdmin: puede tener solo CompanyId (SiteId null).
        else if (string.Equals(role, "CompanyAdmin", StringComparison.OrdinalIgnoreCase))
        {
            if (!user.CompanyId.HasValue)
            {
                return Unauthorized(new { error = "Usuario sin empresa asignada." });
            }
            var names = await _resolution.GetNamesAsync(user.CompanyId.Value, 0, ct);
            empresa = names?.CompanyName;
            sede = null;
        }
        // SiteAdmin, AvatarEditor, User: requieren CompanyId y SiteId.
        else
        {
            if (!user.CompanyId.HasValue || !user.SiteId.HasValue)
            {
                return Unauthorized(new { error = "Usuario sin empresa/sede asignada." });
            }

            var config = await _internalAvatarConfigClient.GetByScopeAsync(user.CompanyId, user.SiteId, ct);
            if (config is null)
            {
                return Unauthorized(new { error = "No se encontró configuración de avatar para este usuario." });
            }

            var names = await _resolution.GetNamesAsync(user.CompanyId.Value, user.SiteId.Value, ct);
            empresa = names?.CompanyName;
            sede = names?.SiteName;
        }

        var (token, exp) = GenerateJwt(user.Id, user.Email, role, empresa, sede);
        return Ok(new LoginResponse
        {
            Token = token,
            Role = role,
            Empresa = empresa,
            Sede = sede,
            ExpiresAtUtc = exp
        });
    }

    private (string token, DateTime expiresUtc) GenerateJwt(int userId, string email, string role, string? empresa, string? sede)
    {
        var now = DateTime.UtcNow;
        var exp = now.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        if (!string.IsNullOrWhiteSpace(empresa))
            claims.Add(new Claim("empresa", empresa.Trim().ToLowerInvariant()));

        if (!string.IsNullOrWhiteSpace(sede))
            claims.Add(new Claim("sede", sede.Trim().ToLowerInvariant()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
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

    /// <summary>Roles que no requieren empresa/sede (CompanyId/SiteId pueden ser NULL).</summary>
    private static bool IsGlobalAdminRole(string role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRole(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return "Admin";
        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return "Admin"; // El panel usa "Admin" para menú admin; SuperAdmin se emite como Admin en JWT.
        if (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase))
            return "User";
        if (string.Equals(role, "CompanyAdmin", StringComparison.OrdinalIgnoreCase))
            return "CompanyAdmin";
        if (string.Equals(role, "SiteAdmin", StringComparison.OrdinalIgnoreCase))
            return "SiteAdmin";
        if (string.Equals(role, "AvatarEditor", StringComparison.OrdinalIgnoreCase))
            return "AvatarEditor";
        return role?.Trim() ?? string.Empty;
    }

}
