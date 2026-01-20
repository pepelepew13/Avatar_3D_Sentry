namespace AvatarSentry.Application.Models;

public class AvatarConfigFilter
{
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
