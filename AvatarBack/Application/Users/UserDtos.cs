using System.ComponentModel.DataAnnotations;

namespace AvatarSentry.Application.Users;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; } = true;
}
