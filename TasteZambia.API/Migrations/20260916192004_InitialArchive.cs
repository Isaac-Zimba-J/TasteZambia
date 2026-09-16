using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TasteZambia.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kicker = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    Meta = table.Column<string>(type: "text", nullable: false),
                    Lede = table.Column<string>(type: "text", nullable: true),
                    IsLead = table.Column<bool>(type: "boolean", nullable: false),
                    ImageAsset = table.Column<string>(type: "text", nullable: true),
                    PhotoNeededCaption = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    AudioLabel = table.Column<string>(type: "text", nullable: true),
                    AudioDuration = table.Column<string>(type: "text", nullable: true),
                    AudioProgress = table.Column<double>(type: "double precision", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ImageAsset = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dishes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocalName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Region = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TimeLabel = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImageAsset = table.Column<string>(type: "text", nullable: true),
                    PhotoNeededCaption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrepTime = table.Column<string>(type: "text", nullable: true),
                    CookTime = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dishes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocalName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    WhereFound = table.Column<string>(type: "text", nullable: false),
                    TraditionalPreparation = table.Column<string>(type: "text", nullable: false),
                    PendingLanguages = table.Column<string>(type: "text", nullable: false),
                    ImageAsset = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredients", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "provinces",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Seat = table.Column<string>(type: "text", nullable: false),
                    Blurb = table.Column<string>(type: "text", nullable: false),
                    CookingTradition = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provinces", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "article_blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleId = table.Column<string>(type: "character varying(64)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_article_blocks_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_related_dishes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleId = table.Column<string>(type: "character varying(64)", nullable: false),
                    DishId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_related_dishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_article_related_dishes_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DishId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Subtitle = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ContributorName = table.Column<string>(type: "text", nullable: false),
                    ContributorLocation = table.Column<string>(type: "text", nullable: false),
                    ContributorAvatarAsset = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recipes_dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_local_names",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngredientKey = table.Column<string>(type: "character varying(64)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient_local_names", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingredient_local_names_ingredients_IngredientKey",
                        column: x => x.IngredientKey,
                        principalTable: "ingredients",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_usages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngredientKey = table.Column<string>(type: "character varying(64)", nullable: false),
                    DishId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingredient_usages_ingredients_IngredientKey",
                        column: x => x.IngredientKey,
                        principalTable: "ingredients",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "province_foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProvinceName = table.Column<string>(type: "character varying(80)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_province_foods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_province_foods_provinces_ProvinceName",
                        column: x => x.ProvinceName,
                        principalTable: "provinces",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "province_ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProvinceName = table.Column<string>(type: "character varying(80)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_province_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_province_ingredients_provinces_ProvinceName",
                        column: x => x.ProvinceName,
                        principalTable: "provinces",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cooking_steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cooking_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cooking_steps_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "method_narratives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Heading = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_method_narratives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_method_narratives_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IngredientKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    DisplaySubtitle = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_paragraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_paragraphs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recipe_paragraphs_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "regional_variations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Place = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regional_variations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_regional_variations_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "method_paragraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodNarrativeId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_method_paragraphs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_method_paragraphs_method_narratives_MethodNarrativeId",
                        column: x => x.MethodNarrativeId,
                        principalTable: "method_narratives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_blocks_ArticleId_SortOrder",
                table: "article_blocks",
                columns: new[] { "ArticleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_article_related_dishes_ArticleId_SortOrder",
                table: "article_related_dishes",
                columns: new[] { "ArticleId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_articles_SortOrder",
                table: "articles",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_categories_SortOrder",
                table: "categories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_cooking_steps_RecipeId_Number",
                table: "cooking_steps",
                columns: new[] { "RecipeId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dishes_SortOrder",
                table: "dishes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ingredient_local_names_IngredientKey_SortOrder",
                table: "ingredient_local_names",
                columns: new[] { "IngredientKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ingredient_usages_IngredientKey_SortOrder",
                table: "ingredient_usages",
                columns: new[] { "IngredientKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ingredients_SortOrder",
                table: "ingredients",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_method_narratives_RecipeId_Kind",
                table: "method_narratives",
                columns: new[] { "RecipeId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_method_paragraphs_MethodNarrativeId_SortOrder",
                table: "method_paragraphs",
                columns: new[] { "MethodNarrativeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_province_foods_ProvinceName_SortOrder",
                table: "province_foods",
                columns: new[] { "ProvinceName", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_province_ingredients_ProvinceName_SortOrder",
                table: "province_ingredients",
                columns: new[] { "ProvinceName", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_provinces_SortOrder",
                table: "provinces",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_ingredients_RecipeId_SortOrder",
                table: "recipe_ingredients",
                columns: new[] { "RecipeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_recipe_paragraphs_RecipeId_SortOrder",
                table: "recipe_paragraphs",
                columns: new[] { "RecipeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_recipes_DishId",
                table: "recipes",
                column: "DishId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regional_variations_RecipeId_SortOrder",
                table: "regional_variations",
                columns: new[] { "RecipeId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_blocks");

            migrationBuilder.DropTable(
                name: "article_related_dishes");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "cooking_steps");

            migrationBuilder.DropTable(
                name: "ingredient_local_names");

            migrationBuilder.DropTable(
                name: "ingredient_usages");

            migrationBuilder.DropTable(
                name: "method_paragraphs");

            migrationBuilder.DropTable(
                name: "province_foods");

            migrationBuilder.DropTable(
                name: "province_ingredients");

            migrationBuilder.DropTable(
                name: "recipe_ingredients");

            migrationBuilder.DropTable(
                name: "recipe_paragraphs");

            migrationBuilder.DropTable(
                name: "regional_variations");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "method_narratives");

            migrationBuilder.DropTable(
                name: "provinces");

            migrationBuilder.DropTable(
                name: "recipes");

            migrationBuilder.DropTable(
                name: "dishes");
        }
    }
}
