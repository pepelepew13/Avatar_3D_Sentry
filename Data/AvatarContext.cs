using Microsoft.EntityFrameworkCore;
using Avatar_3D_Sentry.Modelos;

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
}

