namespace Avatar_3D_Sentry.Modelos;

public class AvatarConfigDto
{
    public int Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }               // URL (p. ej. /assets/123) o clave preset
    public string? ProveedorTts { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? ColorCabello { get; set; }
    public string? LogoPath { get; set; }            // URL (p. ej. /assets/45)

    public static AvatarConfigDto FromEntity(AvatarConfig e) => new()
    {
        Id            = e.Id,
        Empresa       = e.Empresa,
        Sede          = e.Sede,
        IsActive      = e.IsActive,
        Vestimenta    = e.Vestimenta,
        Fondo         = e.Fondo,
        ProveedorTts  = e.ProveedorTts,
        Voz           = e.Voz,
        Idioma        = e.Idioma,
        ColorCabello  = e.ColorCabello,
        LogoPath      = e.LogoPath
    };
}
