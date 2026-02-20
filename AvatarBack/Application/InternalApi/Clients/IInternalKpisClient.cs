namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalKpisClient
{
    Task<string?> GetGlobalAsync(CancellationToken ct = default);
    Task<string?> GetByCompanyAsync(int companyId, CancellationToken ct = default);
    Task<string?> GetBySiteAsync(int siteId, CancellationToken ct = default);
}
