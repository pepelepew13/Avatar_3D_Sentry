namespace AvatarSentry.Shared.DTOs;

public class AvatarConfigDto
{
    public int Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? ColorCabello { get; set; }
    public string? LogoPath { get; set; }
}
