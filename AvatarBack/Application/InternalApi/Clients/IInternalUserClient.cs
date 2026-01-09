using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalUserClient
{
    Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default);
    Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<InternalUserDto> CreateAsync(InternalUserDto user, CancellationToken ct = default);
    Task<InternalUserDto> UpdateAsync(int id, InternalUserDto user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
