using System.Text.Json.Serialization;

namespace AvatarSentry.Shared.DTOs;

public class AnnouncementResponse
{
    [JsonPropertyName("empresa")]
    public string Empresa { get; set; } = string.Empty;

    [JsonPropertyName("sede")]
    public string Sede { get; set; } = string.Empty;

    [JsonPropertyName("texto")]
    public string Texto { get; set; } = string.Empty;

    [JsonPropertyName("audioUrl")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("visemas")]
    public List<VisemaDto> Visemas { get; set; } = new();
}

// Extraemos la clase Visema para compartirla
public class VisemaDto
{
    public string ShapeKey { get; set; } = string.Empty;
    public int Tiempo { get; set; }
    public int? Id { get; set; }
}