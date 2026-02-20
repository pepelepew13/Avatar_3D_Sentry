namespace AvatarSentry.Application.InternalApi.Models;

/// <summary>Respuesta de la API interna: GET by-scope o item dentro de lista.</summary>
public class InternalAvatarConfigDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int SiteId { get; set; }
    public string? ModelUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime? UrlExpiresAtUtc { get; set; }
    public string? Language { get; set; }
    public string? HairColor { get; set; }
    public int[] VoiceIds { get; set; } = Array.Empty<int>();
    public string? Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class InternalAvatarConfigPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public List<InternalAvatarConfigDto> Items { get; set; } = new();
}

/// <summary>Filtro para GET /internal/avatar-config. company y site son IDs (int).</summary>
public class AvatarConfigFilter
{
    public int? Company { get; set; }
    public int? Site { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Body para POST /internal/avatar-config (PascalCase en API).</summary>
public class CreateInternalAvatarConfigRequest
{
    public int CompanyId { get; set; }
    public int SiteId { get; set; }
    public string? ModelPath { get; set; }
    public string? BackgroundPath { get; set; }
    public string? LogoPath { get; set; }
    public string? Language { get; set; }
    public string? HairColor { get; set; }
    public int[] VoiceIds { get; set; } = Array.Empty<int>();
    public string? Status { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Body para PUT /internal/avatar-config/{id} (camelCase en API).</summary>
public class UpdateInternalAvatarConfigRequest
{
    public int CompanyId { get; set; }
    public int SiteId { get; set; }
    public string? ModelPath { get; set; }
    public string? BackgroundPath { get; set; }
    public string? LogoPath { get; set; }
    public string? Language { get; set; }
    public string? HairColor { get; set; }
    public int[] VoiceIds { get; set; } = Array.Empty<int>();
    public string? Status { get; set; }
    public bool IsActive { get; set; }
}
