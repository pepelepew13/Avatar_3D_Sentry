using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Avatar_3D_Sentry.Models;

public class AnuncioRequest
{
    [Required] public string company { get; set; } = default!;
    [Required] public string site    { get; set; } = default!;
    [Required] public string module  { get; set; } = default!;
    [Required] public string ticket  { get; set; } = default!;
    public string? name    { get; set; }
    public string language { get; set; } = "es";
    public string? voice   { get; set; }
}

public record VisemeOut(string shapeKey, int tiempo);

public record TtsResponse(string audioUrl, int durationMs, List<VisemeOut> visemes);
