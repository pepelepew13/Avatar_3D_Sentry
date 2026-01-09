using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;

namespace Avatar_3D_Sentry.Services;

public interface IAvatarDataStore
{
    Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct);
    Task<bool> UserEmailExistsAsync(string email, CancellationToken ct);
    Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct);
    Task UpdateUserAsync(ApplicationUser user, CancellationToken ct);
    Task DeleteUserAsync(ApplicationUser user, CancellationToken ct);
    Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct);

    Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct);
    Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct);
    Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct);
    Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct);
}
