using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class AppendVersionIsReleased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReleased",
                table: "LibraryVersionEntity",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            
            migrationBuilder.Sql("UPDATE LibraryVersionEntity SET IsReleased = true");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReleased",
                table: "LibraryVersionEntity");
        }
    }
}
