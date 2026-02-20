namespace AvatarSentry.Application.InternalApi.Models;

/// <summary>Sede en respuestas de la API interna (GET /internal/sites, GET /internal/sites/{id}).</summary>
public class InternalSiteDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }
}

public class InternalSitePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public List<InternalSiteDto> Items { get; set; } = new();
}
