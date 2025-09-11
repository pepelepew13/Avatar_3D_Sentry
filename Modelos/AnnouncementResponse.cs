using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Avatar_3D_Sentry.Modelos;

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
    public List<Visema> Visemas { get; set; } = new();
}
