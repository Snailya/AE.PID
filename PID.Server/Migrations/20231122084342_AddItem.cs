using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryItemEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    BaseId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItemEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItemEntity_LibraryVersionEntity_VersionId",
                        column: x => x.VersionId,
                        principalTable: "LibraryVersionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItemEntity_VersionId",
                table: "LibraryItemEntity",
                column: "VersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryItemEntity");
        }
    }
}
