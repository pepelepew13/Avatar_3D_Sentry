using AvatarSentry.Application.Interfaces;
using AvatarSentry.Application.Models;

namespace AvatarSentry.Infrastructure;

public class InternalAvatarConfigClient : IInternalAvatarConfigClient
{
    public Task<PagedResult<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PagedResult<InternalAvatarConfigDto>());
    }

    public Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult<InternalAvatarConfigDto?>(null);
    }

    public Task<InternalAvatarConfigDto?> GetByScopeAsync(string empresa, string sede, CancellationToken cancellationToken)
    {
        return Task.FromResult<InternalAvatarConfigDto?>(null);
    }

    public Task<InternalResult> CreateAsync(InternalAvatarConfigCreateDto config, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }

    public Task<InternalResult> UpdateAsync(int id, InternalAvatarConfigUpdateDto config, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }

    public Task<InternalResult> PatchAsync(int id, InternalAvatarConfigPatchDto config, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }

    public Task<InternalResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }
}
