using AvatarSentry.Application.Models;

namespace AvatarSentry.Application.Interfaces;

public interface IInternalUserClient
{
    Task<PagedResult<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken cancellationToken);
    Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<InternalResult> CreateAsync(InternalUserCreateDto user, CancellationToken cancellationToken);
    Task<InternalResult> UpdateAsync(int id, InternalUserUpdateDto user, CancellationToken cancellationToken);
    Task<InternalResult> DeleteAsync(int id, CancellationToken cancellationToken);
}
