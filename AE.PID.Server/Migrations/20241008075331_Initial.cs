using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AE.PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: false),
                    PhysicalFile = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Masters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Masters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepositorySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositorySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stencils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stencils", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: false),
                    IsReleased = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    LibraryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryVersions_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterContentSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseId = table.Column<string>(type: "TEXT", nullable: false),
                    UniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    LineStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    FillStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    TextStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    MasterElement = table.Column<string>(type: "TEXT", nullable: false),
                    MasterDocument = table.Column<string>(type: "TEXT", nullable: false),
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterContentSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterContentSnapshots_Masters_MasterId",
                        column: x => x.MasterId,
                        principalTable: "Masters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StencilSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhysicalFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StencilId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StencilSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StencilSnapshots_Stencils_StencilId",
                        column: x => x.StencilId,
                        principalTable: "Stencils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UniqueId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    BaseId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    LibraryVersionEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    LibraryVersionItemXmlId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_LibraryVersions_LibraryVersionEntityId",
                        column: x => x.LibraryVersionEntityId,
                        principalTable: "LibraryVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryVersionRepositorySnapshot",
                columns: table => new
                {
                    LibrarySnapshotsId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryVersionRepositorySnapshot", x => new { x.LibrarySnapshotsId, x.VersionsId });
                    table.ForeignKey(
                        name: "FK_LibraryVersionRepositorySnapshot_LibraryVersions_VersionsId",
                        column: x => x.VersionsId,
                        principalTable: "LibraryVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryVersionRepositorySnapshot_RepositorySnapshots_LibrarySnapshotsId",
                        column: x => x.LibrarySnapshotsId,
                        principalTable: "RepositorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterContentSnapshotStencilSnapshot",
                columns: table => new
                {
                    MasterContentSnapshotsId = table.Column<int>(type: "INTEGER", nullable: false),
                    StencilSnapshotsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterContentSnapshotStencilSnapshot", x => new { x.MasterContentSnapshotsId, x.StencilSnapshotsId });
                    table.ForeignKey(
                        name: "FK_MasterContentSnapshotStencilSnapshot_MasterContentSnapshots_MasterContentSnapshotsId",
                        column: x => x.MasterContentSnapshotsId,
                        principalTable: "MasterContentSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterContentSnapshotStencilSnapshot_StencilSnapshots_StencilSnapshotsId",
                        column: x => x.StencilSnapshotsId,
                        principalTable: "StencilSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibraryVersionItemXML",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LineStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    FillStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    TextStyleName = table.Column<string>(type: "TEXT", nullable: false),
                    MasterElement = table.Column<string>(type: "TEXT", nullable: false),
                    MasterDocument = table.Column<string>(type: "TEXT", nullable: false),
                    LibraryVersionItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryVersionItemXML", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryVersionItemXML_LibraryItem_LibraryVersionItemId",
                        column: x => x.LibraryVersionItemId,
                        principalTable: "LibraryItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_LibraryVersionEntityId",
                table: "LibraryItem",
                column: "LibraryVersionEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryVersionItemXML_LibraryVersionItemId",
                table: "LibraryVersionItemXML",
                column: "LibraryVersionItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryVersionRepositorySnapshot_VersionsId",
                table: "LibraryVersionRepositorySnapshot",
                column: "VersionsId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryVersions_LibraryId",
                table: "LibraryVersions",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterContentSnapshots_MasterId",
                table: "MasterContentSnapshots",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterContentSnapshotStencilSnapshot_StencilSnapshotsId",
                table: "MasterContentSnapshotStencilSnapshot",
                column: "StencilSnapshotsId");

            migrationBuilder.CreateIndex(
                name: "IX_StencilSnapshots_StencilId",
                table: "StencilSnapshots",
                column: "StencilId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppVersions");

            migrationBuilder.DropTable(
                name: "LibraryVersionItemXML");

            migrationBuilder.DropTable(
                name: "LibraryVersionRepositorySnapshot");

            migrationBuilder.DropTable(
                name: "MasterContentSnapshotStencilSnapshot");

            migrationBuilder.DropTable(
                name: "LibraryItem");

            migrationBuilder.DropTable(
                name: "RepositorySnapshots");

            migrationBuilder.DropTable(
                name: "MasterContentSnapshots");

            migrationBuilder.DropTable(
                name: "StencilSnapshots");

            migrationBuilder.DropTable(
                name: "LibraryVersions");

            migrationBuilder.DropTable(
                name: "Masters");

            migrationBuilder.DropTable(
                name: "Stencils");

            migrationBuilder.DropTable(
                name: "Libraries");
        }
    }
}
