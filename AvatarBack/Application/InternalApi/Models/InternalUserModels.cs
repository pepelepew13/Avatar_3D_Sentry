namespace AvatarSentry.Application.InternalApi.Models;

public class InternalUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public bool IsActive { get; set; }
}

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public class UserFilter
{
    public string? Empresa { get; set; }
    public string? Sede { get; set; }
    public string? Role { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
