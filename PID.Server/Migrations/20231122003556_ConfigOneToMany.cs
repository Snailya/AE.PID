using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class ConfigOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryVersionEntity_Libraries_LibraryEntityId",
                table: "LibraryVersionEntity");

            migrationBuilder.DropIndex(
                name: "IX_LibraryVersionEntity_LibraryEntityId",
                table: "LibraryVersionEntity");

            migrationBuilder.DropColumn(
                name: "LibraryEntityId",
                table: "LibraryVersionEntity");

            migrationBuilder.AddColumn<int>(
                name: "LibraryId",
                table: "LibraryVersionEntity",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryVersionEntity_LibraryId",
                table: "LibraryVersionEntity",
                column: "LibraryId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryVersionEntity_Libraries_LibraryId",
                table: "LibraryVersionEntity",
                column: "LibraryId",
                principalTable: "Libraries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryVersionEntity_Libraries_LibraryId",
                table: "LibraryVersionEntity");

            migrationBuilder.DropIndex(
                name: "IX_LibraryVersionEntity_LibraryId",
                table: "LibraryVersionEntity");

            migrationBuilder.DropColumn(
                name: "LibraryId",
                table: "LibraryVersionEntity");

            migrationBuilder.AddColumn<int>(
                name: "LibraryEntityId",
                table: "LibraryVersionEntity",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryVersionEntity_LibraryEntityId",
                table: "LibraryVersionEntity",
                column: "LibraryEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryVersionEntity_Libraries_LibraryEntityId",
                table: "LibraryVersionEntity",
                column: "LibraryEntityId",
                principalTable: "Libraries",
                principalColumn: "Id");
        }
    }
}
