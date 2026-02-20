using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

public interface IInternalUserClient
{
    Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default);
    Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>Incluye PasswordHash (para auth). Respuesta de GET /internal/users/by-email/{email}.</summary>
    Task<InternalUserByEmailDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<InternalUserDto> CreateAsync(CreateInternalUserRequest request, CancellationToken ct = default);
    Task<InternalUserDto> UpdateAsync(int id, UpdateInternalUserRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default);
}
