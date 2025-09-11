namespace Avatar_3D_Sentry.Modelos;

/// <summary>
/// Representa la configuración visual de un avatar asociada a una empresa y sede.
/// </summary>
public class AvatarConfig
{
    public int Id { get; set; }

    public required string Empresa { get; set; }

    public required string Sede { get; set; }

    /// <summary>
    /// Ruta del logo aplicado al pecho del modelo.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Información sobre la vestimenta seleccionada.
    /// </summary>
    public string? Vestimenta { get; set; }

    /// <summary>
    /// Configuración del fondo para el avatar.
    /// </summary>
    public string? Fondo { get; set; }
}

