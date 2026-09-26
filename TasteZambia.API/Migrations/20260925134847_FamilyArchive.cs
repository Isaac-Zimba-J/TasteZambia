using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteZambia.API.Migrations
{
    /// <inheritdoc />
    public partial class FamilyArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    LocalName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Province = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    TaughtBy = table.Column<string>(type: "text", nullable: false),
                    TaughtByOrigin = table.Column<string>(type: "text", nullable: false),
                    Story = table.Column<string>(type: "text", nullable: false),
                    TraditionalMethod = table.Column<string>(type: "text", nullable: false),
                    Privacy = table.Column<int>(type: "integer", nullable: false),
                    Transcript = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedDishId = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "family_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_invites_family_recipes_FamilyRecipeId",
                        column: x => x.FamilyRecipeId,
                        principalTable: "family_recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "family_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Relation = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_members_family_recipes_FamilyRecipeId",
                        column: x => x.FamilyRecipeId,
                        principalTable: "family_recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "family_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_notes_family_recipes_FamilyRecipeId",
                        column: x => x.FamilyRecipeId,
                        principalTable: "family_recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_invites_Code",
                table: "family_invites",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_invites_FamilyRecipeId",
                table: "family_invites",
                column: "FamilyRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_family_members_FamilyRecipeId_UserId",
                table: "family_members",
                columns: new[] { "FamilyRecipeId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_family_notes_FamilyRecipeId",
                table: "family_notes",
                column: "FamilyRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_family_recipes_OwnerId_UpdatedAt",
                table: "family_recipes",
                columns: new[] { "OwnerId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_media_assets_family_recipes_FamilyRecipeId",
                table: "media_assets",
                column: "FamilyRecipeId",
                principalTable: "family_recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_media_assets_family_recipes_FamilyRecipeId",
                table: "media_assets");

            migrationBuilder.DropTable(
                name: "family_invites");

            migrationBuilder.DropTable(
                name: "family_members");

            migrationBuilder.DropTable(
                name: "family_notes");

            migrationBuilder.DropTable(
                name: "family_recipes");
        }
    }
}
