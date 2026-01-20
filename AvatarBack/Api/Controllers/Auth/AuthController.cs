using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using AvatarSentry.Application.InternalApi.Clients;
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
    private readonly JwtSettings _jwt;

    public AuthController(IInternalUserClient internalUserClient, IOptions<JwtSettings> jwt)
    {
        _internalUserClient = internalUserClient;
        _jwt = jwt.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            "Login desactivado temporalmente: esperando endpoint de autenticación compatible en UserAvatarApi.");

        var normalizedEmail = req.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest("Email requerido.");
        }

        var user = await _internalUserClient.GetByEmailAsync(normalizedEmail.ToUpperInvariant(), ct);
        if (user is null && !string.Equals(normalizedEmail, normalizedEmail.ToUpperInvariant(), StringComparison.Ordinal))
        {
            user = await _internalUserClient.GetByEmailAsync(normalizedEmail, ct);
        }

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Credenciales inválidas.");
        }

        // TODO: Migrar PasswordHash a BCrypt u otro esquema de hashing.
        if (!IsPasswordValid(user.PasswordHash, req.Password))
        {
            return Unauthorized("Credenciales inválidas.");
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

    private static bool IsPasswordValid(string storedPasswordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPasswordHash) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        if (string.Equals(storedPasswordHash, providedPassword, StringComparison.Ordinal))
        {
            return true;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedPassword);
        var providedBase64 = Convert.ToBase64String(providedBytes);

        return string.Equals(storedPasswordHash, providedBase64, StringComparison.Ordinal);
    }

}
