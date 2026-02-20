namespace AvatarSentry.Application.InternalApi.Models;

/// <summary>Empresa en respuestas de la API interna (GET /internal/companies, GET /internal/companies/{id}).</summary>
public class InternalCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? CorporateId { get; set; }
    public string? Sector { get; set; }
    public string? LogoPath { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime? UrlExpiresAtUtc { get; set; }
    public string? AssetsRootPath { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class InternalCompanyPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public List<InternalCompanyDto> Items { get; set; } = new();
}
