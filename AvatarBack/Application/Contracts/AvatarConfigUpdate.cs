using System.ComponentModel.DataAnnotations;

namespace Avatar_3D_Sentry.Modelos;

public class AvatarConfigUpdate
{
    [MaxLength(64)] public string? Vestimenta { get; set; }     // "predeterminado" | "traje" | "vestido" | etc.
    [MaxLength(512)] public string? Fondo { get; set; }         // clave preset o URL (si usas tu CDN)
    [MaxLength(32)] public string? ProveedorTts { get; set; }   // "polly" (por ahora)
    [MaxLength(64)] public string? Voz { get; set; }            // "Lucia", "Joanna"…
    [MaxLength(8)]  public string? Idioma { get; set; }         // "es" | "en" | "pt"
    [MaxLength(32)] public string? ColorCabello { get; set; }   // "negro" | "castano" | "rubio" | "predeterminado"
}
