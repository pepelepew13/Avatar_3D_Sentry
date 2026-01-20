using AvatarSentry.Application.Interfaces;
using AvatarSentry.Application.Models;

namespace AvatarSentry.Infrastructure;

public class InternalUserClient : IInternalUserClient
{
    public Task<PagedResult<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PagedResult<InternalUserDto>());
    }

    public Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult<InternalUserDto?>(null);
    }

    public Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return Task.FromResult<InternalUserDto?>(null);
    }

    public Task<InternalResult> CreateAsync(InternalUserCreateDto user, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }

    public Task<InternalResult> UpdateAsync(int id, InternalUserUpdateDto user, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }

    public Task<InternalResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InternalResult());
    }
}
