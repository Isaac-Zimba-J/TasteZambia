using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

public static partial class ArchiveSeeder
{
    private static Recipe BuildIfisashiRecipe() => new()
    {
        DishId = "ifisashi",
        Subtitle = "Traditional Zambian vegetable dish",
        IsVerified = true,
        ContributorName = "Chanda M.",
        ContributorLocation = "Kitwe",
        ContributorAvatarAsset = ImgAvatar,

        CulturalContext =
        [
            new() { SortOrder = 0, Text = "Ifisashi is what Zambian cooking does with what the land gives: greens from the garden, groundnuts from the field, and nothing else it does not need. There is no oil in the traditional version. The fat comes out of the pounded groundnuts themselves as the pot simmers." },
            new() { SortOrder = 1, Text = "The name is Bemba, but the dish is eaten in every province, and the greens change with what is growing. Pumpkin leaves in the wet season, cassava leaves in Luapula, bondwe wherever it comes up on its own. It is served with nshima and eaten by hand." },
            new() { SortOrder = 2, Text = "Households guard their own proportions. How coarse the groundnuts are pounded, whether tomato belongs in it at all, whether soda goes in to hold the colour of the leaves. These are the details that make one family's ifisashi recognisable from another's." },
        ],

        Ingredients =
        [
            new() { SortOrder = 0, IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 large bundles" },
            new() { SortOrder = 1, IngredientKey = "mbalala",   DisplayName = "Mbalala",   DisplaySubtitle = "Groundnuts",    Quantity = "1 cup, roasted" },
            new() { SortOrder = 2, DisplayName = "Onion",    DisplaySubtitle = "Anyezi", Quantity = "1 medium, sliced" },
            new() { SortOrder = 3, DisplayName = "Tomatoes", DisplaySubtitle = "Tomato", Quantity = "3 ripe, chopped" },
            new() { SortOrder = 4, DisplayName = "Salt",     DisplaySubtitle = "Mucele", Quantity = "To taste" },
            new() { SortOrder = 5, DisplayName = "Water",    DisplaySubtitle = "Menshi", Quantity = "1 cup" },
        ],

        Steps =
        [
            new() { Number = 1, Title = "Prepare the greens",   Body = "Strip the chibwabwa leaves from their stalks, roll them into a tight bundle and shred them finely. Rinse twice in cool water and leave to drain." },
            new() { Number = 2, Title = "Pound the groundnuts", Body = "Roast the mbalala lightly, rub off the skins, then pound in a mortar until they turn to a coarse, oily flour. A blender works, but stop before it becomes butter." },
            new() { Number = 3, Title = "Build the base",       Body = "Soften the onion and tomato in a little water over medium heat until the tomato collapses into a thick sauce. No oil is needed." },
            new() { Number = 4, Title = "Combine and simmer",   Body = "Add the greens with a splash of water and cover for five minutes, then stir the groundnut flour through. Simmer uncovered until it thickens and the oil rises to the surface." },
        ],

        Methods =
        [
            new()
            {
                Kind = MethodKind.Traditional, Heading = "Over charcoal, in a clay pot",
                Paragraphs =
                [
                    new() { SortOrder = 0, Text = "The pot is earthenware, set on a mbaula of glowing charcoal. Clay holds heat evenly and lets the relish reduce slowly without catching." },
                    new() { SortOrder = 1, Text = "Groundnuts are roasted in a dry clay pan, winnowed by hand, then pounded in a wooden mortar with a heavy pestle until the flour begins to release its oil." },
                    new() { SortOrder = 2, Text = "Greens are shredded with a knife against the palm, never chopped on a board, and stirred with a wooden mwiko." },
                ],
            },
            new()
            {
                Kind = MethodKind.Modern, Heading = "In a flat you rent abroad",
                Paragraphs =
                [
                    new() { SortOrder = 0, Text = "A heavy-based saucepan on medium heat stands in for the clay pot. Keep the lid on for the first five minutes, then off to reduce." },
                    new() { SortOrder = 1, Text = "Pulse roasted peanuts in a blender in short bursts. Unsweetened natural peanut butter works if you thin it with water first; anything with sugar in it will not." },
                    new() { SortOrder = 2, Text = "Frozen chopped spinach, collard greens or kale substitute for chibwabwa. Squeeze the water out before it goes in." },
                ],
            },
        ],

        Variations =
        [
            new() { SortOrder = 0, Place = "Copperbelt",        Description = "More tomato, and often a handful of kapenta dropped in with the greens." },
            new() { SortOrder = 1, Place = "Eastern Province",  Description = "Made with pounded cassava leaves in place of pumpkin leaves, cooked much longer." },
            new() { SortOrder = 2, Place = "Northern Province", Description = "Groundnuts pounded coarse so the texture stays rough and nutty." },
            new() { SortOrder = 3, Place = "Family style",      Description = "Some households finish with a spoon of soda to keep the greens bright; others refuse it." },
        ],
    };
}
