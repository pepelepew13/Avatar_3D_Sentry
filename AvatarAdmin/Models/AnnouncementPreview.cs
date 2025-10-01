using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AvatarAdmin.Models;

public sealed class AnnouncementPreviewRequest
{
    [JsonPropertyName("empresa")]
    public string Empresa { get; set; } = string.Empty;

    [JsonPropertyName("sede")]
    public string Sede { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    public string Modulo { get; set; } = string.Empty;

    [JsonPropertyName("turno")]
    public string Turno { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class AnnouncementPreviewResponse
{
    [JsonPropertyName("texto")]
    public string Texto { get; set; } = string.Empty;

    [JsonPropertyName("audioUrl")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("visemas")]
    public List<VisemeFrame> Visemas { get; set; } = new();
}

public sealed class VisemeFrame
{
    [JsonPropertyName("shapeKey")]
    public string? ShapeKey { get; set; }

    [JsonPropertyName("tiempo")]
    public int Tiempo { get; set; }

    [JsonPropertyName("viseme")]
    public string? Viseme { get; set; }
}
