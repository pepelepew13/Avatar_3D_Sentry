using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avatar_3D_Sentry.Migrations
{
    /// <inheritdoc />
    public partial class AddAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
                table: "AvatarConfigs");

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Empresa = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Sede = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Empresa = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Sede = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
                table: "AvatarConfigs",
                columns: new[] { "NormalizedEmpresa", "NormalizedSede" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Empresa_Sede_Tipo",
                table: "Assets",
                columns: new[] { "Empresa", "Sede", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
                table: "AvatarConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarConfigs_NormalizedEmpresa_NormalizedSede",
                table: "AvatarConfigs",
                columns: new[] { "NormalizedEmpresa", "NormalizedSede" },
                unique: true);
        }
    }
}
