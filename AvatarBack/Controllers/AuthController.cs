using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AvatarContext _db;
    private readonly PasswordHasher<ApplicationUser> _hasher = new();
    private readonly JwtSettings _jwt;

    public AuthController(AvatarContext db, IOptions<JwtSettings> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
        if (user is null) return Unauthorized("Credenciales inválidas.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed) return Unauthorized("Credenciales inválidas.");

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

        if (await _db.Users.AnyAsync(u => u.Email == req.Email, ct))
            return Conflict("Ya existe un usuario con ese email.");

        var user = new ApplicationUser
        {
            Email = req.Email.Trim().ToLowerInvariant(),
            Role  = req.Role.Trim(),
            Empresa = string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? null : req.Empresa?.Trim(),
            Sede    = string.Equals(req.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? null : req.Sede?.Trim()
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetMe), new { }, null);
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
}
