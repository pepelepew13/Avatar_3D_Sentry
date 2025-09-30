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

        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"AvatarConfigs\" ADD COLUMN IF NOT EXISTS \"ColorCabello\" TEXT NULL;");
        }
        else
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorCabello",
                table: "AvatarConfigs",
                type: "TEXT",
                nullable: true);
        }

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {

        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return;
        }


        migrationBuilder.DropColumn(
            name: "ColorCabello",
            table: "AvatarConfigs");
    }
}
