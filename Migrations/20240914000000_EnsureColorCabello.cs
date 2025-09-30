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
        migrationBuilder.AddColumn<string>(
            name: "ColorCabello",
            table: "AvatarConfigs",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ColorCabello",
            table: "AvatarConfigs");
    }
}
