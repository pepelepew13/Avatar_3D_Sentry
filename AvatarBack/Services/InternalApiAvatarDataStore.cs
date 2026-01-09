using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services;

public class InternalApiAvatarDataStore : IAvatarDataStore
{
    private readonly InternalApiOptions _options;

    public InternalApiAvatarDataStore(IOptions<InternalApiOptions> options)
    {
        _options = options.Value;
        _ = _options.BaseUrl;
    }

    public Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }

    public Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        throw new NotImplementedException("TODO: reemplazar EF Core por llamadas a la API interna.");
    }
}
