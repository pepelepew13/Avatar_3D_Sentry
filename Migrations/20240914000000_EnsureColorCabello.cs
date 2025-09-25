using Microsoft.EntityFrameworkCore.Migrations;

namespace Avatar_3D_Sentry.Migrations;

public partial class EnsureColorCabello : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "AvatarConfigs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AvatarConfigs" PRIMARY KEY AUTOINCREMENT,
                "Empresa" TEXT NOT NULL,
                "Sede" TEXT NOT NULL,
                "LogoPath" TEXT NULL,
                "Vestimenta" TEXT NULL,
                "Fondo" TEXT NULL,
                "ProveedorTts" TEXT NULL,
                "Voz" TEXT NULL,
                "Idioma" TEXT NULL,
                "ColorCabello" TEXT NULL
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_AvatarConfigs_Empresa_Sede"
            ON "AvatarConfigs" ("Empresa", "Sede");
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE "AvatarConfigs"
            ADD COLUMN IF NOT EXISTS "ColorCabello" TEXT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_AvatarConfigs_Empresa_Sede";
            """);

        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "AvatarConfigs";
            """);
    }
}
