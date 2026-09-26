using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteZambia.API.Migrations
{
    /// <inheritdoc />
    public partial class Media : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FamilyRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_ContributionId",
                table: "media_assets",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_FamilyRecipeId",
                table: "media_assets",
                column: "FamilyRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_UserId",
                table: "media_assets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
