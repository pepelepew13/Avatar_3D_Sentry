using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalAvatarConfigClient
{
    Task<PagedResponse<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken ct = default);
    Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<InternalAvatarConfigDto?> GetByScopeAsync(string empresa, string sede, CancellationToken ct = default);
    Task<InternalAvatarConfigDto> CreateAsync(InternalAvatarConfigDto config, CancellationToken ct = default);
    Task<InternalAvatarConfigDto> UpdateAsync(int id, InternalAvatarConfigDto config, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
