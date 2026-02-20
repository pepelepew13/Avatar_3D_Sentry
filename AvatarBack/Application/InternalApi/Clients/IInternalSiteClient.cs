using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalSiteClient
{
    Task<InternalSiteDto?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Obtiene la primera sede de la empresa con el Code dado (case-insensitive).</summary>
    Task<InternalSiteDto?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken ct = default);
    Task<PagedResponse<InternalSiteDto>> GetSitesAsync(int? companyId, string? code, string? name, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default);
}
