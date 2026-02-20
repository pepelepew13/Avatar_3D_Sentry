using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalAvatarConfigClient
{
    Task<PagedResponse<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken ct = default);
    Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<InternalAvatarConfigDto?> GetByScopeAsync(int? company, int? site, CancellationToken ct = default);
    Task<InternalAvatarConfigDto> CreateAsync(CreateInternalAvatarConfigRequest request, CancellationToken ct = default);
    Task<InternalAvatarConfigDto> UpdateAsync(int id, UpdateInternalAvatarConfigRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
