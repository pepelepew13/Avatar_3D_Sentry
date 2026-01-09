using System.ComponentModel.DataAnnotations;

namespace AvatarSentry.Application.AvatarConfigs;

public class AvatarConfigDto
{
    public int Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}

public class AvatarConfigPatchRequest
{
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}

public class CreateAvatarConfigRequest
{
    [Required]
    public string Empresa { get; set; } = string.Empty;

    [Required]
    public string Sede { get; set; } = string.Empty;

    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}

public class UpdateAvatarConfigRequest
{
    [Required]
    public string Empresa { get; set; } = string.Empty;

    [Required]
    public string Sede { get; set; } = string.Empty;

    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}
