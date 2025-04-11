using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AE.PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class VersionChannelSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "AppVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "AppVersions");
        }
    }
}
