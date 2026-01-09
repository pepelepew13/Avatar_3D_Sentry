using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Microsoft.EntityFrameworkCore;

namespace Avatar_3D_Sentry.Services;

public class EfAvatarDataStore : IAvatarDataStore
{
    private readonly AvatarContext _db;

    public EfAvatarDataStore(AvatarContext db)
    {
        _db = db;
    }

    public Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
    {
        return _db.Users.AnyAsync(u => u.Email == email, ct);
    }

    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [ApplicationUser] SET [PasswordHash] = {passwordHash} WHERE [Id] = {userId}",
            ct);
    }

    public Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var normalizedEmpresa = empresa.Trim().ToLowerInvariant();
        var normalizedSede = sede.Trim().ToLowerInvariant();

        return _db.AvatarConfigs.FirstOrDefaultAsync(a =>
            a.NormalizedEmpresa == normalizedEmpresa && a.NormalizedSede == normalizedSede, ct);
    }

    public async Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        _db.AvatarConfigs.Add(config);
        await _db.SaveChangesAsync(ct);
        return config;
    }

    public async Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        _db.AvatarConfigs.Update(config);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        _db.AvatarConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
    }
}
