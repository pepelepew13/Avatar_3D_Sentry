using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Avatar_3D_Sentry.Modelos;

public class SolicitudAnuncio
{
    private const string Patron = @"^[A-Za-zÀ-ÿ0-9\s]{1,50}$";

    [JsonPropertyName("empresa")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(Patron)]
    public string Empresa { get; set; } = string.Empty;

    [JsonPropertyName("sede")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(Patron)]
    public string Sede { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(Patron)]
    public string Modulo { get; set; } = string.Empty;

    [JsonPropertyName("turno")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(Patron)]
    public string Turno { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(Patron)]
    public string Nombre { get; set; } = string.Empty;
}
