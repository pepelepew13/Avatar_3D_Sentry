using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AvatarSentry.Shared.DTOs;

public class AnnouncementRequest
{
    private const string Patron = @"^[A-Za-zÀ-ÿ0-9\s]{1,50}$";

    [JsonPropertyName("empresa")]
    [Required(ErrorMessage = "La empresa es obligatoria")]
    [RegularExpression(Patron)]
    public string Empresa { get; set; } = string.Empty;

    [JsonPropertyName("sede")]
    [Required(ErrorMessage = "La sede es obligatoria")]
    [RegularExpression(Patron)]
    public string Sede { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    [Required]
    [RegularExpression(Patron)]
    public string Modulo { get; set; } = string.Empty;

    [JsonPropertyName("turno")]
    [Required]
    [RegularExpression(Patron)]
    public string Turno { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    [Required]
    [RegularExpression(Patron)]
    public string Nombre { get; set; } = string.Empty;

    // NUEVO: Soporte multilenguaje (es, en, pt)
    [JsonPropertyName("idioma")]
    public string Idioma { get; set; } = "es";
}