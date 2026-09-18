using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TasteZambia.API.Migrations
{
    /// <inheritdoc />
    public partial class Contributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContributionId",
                table: "dishes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provenance",
                table: "dishes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LocalName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EnglishDescription = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Province = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MealType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: false),
                    CulturalSignificance = table.Column<string>(type: "text", nullable: false),
                    TraditionalMethod = table.Column<string>(type: "text", nullable: false),
                    TaughtBy = table.Column<string>(type: "text", nullable: false),
                    TaughtByOrigin = table.Column<string>(type: "text", nullable: false),
                    CreditTeacher = table.Column<bool>(type: "boolean", nullable: false),
                    ContributorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContributorLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedDishId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contributions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contribution_ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IngredientKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplaySubtitle = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contribution_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contribution_ingredients_contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contribution_steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contribution_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contribution_steps_contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flagged_fields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Field = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    CurrentValue = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flagged_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_flagged_fields_contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_events_contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "contributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_ContributionId",
                table: "dishes",
                column: "ContributionId",
                unique: true,
                filter: "\"ContributionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_contribution_ingredients_ContributionId",
                table: "contribution_ingredients",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_contribution_steps_ContributionId",
                table: "contribution_steps",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_contributions_Status",
                table: "contributions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_contributions_UserId_SubmittedAt",
                table: "contributions",
                columns: new[] { "UserId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_flagged_fields_ContributionId",
                table: "flagged_fields",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_review_events_ContributionId_At",
                table: "review_events",
                columns: new[] { "ContributionId", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contribution_ingredients");

            migrationBuilder.DropTable(
                name: "contribution_steps");

            migrationBuilder.DropTable(
                name: "flagged_fields");

            migrationBuilder.DropTable(
                name: "review_events");

            migrationBuilder.DropTable(
                name: "contributions");

            migrationBuilder.DropIndex(
                name: "IX_dishes_ContributionId",
                table: "dishes");

            migrationBuilder.DropColumn(
                name: "ContributionId",
                table: "dishes");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "dishes");
        }
    }
}
