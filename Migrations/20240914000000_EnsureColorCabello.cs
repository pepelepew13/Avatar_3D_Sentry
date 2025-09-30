using Avatar_3D_Sentry.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Avatar_3D_Sentry.Migrations;

[DbContext(typeof(AvatarContext))]
[Migration("20240914000000_EnsureColorCabello")]
public partial class EnsureColorCabello : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AvatarConfigs_Empresa_Sede",
            table: "AvatarConfigs");

        migrationBuilder.AddColumn<string>(
            name: "NormalizedEmpresa",
            table: "AvatarConfigs",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "NormalizedSede",
            table: "AvatarConfigs",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql(
            """
            UPDATE "AvatarConfigs"
            SET "NormalizedEmpresa" = LOWER("Empresa"),
                "NormalizedSede" = LOWER("Sede");
            """);

        migrationBuilder.CreateIndex(
            name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
            table: "AvatarConfigs",
            columns: new[] { "NormalizedEmpresa", "NormalizedSede" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
            table: "AvatarConfigs");

        migrationBuilder.DropColumn(
            name: "NormalizedSede",
            table: "AvatarConfigs");

        migrationBuilder.DropColumn(
            name: "NormalizedEmpresa",
            table: "AvatarConfigs");

        migrationBuilder.CreateIndex(
            name: "IX_AvatarConfigs_Empresa_Sede",
            table: "AvatarConfigs",
            columns: new[] { "Empresa", "Sede" },
            unique: true);
    }
}
