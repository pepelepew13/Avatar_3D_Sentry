using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services;
using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

/// <summary>
/// Stubs cuando InternalApi:BaseUrl no está configurado.
/// La API que conecta la DB con el sistema (documento MetaFusion→Sentry) la proporciona Sentry; hasta entonces el BFF no puede atender login ni gestión users/avatar-config.
/// </summary>
internal static class InternalApiNotConfigured
{
    public const string Message =
        "La API interna no está configurada (InternalApi:BaseUrl vacío). " +
        "Según el documento técnico, la API que conecta la DB con el sistema la proporciona el equipo Sentry.";
}

public class StubInternalUserClient : IInternalUserClient
{
    public Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto> CreateAsync(InternalUserDto user, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto> UpdateAsync(int id, InternalUserDto user, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubInternalAvatarConfigClient : IInternalAvatarConfigClient
{
    public Task<PagedResponse<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto?> GetByScopeAsync(string empresa, string sede, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto> CreateAsync(InternalAvatarConfigDto config, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto> UpdateAsync(int id, InternalAvatarConfigDto config, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubAvatarDataStore : IAvatarDataStore
{
    public Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}
