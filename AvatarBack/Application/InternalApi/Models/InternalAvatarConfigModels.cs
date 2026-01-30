namespace AvatarSentry.Application.InternalApi.Models;

public class InternalAvatarConfigDto
{
    public int Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string? Vestimenta { get; set; }
    public string? Fondo { get; set; }
    public string? Voz { get; set; }
    public string? Idioma { get; set; }
    public string? LogoPath { get; set; }
    public string? ColorCabello { get; set; }
    public string? BackgroundPath { get; set; }
    public bool IsActive { get; set; }
}

public class InternalAvatarConfigPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<InternalAvatarConfigDto> Items { get; set; } = new();
}

public class AvatarConfigFilter
{
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
