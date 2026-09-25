using TasteZambia.Core.Models;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Data;

/// <summary>
/// The archive content, transcribed verbatim from the approved design canvas.
/// This is a cultural record: do not paraphrase, reorder or "improve" any string here.
/// </summary>
public static class SeedData
{
    public const string ImgIfisashi = "ifisashi.png";
    public const string ImgNshima   = "spread_nshima.png";
    public const string ImgChikanda = "chikanda.png";
    public const string ImgMarket   = "market_ingredients.png";
    public const string ImgAvatar   = "avatar_chanda.png";

    public static readonly IReadOnlyList<Dish> Dishes =
    [
        new() { Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy", ImageAsset = ImgIfisashi,
            PrepTime = "20 min", CookTime = "30 min",
            Description = "Leafy greens simmered in pounded groundnuts until the sauce thickens and the oil rises. Eaten with nshima across the country." },

        new() { Id = "nshima", LocalName = "Nshima", EnglishName = "Maize meal staple",
            Region = "Pan-Zambian", TimeLabel = "30 min", Difficulty = "Easy", ImageAsset = ImgNshima,
            Description = "The staple at the centre of nearly every Zambian meal, stirred from maize meal and eaten by hand with relish." },

        new() { Id = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid cake",
            Region = "Northern and Muchinga", TimeLabel = "1 hr 30", Difficulty = "Medium", ImageAsset = ImgChikanda,
            Description = "Ground orchid tubers cooked with groundnut flour and chilli into a firm loaf, sliced and served cold." },

        new() { Id = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            Region = "Luapula", TimeLabel = "25 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: fried kapenta with tomato",
            Description = "Small dried fish from Lake Tanganyika and Lake Kariba, fried with onion and tomato into a salty, deeply savoury relish." },

        new() { Id = "inkoko", LocalName = "Inkoko ya Mumushi", EnglishName = "Village chicken",
            Region = "Central", TimeLabel = "1 hr 15", Difficulty = "Medium",
            PhotoNeededCaption = "photo: village chicken stew",
            Description = "Free-range chicken cooked slowly with tomato and onion. Tougher and far more flavourful than farmed birds." },

        new() { Id = "kandolo", LocalName = "Kandolo", EnglishName = "Sweet potatoes",
            Region = "Eastern", TimeLabel = "35 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: boiled sweet potatoes",
            Description = "Boiled or roasted and eaten at breakfast with tea, or pounded with groundnuts as a sweet afternoon dish." },

        new() { Id = "munkoyo", LocalName = "Munkoyo", EnglishName = "Fermented root drink",
            Region = "North-Western", TimeLabel = "3 days", Difficulty = "Medium",
            PhotoNeededCaption = "photo: munkoyo in a calabash",
            Description = "Maize porridge fermented with munkoyo root into a lightly sour, faintly sweet drink served cool." },

        new() { Id = "delele", LocalName = "Delele", EnglishName = "Okra relish",
            Region = "Southern", TimeLabel = "30 min", Difficulty = "Easy",
            PhotoNeededCaption = "photo: okra relish",
            Description = "Okra cooked with a pinch of bicarbonate of soda until it draws into a smooth relish. Divisive, and beloved." },
    ];

    public static readonly IReadOnlyList<Category> Categories =
    [
        new("Traditional Meals", ImgNshima),
        new("Staple Foods", null),
        new("Vegetable Dishes", ImgIfisashi),
        new("Meat Dishes", null),
        new("Fish and Seafood", null),
        new("Snacks", ImgChikanda),
        new("Desserts", null),
        new("Traditional Drinks", null),
    ];

    public static readonly IReadOnlyList<Ingredient> Ingredients =
    [
        new() { Key = "chibwabwa", LocalName = "Chibwabwa", EnglishName = "Pumpkin leaves", ImageAsset = ImgMarket,
            LocalNames = [new("Bemba, Nyanja", "Chibwabwa")],
            PendingLanguages = "Tonga, Lozi, Kaonde, Lunda, Luvale",
            Description = "The young leaves and tender shoots of the pumpkin plant, picked before the fruit is taken. Sold in tied bundles at every market and grown in almost every village garden.",
            WhereFound = "Grown countrywide, most abundant in the rainy season from December to March.",
            TraditionalPreparation = "The leaves are stripped from their stalks, rolled tight and shredded fine with a knife, then rubbed between the palms with a little salt to soften them before cooking.",
            UsedInDishIds = ["ifisashi", "nshima", "delele"] },

        new() { Key = "mbalala", LocalName = "Mbalala", EnglishName = "Groundnuts",
            LocalNames = [new("Bemba", "Mbalala"), new("Nyanja", "Nsawawa")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Roasted and pounded into a coarse, oily flour that thickens relishes and supplies the fat in cooking where oil was never used.",
            WhereFound = "Eastern Province is the heartland of groundnut farming; grown in every province.",
            TraditionalPreparation = "Roasted in a clay pan over coals, winnowed by hand, then pounded in a wooden mortar until the flour begins to release its oil.",
            UsedInDishIds = ["ifisashi", "chikanda", "kandolo"] },

        new() { Key = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            LocalNames = [new("Bemba", "Kapenta"), new("Nyanja", "Kapenta")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Small freshwater sardines caught at night under lamps, sun-dried whole on racks and sold by the tin.",
            WhereFound = "Lake Tanganyika, Lake Mweru and Lake Kariba. Traded countrywide.",
            TraditionalPreparation = "Rinsed, then dry-fried without oil until crisp before tomato and onion are added.",
            UsedInDishIds = ["kapenta", "ifisashi"] },

        new() { Key = "tute", LocalName = "Tute", EnglishName = "Cassava",
            LocalNames = [new("Bemba", "Tute"), new("Luvale", "Mbombo")],
            PendingLanguages = "Tonga, Nyanja",
            Description = "A starchy root that stores in the ground for years, making it the food that carries households through a poor harvest.",
            WhereFound = "Luapula, Northern and North-Western Provinces.",
            TraditionalPreparation = "Peeled, soaked for several days to ferment and remove bitterness, then sun-dried and pounded into flour.",
            UsedInDishIds = ["nshima"] },

        new() { Key = "katapa", LocalName = "Katapa", EnglishName = "Cassava leaves",
            LocalNames = [new("Bemba", "Katapa")],
            PendingLanguages = "Luvale, Lunda, Kaonde",
            Description = "The leaves of the cassava plant, pounded rather than chopped, cooked long to soften and to drive off bitterness.",
            WhereFound = "Luapula and Northern Provinces.",
            TraditionalPreparation = "Pounded in a mortar with a little water, then simmered a long while with groundnuts.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid tubers",
            LocalNames = [new("Bemba", "Chikanda")],
            PendingLanguages = "Nyanja, Tonga",
            Description = "Tubers of wild terrestrial orchids, foraged from grassland. Now under pressure from overharvesting.",
            WhereFound = "Northern, Muchinga and Eastern Provinces.",
            TraditionalPreparation = "Peeled, boiled and pounded, then cooked with groundnut flour, soda and chilli until it sets firm.",
            UsedInDishIds = ["chikanda"] },

        new() { Key = "impwa", LocalName = "Impwa", EnglishName = "Garden eggs",
            LocalNames = [new("Bemba", "Impwa"), new("Nyanja", "Sumu")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Small pale African eggplants, firmer and more bitter than the purple kind, eaten raw with salt or cooked into relish.",
            WhereFound = "Grown countrywide in home gardens.",
            TraditionalPreparation = "Sliced and cooked briefly with tomato so the bitterness stays present.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "bondwe", LocalName = "Bondwe", EnglishName = "Amaranth greens",
            LocalNames = [new("Bemba", "Bondwe"), new("Nyanja", "Bonongwe")],
            PendingLanguages = "Tonga, Lozi",
            Description = "A wild green that comes up on its own in cultivated ground. Gathered, not planted.",
            WhereFound = "Countrywide, following the rains.",
            TraditionalPreparation = "Gathered young, cooked with groundnuts or with a little soda to hold the colour.",
            UsedInDishIds = ["ifisashi"] },

        new() { Key = "kandolo", LocalName = "Kandolo", EnglishName = "Sweet potatoes",
            LocalNames = [new("Bemba", "Kandolo"), new("Nyanja", "Mbatatesi")],
            PendingLanguages = "Tonga, Lozi",
            Description = "Boiled or roasted in the coals, eaten with tea in the morning or pounded with groundnuts.",
            WhereFound = "Eastern and Northern Provinces.",
            TraditionalPreparation = "Buried in hot ash and coals to roast slowly in the skin.",
            UsedInDishIds = ["kandolo"] },
    ];

    public static readonly IReadOnlyList<Province> Provinces =
    [
        new() { Name = "Central", Seat = "Kabwe",
            Blurb = "Maize country, and the province most associated with free-range village chicken cooked slowly over an open fire.",
            SignatureFoods = ["Inkoko ya Mumushi", "Nshima", "Ifisashi"],
            CommonIngredients = ["Maize", "Groundnuts", "Village chicken"],
            CookingTradition = "Chicken is cooked whole in a clay pot set directly in the coals, the broth reduced rather than thickened." },

        new() { Name = "Copperbelt", Seat = "Ndola",
            Blurb = "The most urban food culture in the country. Mining towns drew people from every province, and the cooking absorbed all of it.",
            SignatureFoods = ["Kapenta", "Ifisashi", "Nshima"],
            CommonIngredients = ["Kapenta", "Tomatoes", "Cooking oil"],
            CookingTradition = "Relishes here carry more tomato and more oil than their village versions, a change that came with wage kitchens." },

        new() { Name = "Eastern", Seat = "Chipata",
            Blurb = "The groundnut heartland. Almost every relish in the province passes through pounded groundnuts at some point.",
            SignatureFoods = ["Chikanda", "Kandolo", "Ifisashi"],
            CommonIngredients = ["Groundnuts", "Sweet potatoes", "Pumpkin leaves"],
            CookingTradition = "Groundnuts are roasted in a dry clay pan, winnowed in a flat basket, then pounded in a wooden mortar." },

        new() { Name = "Luapula", Seat = "Mansa",
            Blurb = "Fish and cassava. The Luapula river, Lake Mweru and the Bangweulu swamps supply most of what is eaten here.",
            SignatureFoods = ["Kapenta", "Katapa", "Nshima"],
            CommonIngredients = ["Cassava", "Kapenta", "Cassava leaves"],
            CookingTradition = "Cassava is soaked for days to ferment, sun-dried on rocks, then pounded into flour for nshima." },

        new() { Name = "Lusaka", Seat = "Lusaka",
            Blurb = "Every province cooks here. The capital is where regional dishes meet, get shortened for time, and turn into street food.",
            SignatureFoods = ["Nshima", "Kapenta", "Delele"],
            CommonIngredients = ["Maize meal", "Tomatoes", "Rape leaves"],
            CookingTradition = "Market kitchens serve nshima with a choice of relishes from four or five provinces at one counter." },

        new() { Name = "Muchinga", Seat = "Chinsali",
            Blurb = "Foraged food. Wild orchid tubers and rainy-season mushrooms are gathered here in quantities found almost nowhere else.",
            SignatureFoods = ["Chikanda", "Nshima", "Ifisashi"],
            CommonIngredients = ["Wild orchid tubers", "Bowa mushrooms", "Millet"],
            CookingTradition = "Mushrooms are dried on grass mats above the cooking fire so they keep through the dry season." },

        new() { Name = "Northern", Seat = "Kasama",
            Blurb = "Bemba cooking at its source. Cassava, groundnuts and pounded leaves, with long cooking times and little added fat.",
            SignatureFoods = ["Ifisashi", "Katapa", "Chikanda"],
            CommonIngredients = ["Cassava", "Groundnuts", "Cassava leaves"],
            CookingTradition = "Leaves are pounded in a mortar rather than chopped, then simmered long enough to lose all bitterness." },

        new() { Name = "North-Western", Seat = "Solwezi",
            Blurb = "Cassava, wild fruit and honey. Home of munkoyo, the fermented root drink served cool from a calabash.",
            SignatureFoods = ["Munkoyo", "Nshima", "Katapa"],
            CommonIngredients = ["Munkoyo root", "Cassava", "Wild honey"],
            CookingTradition = "Munkoyo root is crushed and stirred into cooled maize porridge, then left to ferment for two or three days." },

        new() { Name = "Southern", Seat = "Choma",
            Blurb = "Cattle country. Soured milk sits alongside the relish here in a way it does not elsewhere in Zambia.",
            SignatureFoods = ["Delele", "Nshima", "Ifisashi"],
            CommonIngredients = ["Maize", "Soured milk", "Okra"],
            CookingTradition = "Fresh milk is left to sour in a covered gourd and eaten with nshima, needing no cooking at all." },

        new() { Name = "Western", Seat = "Mongu",
            Blurb = "The Barotse floodplain. Fish from the Zambezi, rice grown on the plain, and wild fruit from the sandveld.",
            SignatureFoods = ["Nshima", "Kapenta", "Delele"],
            CommonIngredients = ["Freshwater fish", "Rice", "Wild fruit"],
            CookingTradition = "Fish is smoked over a slow fire on raised racks, a method that carries it through the flood season." },
    ];

    /// <summary>Names appearing in <see cref="Province.SignatureFoods"/> that are not seeded dishes.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExtraFoodSubtitles =
        new Dictionary<string, string> { ["Katapa"] = "Cassava leaf relish" };

    public static readonly IReadOnlyList<string> Filters =
        ["All", "Province", "Main Ingredient", "Difficulty", "Cooking Time", "Vegetarian", "Traditional", "Meal Type"];

    public static readonly IReadOnlyList<string> ShareSteps =
        ["Recipe basics", "Ingredients and method", "Cultural background", "Submit for review"];

    public static readonly IReadOnlyList<string> FamilySteps =
        ["The recipe", "Who taught you", "The story", "Who can see it"];

    public static RecipeDetail IfisashiRecipe() => new()
    {
        Dish = Dishes[0],
        Subtitle = "Traditional Zambian vegetable dish",
        IsVerified = true,
        CulturalContext =
        [
            "Ifisashi is what Zambian cooking does with what the land gives: greens from the garden, groundnuts from the field, and nothing else it does not need. There is no oil in the traditional version. The fat comes out of the pounded groundnuts themselves as the pot simmers.",
            "The name is Bemba, but the dish is eaten in every province, and the greens change with what is growing. Pumpkin leaves in the wet season, cassava leaves in Luapula, bondwe wherever it comes up on its own. It is served with nshima and eaten by hand.",
            "Households guard their own proportions. How coarse the groundnuts are pounded, whether tomato belongs in it at all, whether soda goes in to hold the colour of the leaves. These are the details that make one family's ifisashi recognisable from another's.",
        ],
        Ingredients =
        [
            new() { IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 large bundles" },
            new() { IngredientKey = "mbalala",   DisplayName = "Mbalala",   DisplaySubtitle = "Groundnuts",    Quantity = "1 cup, roasted" },
            new() { DisplayName = "Onion",    DisplaySubtitle = "Anyezi", Quantity = "1 medium, sliced" },
            new() { DisplayName = "Tomatoes", DisplaySubtitle = "Tomato", Quantity = "3 ripe, chopped" },
            new() { DisplayName = "Salt",     DisplaySubtitle = "Mucele", Quantity = "To taste" },
            new() { DisplayName = "Water",    DisplaySubtitle = "Menshi", Quantity = "1 cup" },
        ],
        Steps =
        [
            new(1, "Prepare the greens", "Strip the chibwabwa leaves from their stalks, roll them into a tight bundle and shred them finely. Rinse twice in cool water and leave to drain."),
            new(2, "Pound the groundnuts", "Roast the mbalala lightly, rub off the skins, then pound in a mortar until they turn to a coarse, oily flour. A blender works, but stop before it becomes butter."),
            new(3, "Build the base", "Soften the onion and tomato in a little water over medium heat until the tomato collapses into a thick sauce. No oil is needed."),
            new(4, "Combine and simmer", "Add the greens with a splash of water and cover for five minutes, then stir the groundnut flour through. Simmer uncovered until it thickens and the oil rises to the surface."),
        ],
        TraditionalMethod = new("Over charcoal, in a clay pot",
        [
            "The pot is earthenware, set on a mbaula of glowing charcoal. Clay holds heat evenly and lets the relish reduce slowly without catching.",
            "Groundnuts are roasted in a dry clay pan, winnowed by hand, then pounded in a wooden mortar with a heavy pestle until the flour begins to release its oil.",
            "Greens are shredded with a knife against the palm, never chopped on a board, and stirred with a wooden mwiko.",
        ]),
        ModernMethod = new("In a flat you rent abroad",
        [
            "A heavy-based saucepan on medium heat stands in for the clay pot. Keep the lid on for the first five minutes, then off to reduce.",
            "Pulse roasted peanuts in a blender in short bursts. Unsweetened natural peanut butter works if you thin it with water first; anything with sugar in it will not.",
            "Frozen chopped spinach, collard greens or kale substitute for chibwabwa. Squeeze the water out before it goes in.",
        ]),
        Variations =
        [
            new("Copperbelt", "More tomato, and often a handful of kapenta dropped in with the greens."),
            new("Eastern Province", "Made with pounded cassava leaves in place of pumpkin leaves, cooked much longer."),
            new("Northern Province", "Groundnuts pounded coarse so the texture stays rough and nutty."),
            new("Family style", "Some households finish with a spoon of soda to keep the greens bright; others refuse it."),
        ],
        Contributor = new("Chanda M.", "Kitwe", ImgAvatar),
    };

    public static readonly IReadOnlyList<Article> Articles =
    [
        new() { Id = "nshima", Kicker = "Archive essay", Title = "The History of Nshima",
            Author = "Dr. Mutale Chileshe", Meta = "8 min read · Pan-Zambian", IsLead = true,
            Lede = "The dish at the centre of every Zambian meal is younger than most people assume.",
            PhotoNeededCaption = "photo: woman stirring nshima with a mwiko",
            Audio = new("Listen in Bemba", "12:40", 0.18),
            RelatedDishIds = ["nshima"],
            Body =
            [
                new(ArticleBlockKind.Lede, "The dish at the centre of every Zambian meal is younger than most people assume. Maize arrived in this part of Africa through trade, and for a long time it sat alongside older grains rather than replacing them."),
                new(ArticleBlockKind.Paragraph, "Before maize, the staples were millet and sorghum. They were pounded, sifted and stirred into a thick porridge by the same method still used today: water brought to the boil, a thin gruel made first, then more meal worked in with a wooden mwiko until the mixture pulls away from the sides of the pot."),
                new(ArticleBlockKind.Paragraph, "What changed was the grain, not the technique. Maize gave a higher yield and a whiter meal, and through the colonial period it was actively promoted over the older grains. Within two or three generations it had become the default, and nshima came to mean maize nshima specifically."),
                new(ArticleBlockKind.PullQuote, "The older grains never disappeared. In parts of Muchinga and North-Western Province, millet nshima is still what is served when the meal matters."),
                new(ArticleBlockKind.Paragraph, "Cassava followed a similar path in the north. In Luapula and Northern Province, nshima made from fermented, sun-dried cassava flour is the everyday version, and maize is the visitor. The name stays the same; the flour, the colour and the taste do not."),
                new(ArticleBlockKind.Paragraph, "This is why the archive records nshima as a family of dishes rather than one recipe. What is constant is the method, the mwiko, and the fact that it is never eaten alone."),
            ] },

        new() { Id = "groundnuts", Kicker = "Archive essay", Title = "Why Groundnuts Anchor Zambian Cooking",
            Author = "Namakau Sitali", Meta = "6 min read · Eastern Province",
            PhotoNeededCaption = "photo: groundnuts being winnowed" },

        new() { Id = "methods", Kicker = "Technique", Title = "Traditional Cooking Methods in Zambia",
            Author = "Archive team", Meta = "11 min read · Countrywide",
            PhotoNeededCaption = "photo: clay pot on a mbaula" },

        new() { Id = "growingup", Kicker = "Community voices", Title = "Foods We Ate Growing Up",
            Author = "12 contributors", Meta = "Audio and text · Open for submissions",
            PhotoNeededCaption = "photo: family eating together" },

        new() { Id = "passeddown", Kicker = "Family archive", Title = "Recipes Passed Down Through Generations",
            Author = "Archive team", Meta = "9 min read · 24 family recipes",
            PhotoNeededCaption = "photo: handwritten recipe book" },

        new() { Id = "beforekitchens", Kicker = "Archive essay", Title = "Cooking Before the Modern Kitchen",
            Author = "Dr. Mutale Chileshe", Meta = "7 min read · Countrywide",
            PhotoNeededCaption = "photo: open fire cooking" },
    ];

    /// <summary>Compact story teasers shown on Home.</summary>
    public static readonly IReadOnlyList<(string Title, string Meta)> HomeStories =
    [
        ("The History of Nshima", "Archive essay · 8 min read"),
        ("Why Groundnuts Anchor Zambian Cooking", "Archive essay · 6 min read"),
        ("Foods We Ate Growing Up", "Community voices · 12 contributions"),
    ];

    /// <summary>A reader who has not written anything yet. Real counts come from the account.</summary>
    public static readonly UserProfile Profile = new()
    {
        Name = "Taste Zambia reader", Location = "", Languages = "",
        AvatarAsset = "",
        CookedCount = 0, FavouriteCount = 0, ContributedCount = 0, PreservedCount = 0,
    };

    public static readonly IReadOnlyList<RecipeCollection> Collections =
    [
        new("My Favourite Zambian Foods", "Nothing yet", "#A3452A"),
        new("Recipes I Want to Try",      "Nothing yet", "#C07F1E"),
        new("Recipes I've Cooked",        "Nothing yet", "#2F6A4D"),
        new("My Family Recipes",          "Nothing preserved yet", "#17402F"),
    ];

    public static readonly IReadOnlyList<string> SettingsRows =
        ["Language — English", "Offline recipes", "Notifications", "Account and privacy", "About the archive"];

    public static readonly IReadOnlyList<ReviewStage> ReviewPipeline =
    [
        new("Submitted", "Your recipe leaves your device and enters the review queue.", true),
        new("Checked against regional sources", "The archive team compares names, ingredients and method against records for the province you named.", true),
        new("Contributor contacted if anything is unclear", "We ask rather than edit. Nothing is changed without your reply.", false),
        new("Published and credited", "Credited to you, and to the person who taught you, in the public archive.", false),
    ];
    public static readonly IReadOnlyList<RecipeDraft> Drafts =
    [
        new("Chibwabwa na Mbalala", 85, "Needs one more cooking step",           "Edited 2 hours ago", "#2F6A4D"),
        new("Munkoyo",              40, "Needs photos and the fermenting times", "Edited 4 days ago",  "#C07F1E"),
        new("Ubwali bwa Tute",      15, "Only the name and province so far",     "Edited 3 weeks ago", "#A3452A"),
    ];

    public static readonly IReadOnlyList<ReviewStep> SubmissionTimeline =
    [
        new("Submitted", "2 Sep 2026",
            "Left your device and entered the queue.", ReviewState.Done, true),
        new("Read by the archive team", "3 Sep 2026",
            "Reviewed by Namakau Sitali, Northern Province records.", ReviewState.Done, true),
        new("Checked against regional sources", "In progress",
            "Names, ingredients and method compared against the provincial record.", ReviewState.InProgress, true),
        new("Published and credited", "Expected mid-September",
            "Credited to you and to whoever taught you the dish.", ReviewState.Pending, false),
    ];

    public static readonly IReadOnlyList<FlaggedField> FlaggedFields =
    [
        new("Local name",
            "Is this dish called Chibwabwa na Mbalala in Mungwi specifically, or is that the Kasama town name? Our Northern records have both.",
            "Chibwabwa na Mbalala"),
        new("Cooking step 2",
            "You say pound until the oil shows. Roughly how long does that take by hand? Readers abroad will be using a blender.",
            "Pound the groundnuts until the oil starts to show."),
    ];

    public const string PublishedCredit =
        "Recorded by Chanda Mwaba, Kitwe. As taught by Banakulu Mwaba of Mungwi, Northern Province. Verified against provincial records, September 2026.";
    public static readonly IReadOnlyList<SavedEntry> Saved =
    [
        new("ifisashi", "Saved March 2026",    "The one I cook most"),
        new("chikanda", "Saved March 2026",    ""),
        new("nshima",   "Saved January 2026",  ""),
        new("kapenta",  "Saved January 2026",  "Mum makes this better"),
        new("inkoko",   "Saved December 2025", ""),
        new("delele",   "Saved December 2025", ""),
    ];

    public static readonly IReadOnlyList<WishlistEntry> Wishlist =
    [
        new("munkoyo",  "Grandfather used to make this. No one in the family wrote it down."),
        new("chikanda", "Want to try it before buying it at the market again."),
        new("kandolo",  ""),
        new("inkoko",   "For when the family visits at Christmas."),
        new("delele",   ""),
    ];

    public static readonly IReadOnlyList<CookedEntry> Cooked =
    [
        new("nshima",   "Cooked 31 times", "Yesterday",     ""),
        new("ifisashi", "Cooked 18 times", "Last week",     "Less water than the recipe says. Mine came out thin the first time."),
        new("kapenta",  "Cooked 7 times",  "Two weeks ago", ""),
        new("kandolo",  "Cooked 4 times",  "August",        "Roasting beats boiling."),
        new("delele",   "Cooked 2 times",  "July",          ""),
    ];

    public static readonly IReadOnlyList<LanguageStatus> LanguageStatuses =
    [
        new("English", "Current interface language",           true),
        new("Bemba",   "Recipe and ingredient names available", false),
        new("Nyanja",  "Recipe and ingredient names available", false),
        new("Tonga",   "In progress · names being collected",   false),
        new("Lozi",    "In progress · names being collected",   false),
        new("Kaonde",  "Not started",                           false),
        new("Lunda",   "Not started",                           false),
        new("Luvale",  "Not started",                           false),
    ];

    public static readonly IReadOnlyList<SettingToggle> SettingToggles =
    [
        new("Keep recipes for offline cooking", "Saved and family recipes stay on this device. 38 MB used.", true),
        new("Download photos too",              "Uses more storage. Turn off on a limited data plan.",       true),
        new("New stories from the archive",     "About twice a month.",                                      true),
        new("Replies from the archive team",    "When a reviewer asks you something.",                       true),
        new("Family recipe activity",           "When a relative adds a note or a photo.",                   false),
    ];

    /// <summary>The design's walkthrough submission - Chibwabwa na Mbalala - kept for tests and previews.</summary>
    public static ContributionDraft WalkthroughShareDraft() => new()
    {
        LocalName = "Chibwabwa na Mbalala",
        EnglishDescription = "Pumpkin leaves cooked with pounded groundnuts and nothing else",
        Province = "Northern",
        MealType = "Relish",
        Ingredients =
        [
            new() { IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 bundles" },
            new() { IngredientKey = "mbalala",   DisplayName = "Mbalala",   DisplaySubtitle = "Groundnuts",    Quantity = "1 cup" },
            new() { DisplayName = "Salt", DisplaySubtitle = "Mucele", Quantity = "To taste" },
        ],
        Steps =
        [
            "Shred the leaves fine and rinse them twice.",
            "Pound the groundnuts until the oil starts to show.",
        ],
        Origin = "Cooked in Mungwi and the villages around Kasama. It is a rainy-season dish because that is when the pumpkin leaves are at their best.",
        CulturalSignificance = "This is the relish cooked when there is no money for meat, and it is not thought of as a lesser meal. It is what most people mean when they talk about eating well at home.",
        TraditionalMethod = "Clay pot on charcoal. Groundnuts pounded in a mortar, not blended.",
        CreditTeacher = true,
    };
}
