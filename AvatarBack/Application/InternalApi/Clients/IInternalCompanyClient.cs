using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalCompanyClient
{
    Task<InternalCompanyDto?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Obtiene la primera empresa cuyo Code coincide (case-insensitive).</summary>
    Task<InternalCompanyDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PagedResponse<InternalCompanyDto>> GetCompaniesAsync(string? code, string? name, string? sector, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default);
}
