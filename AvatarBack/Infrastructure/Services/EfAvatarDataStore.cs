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
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
    }

    public Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
    }

    public async Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
    {
        var query = _db.Users.Where(u => u.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(u => u.Role.ToLower() == normalizedRole);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalizedQ = q.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(normalizedQ) ||
                (u.Empresa != null && u.Empresa.ToLower().Contains(normalizedQ)) ||
                (u.Sede != null && u.Sede.ToLower().Contains(normalizedQ)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (total, items);
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
            a.NormalizedEmpresa == normalizedEmpresa && a.NormalizedSede == normalizedSede && a.IsActive, ct);
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
