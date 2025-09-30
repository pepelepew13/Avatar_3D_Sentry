using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Avatar_3D_Sentry.Modelos;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Avatar_3D_Sentry.Data;

/// <summary>
/// DbContext para gestionar la configuración del avatar.
/// </summary>
public class AvatarContext : DbContext
{
    public AvatarContext(DbContextOptions<AvatarContext> options)
        : base(options)
    {
    }

    public DbSet<AvatarConfig> AvatarConfigs => Set<AvatarConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvatarConfig>()
            .HasIndex(config => new { config.NormalizedEmpresa, config.NormalizedSede })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeAvatarConfigs();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeAvatarConfigs();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeAvatarConfigs()
    {
        IEnumerable<EntityEntry<AvatarConfig>> entries = ChangeTracker
            .Entries<AvatarConfig>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.Normalize();
        }
    }
}

