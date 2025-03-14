using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AE.PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddHashPropertyToAppVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "AppVersions",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "AppVersions");
        }
    }
}
