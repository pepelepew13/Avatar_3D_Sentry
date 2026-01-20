using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Microsoft.EntityFrameworkCore;

namespace Avatar_3D_Sentry.Data;

public class AvatarContext : DbContext
{
    public AvatarContext(DbContextOptions<AvatarContext> options) : base(options) { }

    public DbSet<AvatarConfig> AvatarConfigs => Set<AvatarConfig>();
    public DbSet<ApplicationUser> Users       => Set<ApplicationUser>();
    public DbSet<AssetFile>      Assets      => Set<AssetFile>();   // 👈 AQUI

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AvatarConfig>(b =>
        {
            b.ToTable("AvatarConfig");
            b.HasIndex(a => new { a.NormalizedEmpresa, a.NormalizedSede }).IsUnique(false);
        });

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("ApplicationUser");
            b.HasIndex(u => u.Email).IsUnique();
        });

        // --- AssetFile mapeo simple
        modelBuilder.Entity<AssetFile>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Empresa).HasMaxLength(128);
            b.Property(a => a.Sede).HasMaxLength(128);
            b.Property(a => a.Tipo).HasMaxLength(32);
            b.Property(a => a.FileName).HasMaxLength(256);
            b.Property(a => a.ContentType).HasMaxLength(128);
            b.Property(a => a.Data).IsRequired();
            b.HasIndex(a => new { a.Empresa, a.Sede, a.Tipo });
        });
    }
}
