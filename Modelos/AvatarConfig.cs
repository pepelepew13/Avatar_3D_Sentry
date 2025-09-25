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

    /// <summary>
    /// Proveedor del servicio TTS seleccionado.
    /// </summary>
    public string? ProveedorTts { get; set; }

    /// <summary>
    /// Voz empleada al generar el audio del avatar.
    /// </summary>
    public string? Voz { get; set; }

    /// <summary>
    /// Idioma preferido para la narración del avatar.
    /// </summary>
    public string? Idioma { get; set; }
}

