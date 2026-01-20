using AvatarSentry.Application.Models;

namespace AvatarSentry.Application.Interfaces;

public interface IInternalAvatarConfigClient
{
    Task<PagedResult<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken cancellationToken);
    Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<InternalAvatarConfigDto?> GetByScopeAsync(string empresa, string sede, CancellationToken cancellationToken);
    Task<InternalResult> CreateAsync(InternalAvatarConfigCreateDto config, CancellationToken cancellationToken);
    Task<InternalResult> UpdateAsync(int id, InternalAvatarConfigUpdateDto config, CancellationToken cancellationToken);
    Task<InternalResult> PatchAsync(int id, InternalAvatarConfigPatchDto config, CancellationToken cancellationToken);
    Task<InternalResult> DeleteAsync(int id, CancellationToken cancellationToken);
}
