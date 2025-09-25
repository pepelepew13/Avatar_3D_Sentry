using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;

namespace Avatar_3D_Sentry.Migrations;

[DbContext(typeof(AvatarContext))]
partial class AvatarContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

        modelBuilder.Entity<AvatarConfig>(b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            b.Property<string>("ColorCabello")
                .HasColumnType("TEXT");

            b.Property<string>("Empresa")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Fondo")
                .HasColumnType("TEXT");

            b.Property<string>("Idioma")
                .HasColumnType("TEXT");

            b.Property<string>("LogoPath")
                .HasColumnType("TEXT");

            b.Property<string>("ProveedorTts")
                .HasColumnType("TEXT");

            b.Property<string>("Sede")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Vestimenta")
                .HasColumnType("TEXT");

            b.Property<string>("Voz")
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("Empresa", "Sede")
                .IsUnique();

            b.ToTable("AvatarConfigs");
        });
    }
}
