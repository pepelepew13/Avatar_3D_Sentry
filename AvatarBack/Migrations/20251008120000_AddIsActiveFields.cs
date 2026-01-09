using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avatar_3D_Sentry.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AvatarConfigs",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AvatarConfigs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
