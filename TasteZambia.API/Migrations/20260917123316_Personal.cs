using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TasteZambia.API.Migrations
{
    /// <inheritdoc />
    public partial class Personal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cook_progress",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DishId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cook_progress", x => new { x.UserId, x.DishId, x.StepNumber });
                    table.ForeignKey(
                        name: "FK_cook_progress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "onboarding_choices",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Who = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Tastes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OfflineEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StoryNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_onboarding_choices", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_onboarding_choices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_dishes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DishId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsSaved = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_dishes", x => new { x.UserId, x.DishId });
                    table.ForeignKey(
                        name: "FK_saved_dishes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Languages = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarAsset = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cook_progress");

            migrationBuilder.DropTable(
                name: "onboarding_choices");

            migrationBuilder.DropTable(
                name: "saved_dishes");

            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
