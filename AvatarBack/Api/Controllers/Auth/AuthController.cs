using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using AvatarSentry.Application.InternalApi.Clients;
using Microsoft.AspNetCore.Http;
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
    private readonly JwtSettings _jwt;

    public AuthController(
        IInternalUserClient internalUserClient,
        IInternalAvatarConfigClient internalAvatarConfigClient,
        IOptions<JwtSettings> jwt)
    {
        _internalUserClient = internalUserClient;
        _internalAvatarConfigClient = internalAvatarConfigClient;
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

        AvatarSentry.Application.InternalApi.Models.InternalUserDto? user;
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

        if (!string.Equals(user.PasswordHash, req.Password, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Credenciales inválidas." });
        }

        if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(user.Empresa) || string.IsNullOrWhiteSpace(user.Sede))
            {
                return Unauthorized(new { error = "Usuario sin empresa/sede asignada." });
            }

            AvatarSentry.Application.InternalApi.Models.AvatarConfigDto? config;
            try
            {
                config = await _internalAvatarConfigClient.GetByScopeAsync(user.Empresa, user.Sede, ct);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = "No se pudo validar la configuración de avatar en la API interna." });
            }
            if (config is null)
            {
                return Unauthorized(new { error = "No se encontró configuración de avatar para este usuario." });
            }

            user.Empresa = config.Empresa;
            user.Sede = config.Sede;
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


    private (string token, DateTime expiresUtc) GenerateJwt(AvatarSentry.Application.InternalApi.Models.InternalUserDto user)
    {
        var now = DateTime.UtcNow;
        var exp = now.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role)
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

}
