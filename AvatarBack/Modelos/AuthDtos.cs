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

public class UserItem
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
}

public class UserListResponse
{
    public int Total { get; set; }
    public List<UserItem> Items { get; set; } = new();
}

public class UpdateUserRequest
{
    [Required] public string Role { get; set; } = "User";
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public string? NewPassword { get; set; }
}
