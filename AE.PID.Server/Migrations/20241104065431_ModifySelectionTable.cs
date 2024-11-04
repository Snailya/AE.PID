using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AE.PID.Server.Migrations
{
    /// <inheritdoc />
    public partial class ModifySelectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialSelections");

            migrationBuilder.CreateTable(
                name: "MaterialRecommendationCollectionFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedRecommendationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRecommendationCollectionFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRecommendationCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Context_ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Context_FunctionZone = table.Column<string>(type: "TEXT", nullable: false),
                    Context_FunctionGroup = table.Column<string>(type: "TEXT", nullable: false),
                    Context_FunctionElement = table.Column<string>(type: "TEXT", nullable: false),
                    Context_MaterialLocationType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRecommendationCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMaterialSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Context_ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Context_FunctionZone = table.Column<string>(type: "TEXT", nullable: false),
                    Context_FunctionGroup = table.Column<string>(type: "TEXT", nullable: false),
                    Context_FunctionElement = table.Column<string>(type: "TEXT", nullable: false),
                    Context_MaterialLocationType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMaterialSelections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRecommendation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    MaterialRecommendationCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRecommendation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRecommendation_MaterialRecommendationCollections_MaterialRecommendationCollectionId",
                        column: x => x.MaterialRecommendationCollectionId,
                        principalTable: "MaterialRecommendationCollections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRecommendation_MaterialRecommendationCollectionId",
                table: "MaterialRecommendation",
                column: "MaterialRecommendationCollectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialRecommendation");

            migrationBuilder.DropTable(
                name: "MaterialRecommendationCollectionFeedbacks");

            migrationBuilder.DropTable(
                name: "UserMaterialSelections");

            migrationBuilder.DropTable(
                name: "MaterialRecommendationCollections");

            migrationBuilder.CreateTable(
                name: "MaterialSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaterialCode = table.Column<string>(type: "TEXT", nullable: false),
                    MaterialLocationType = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSelections", x => x.Id);
                });
        }
    }
}
