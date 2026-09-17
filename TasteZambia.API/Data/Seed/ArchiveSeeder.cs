using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

/// <summary>
/// The archive content, ported string-for-string from the mobile app's SeedData.cs -
/// the reviewed transcription of the design canvas. This is a cultural record: do not
/// paraphrase, reorder or "improve" any string here. SortOrder reproduces array order
/// because the design's ordering is editorial, not incidental.
/// </summary>
public static partial class ArchiveSeeder
{
    private const string ImgIfisashi = "ifisashi.png";
    private const string ImgNshima   = "spread_nshima.png";
    private const string ImgChikanda = "chikanda.png";
    private const string ImgMarket   = "market_ingredients.png";
    private const string ImgAvatar   = "avatar_chanda.png";

    public static async Task SeedAsync(TasteZambiaDbContext db, CancellationToken ct = default)
    {
        if (await db.Dishes.AnyAsync(ct)) return;   // idempotent

        db.Dishes.AddRange(BuildDishes());
        db.Ingredients.AddRange(BuildIngredients());
        db.Provinces.AddRange(BuildProvinces());
        db.Categories.AddRange(BuildCategories());
        db.Articles.AddRange(BuildArticles());
        await db.SaveChangesAsync(ct);

        db.Recipes.Add(BuildIfisashiRecipe());
        await db.SaveChangesAsync(ct);
    }

    private static List<Dish> BuildDishes() =>
    [
        new() { Id = "ifisashi", SortOrder = 0, LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy", ImageAsset = ImgIfisashi,
            PrepTime = "20 min", CookTime = "30 min",
            Description = "Leafy greens simmered in pounded groundnuts until the sauce thickens and the oil rises. Eaten with nshima across the country." },

        new() { Id = "nshima", SortOrder = 1, LocalName = "Nshima", EnglishName = "Maize meal staple",
            Region = "Pan-Zambian", TimeLabel = "30 min", Difficulty = "Easy", ImageAsset = ImgNshima,
            Description = "The staple at the centre of nearly every Zambian meal, stirred from maize meal and eaten by hand with relish." },

        new() { Id = "chikanda", SortOrder = 2, LocalName = "Chikanda", EnglishName = "Wild orchid cake",
            Region = "Northern and Muchinga", TimeLabel = "1 hr 30", Difficulty = "Medium", ImageAsset = ImgChikanda,
            Description = "Ground orchid tubers cooked with groundnut flour and chilli into a firm loaf, sliced and served cold." },

        new() { Id = "kapenta", SortOrder = 3, LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            Region = "Luapula", TimeLabel = "25 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: fried kapenta with tomato",
            Description = "Small dried fish from Lake Tanganyika and Lake Kariba, fried with onion and tomato into a salty, deeply savoury relish." },

        new() { Id = "inkoko", SortOrder = 4, LocalName = "Inkoko ya Mumushi", EnglishName = "Village chicken",
            Region = "Central", TimeLabel = "1 hr 15", Difficulty = "Medium",
            PhotoNeededCaption = "photo: village chicken stew",
            Description = "Free-range chicken cooked slowly with tomato and onion. Tougher and far more flavourful than farmed birds." },

        new() { Id = "kandolo", SortOrder = 5, LocalName = "Kandolo", EnglishName = "Sweet potatoes",
            Region = "Eastern", TimeLabel = "35 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: boiled sweet potatoes",
            Description = "Boiled or roasted and eaten at breakfast with tea, or pounded with groundnuts as a sweet afternoon dish." },

        new() { Id = "munkoyo", SortOrder = 6, LocalName = "Munkoyo", EnglishName = "Fermented root drink",
            Region = "North-Western", TimeLabel = "3 days", Difficulty = "Medium",
            PhotoNeededCaption = "photo: munkoyo in a calabash",
            Description = "Maize porridge fermented with munkoyo root into a lightly sour, faintly sweet drink served cool." },

        new() { Id = "delele", SortOrder = 7, LocalName = "Delele", EnglishName = "Okra relish",
            Region = "Southern", TimeLabel = "30 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: okra relish",
            Description = "Okra cooked with a pinch of bicarbonate of soda until it draws into a smooth relish. Divisive, and beloved." },
    ];

    private static List<Category> BuildCategories() =>
    [
        new() { SortOrder = 0, Name = "Traditional Meals",  ImageAsset = ImgNshima },
        new() { SortOrder = 1, Name = "Staple Foods" },
        new() { SortOrder = 2, Name = "Vegetable Dishes",   ImageAsset = ImgIfisashi },
        new() { SortOrder = 3, Name = "Meat Dishes" },
        new() { SortOrder = 4, Name = "Fish and Seafood" },
        new() { SortOrder = 5, Name = "Snacks",             ImageAsset = ImgChikanda },
        new() { SortOrder = 6, Name = "Desserts" },
        new() { SortOrder = 7, Name = "Traditional Drinks" },
    ];
}
