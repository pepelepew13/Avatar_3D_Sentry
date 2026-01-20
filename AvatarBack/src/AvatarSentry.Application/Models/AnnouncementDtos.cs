using System.ComponentModel.DataAnnotations;

namespace AvatarSentry.Application.Models;

public class AnnouncementRequest
{
    [Required] public string Empresa { get; set; } = string.Empty;
    [Required] public string Sede { get; set; } = string.Empty;
    [Required] public string Modulo { get; set; } = string.Empty;
    [Required] public string Turno { get; set; } = string.Empty;
    public string? Nombre { get; set; }
}

public class AnnouncementResponse
{
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public List<VisemeDto> Visemas { get; set; } = new();
}

public class VisemeDto
{
    public string ShapeKey { get; set; } = string.Empty;
    public int Tiempo { get; set; }
    public int? Viseme { get; set; }
}
