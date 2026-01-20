using System.ComponentModel.DataAnnotations;

namespace AvatarSentry.Application.Models;

public class UserItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; }
}

public class UserCreateRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = "User";
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
}

public class UserUpdateRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    [Required] public string Role { get; set; } = "User";
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; }
}
