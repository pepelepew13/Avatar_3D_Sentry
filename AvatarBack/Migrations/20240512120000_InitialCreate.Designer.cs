using Avatar_3D_Sentry.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avatar_3D_Sentry.Migrations;

[DbContext(typeof(AvatarContext))]
[Migration("20240512120000_InitialCreate")]
partial class InitialCreate
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

        modelBuilder.Entity("Avatar_3D_Sentry.Modelos.AvatarConfig", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

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
#pragma warning restore 612, 618
    }
}
