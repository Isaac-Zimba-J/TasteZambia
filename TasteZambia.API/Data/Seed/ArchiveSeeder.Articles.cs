using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

public static partial class ArchiveSeeder
{
    private static Article Art(int order, string id, string kicker, string title, string author, string meta, string caption,
        string? lede = null, bool isLead = false, (string Label, string Duration, double Progress)? audio = null,
        (ArticleBlockKind Kind, string Text)[]? body = null, string[]? related = null) => new()
    {
        Id = id, SortOrder = order, Kicker = kicker, Title = title, Author = author, Meta = meta,
        PhotoNeededCaption = caption, Lede = lede, IsLead = isLead,
        AudioLabel = audio?.Label, AudioDuration = audio?.Duration, AudioProgress = audio?.Progress,
        Body = (body ?? []).Select((b, i) => new ArticleBlock { ArticleId = id, SortOrder = i, Kind = b.Kind, Text = b.Text }).ToList(),
        RelatedDishes = (related ?? []).Select((d, i) => new ArticleRelatedDish { ArticleId = id, SortOrder = i, DishId = d }).ToList(),
    };

    private static List<Article> BuildArticles() =>
    [
        Art(0, "nshima", "Archive essay", "The History of Nshima", "Dr. Mutale Chileshe", "8 min read · Pan-Zambian",
            "photo: woman stirring nshima with a mwiko",
            lede: "The dish at the centre of every Zambian meal is younger than most people assume.",
            isLead: true,
            audio: ("Listen in Bemba", "12:40", 0.18),
            related: ["nshima"],
            body:
            [
                (ArticleBlockKind.Lede, "The dish at the centre of every Zambian meal is younger than most people assume. Maize arrived in this part of Africa through trade, and for a long time it sat alongside older grains rather than replacing them."),
                (ArticleBlockKind.Paragraph, "Before maize, the staples were millet and sorghum. They were pounded, sifted and stirred into a thick porridge by the same method still used today: water brought to the boil, a thin gruel made first, then more meal worked in with a wooden mwiko until the mixture pulls away from the sides of the pot."),
                (ArticleBlockKind.Paragraph, "What changed was the grain, not the technique. Maize gave a higher yield and a whiter meal, and through the colonial period it was actively promoted over the older grains. Within two or three generations it had become the default, and nshima came to mean maize nshima specifically."),
                (ArticleBlockKind.PullQuote, "The older grains never disappeared. In parts of Muchinga and North-Western Province, millet nshima is still what is served when the meal matters."),
                (ArticleBlockKind.Paragraph, "Cassava followed a similar path in the north. In Luapula and Northern Province, nshima made from fermented, sun-dried cassava flour is the everyday version, and maize is the visitor. The name stays the same; the flour, the colour and the taste do not."),
                (ArticleBlockKind.Paragraph, "This is why the archive records nshima as a family of dishes rather than one recipe. What is constant is the method, the mwiko, and the fact that it is never eaten alone."),
            ]),

        Art(1, "groundnuts", "Archive essay", "Why Groundnuts Anchor Zambian Cooking", "Namakau Sitali", "6 min read · Eastern Province",
            "photo: groundnuts being winnowed"),

        Art(2, "methods", "Technique", "Traditional Cooking Methods in Zambia", "Archive team", "11 min read · Countrywide",
            "photo: clay pot on a mbaula"),

        Art(3, "growingup", "Community voices", "Foods We Ate Growing Up", "12 contributors", "Audio and text · Open for submissions",
            "photo: family eating together"),

        Art(4, "passeddown", "Family archive", "Recipes Passed Down Through Generations", "Archive team", "9 min read · 24 family recipes",
            "photo: handwritten recipe book"),

        Art(5, "beforekitchens", "Archive essay", "Cooking Before the Modern Kitchen", "Dr. Mutale Chileshe", "7 min read · Countrywide",
            "photo: open fire cooking"),
    ];
}
