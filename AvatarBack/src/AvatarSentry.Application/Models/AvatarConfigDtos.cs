namespace AvatarSentry.Application.Models;

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

public class AvatarConfigCreateRequest
{
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}

public class AvatarConfigUpdateRequest
{
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
}

public class AvatarConfigPublicDto
{
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string? FondoUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
}
