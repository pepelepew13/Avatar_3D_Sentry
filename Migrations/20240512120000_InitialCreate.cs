using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avatar_3D_Sentry.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AvatarConfigs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Empresa = table.Column<string>(type: "TEXT", nullable: false),
                Sede = table.Column<string>(type: "TEXT", nullable: false),
                LogoPath = table.Column<string>(type: "TEXT", nullable: true),
                Vestimenta = table.Column<string>(type: "TEXT", nullable: true),
                Fondo = table.Column<string>(type: "TEXT", nullable: true),
                ProveedorTts = table.Column<string>(type: "TEXT", nullable: true),
                Voz = table.Column<string>(type: "TEXT", nullable: true),
                Idioma = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AvatarConfigs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AvatarConfigs_Empresa_Sede",
            table: "AvatarConfigs",
            columns: new[] { "Empresa", "Sede" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AvatarConfigs");
    }
}
