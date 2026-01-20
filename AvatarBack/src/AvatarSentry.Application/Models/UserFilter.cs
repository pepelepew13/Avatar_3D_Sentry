namespace AvatarSentry.Application.Models;

public class UserFilter
{
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public string? Role { get; set; }
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
