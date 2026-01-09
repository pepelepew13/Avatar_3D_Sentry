using System.ComponentModel.DataAnnotations;

namespace AvatarSentry.Shared.DTOs;

public class LoginRequest
{
    [Required, EmailAddress] 
    public string Email { get; set; } = string.Empty;
    
    [Required] 
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}