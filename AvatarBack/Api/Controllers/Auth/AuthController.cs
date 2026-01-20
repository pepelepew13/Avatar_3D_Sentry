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
        var normalizedEmail = req.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            normalizedEmail = "anonymous@local";
        }

        var user = new AvatarSentry.Application.InternalApi.Models.InternalUserDto
        {
            Id = 0,
            Email = normalizedEmail,
            Role = "Admin",
            Empresa = null,
            Sede = null,
            IsActive = true
        };

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
