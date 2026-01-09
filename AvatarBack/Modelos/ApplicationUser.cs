using System.ComponentModel.DataAnnotations;

namespace Avatar_3D_Sentry.Models;

public class ApplicationUser
{
    public int Id { get; set; }

    [Required, MaxLength(128)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // "Admin" | "User"
    [Required, MaxLength(16)]
    public string Role { get; set; } = "User";

    // Filtro de alcance
    [MaxLength(64)]
    public string? Empresa { get; set; }   // null para Admin

    [MaxLength(64)]
    public string? Sede { get; set; }      // null => todas las sedes de Empresa

    public bool IsActive { get; set; } = true;
}
