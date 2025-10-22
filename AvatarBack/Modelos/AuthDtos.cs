using System.ComponentModel.DataAnnotations;

namespace Avatar_3D_Sentry.Models;

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class CreateUserRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;

    // "Admin" | "User"
    [Required] public string Role { get; set; } = "User";

    // Para usuarios (Scope). Ignorados si Role=Admin.
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
}
