using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class AppendItemInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FillStyleName",
                table: "LibraryItemEntity",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LineStyleName",
                table: "LibraryItemEntity",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MasterDocument",
                table: "LibraryItemEntity",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MasterElement",
                table: "LibraryItemEntity",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TextStyleName",
                table: "LibraryItemEntity",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FillStyleName",
                table: "LibraryItemEntity");

            migrationBuilder.DropColumn(
                name: "LineStyleName",
                table: "LibraryItemEntity");

            migrationBuilder.DropColumn(
                name: "MasterDocument",
                table: "LibraryItemEntity");

            migrationBuilder.DropColumn(
                name: "MasterElement",
                table: "LibraryItemEntity");

            migrationBuilder.DropColumn(
                name: "TextStyleName",
                table: "LibraryItemEntity");
        }
    }
}
