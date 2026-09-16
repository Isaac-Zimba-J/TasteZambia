using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

public static partial class ArchiveSeeder
{
    private static Ingredient Ing(int order, string key, string local, string english, string description,
        string whereFound, string preparation, string pending, string? image,
        (string Language, string Name)[] localNames, string[] usedIn) => new()
    {
        Key = key, SortOrder = order, LocalName = local, EnglishName = english,
        Description = description, WhereFound = whereFound, TraditionalPreparation = preparation,
        PendingLanguages = pending, ImageAsset = image,
        LocalNames = localNames.Select((l, i) => new IngredientLocalName
            { IngredientKey = key, SortOrder = i, Language = l.Language, Name = l.Name }).ToList(),
        Usages = usedIn.Select((d, i) => new IngredientUsage
            { IngredientKey = key, SortOrder = i, DishId = d }).ToList(),
    };

    private static List<Ingredient> BuildIngredients() =>
    [
        Ing(0, "chibwabwa", "Chibwabwa", "Pumpkin leaves",
            "The young leaves and tender shoots of the pumpkin plant, picked before the fruit is taken. Sold in tied bundles at every market and grown in almost every village garden.",
            "Grown countrywide, most abundant in the rainy season from December to March.",
            "The leaves are stripped from their stalks, rolled tight and shredded fine with a knife, then rubbed between the palms with a little salt to soften them before cooking.",
            "Tonga, Lozi, Kaonde, Lunda, Luvale", ImgMarket,
            [("Bemba, Nyanja", "Chibwabwa")], ["ifisashi", "nshima", "delele"]),

        Ing(1, "mbalala", "Mbalala", "Groundnuts",
            "Roasted and pounded into a coarse, oily flour that thickens relishes and supplies the fat in cooking where oil was never used.",
            "Eastern Province is the heartland of groundnut farming; grown in every province.",
            "Roasted in a clay pan over coals, winnowed by hand, then pounded in a wooden mortar until the flour begins to release its oil.",
            "Tonga, Lozi", null,
            [("Bemba", "Mbalala"), ("Nyanja", "Nsawawa")], ["ifisashi", "chikanda", "kandolo"]),

        Ing(2, "kapenta", "Kapenta", "Dried lake sardines",
            "Small freshwater sardines caught at night under lamps, sun-dried whole on racks and sold by the tin.",
            "Lake Tanganyika, Lake Mweru and Lake Kariba. Traded countrywide.",
            "Rinsed, then dry-fried without oil until crisp before tomato and onion are added.",
            "Tonga, Lozi", null,
            [("Bemba", "Kapenta"), ("Nyanja", "Kapenta")], ["kapenta", "ifisashi"]),

        Ing(3, "tute", "Tute", "Cassava",
            "A starchy root that stores in the ground for years, making it the food that carries households through a poor harvest.",
            "Luapula, Northern and North-Western Provinces.",
            "Peeled, soaked for several days to ferment and remove bitterness, then sun-dried and pounded into flour.",
            "Tonga, Nyanja", null,
            [("Bemba", "Tute"), ("Luvale", "Mbombo")], ["nshima"]),

        Ing(4, "katapa", "Katapa", "Cassava leaves",
            "The leaves of the cassava plant, pounded rather than chopped, cooked long to soften and to drive off bitterness.",
            "Luapula and Northern Provinces.",
            "Pounded in a mortar with a little water, then simmered a long while with groundnuts.",
            "Luvale, Lunda, Kaonde", null,
            [("Bemba", "Katapa")], ["ifisashi"]),

        Ing(5, "chikanda", "Chikanda", "Wild orchid tubers",
            "Tubers of wild terrestrial orchids, foraged from grassland. Now under pressure from overharvesting.",
            "Northern, Muchinga and Eastern Provinces.",
            "Peeled, boiled and pounded, then cooked with groundnut flour, soda and chilli until it sets firm.",
            "Nyanja, Tonga", null,
            [("Bemba", "Chikanda")], ["chikanda"]),

        Ing(6, "impwa", "Impwa", "Garden eggs",
            "Small pale African eggplants, firmer and more bitter than the purple kind, eaten raw with salt or cooked into relish.",
            "Grown countrywide in home gardens.",
            "Sliced and cooked briefly with tomato so the bitterness stays present.",
            "Tonga, Lozi", null,
            [("Bemba", "Impwa"), ("Nyanja", "Sumu")], ["ifisashi"]),

        Ing(7, "bondwe", "Bondwe", "Amaranth greens",
            "A wild green that comes up on its own in cultivated ground. Gathered, not planted.",
            "Countrywide, following the rains.",
            "Gathered young, cooked with groundnuts or with a little soda to hold the colour.",
            "Tonga, Lozi", null,
            [("Bemba", "Bondwe"), ("Nyanja", "Bonongwe")], ["ifisashi"]),

        Ing(8, "kandolo", "Kandolo", "Sweet potatoes",
            "Boiled or roasted in the coals, eaten with tea in the morning or pounded with groundnuts.",
            "Eastern and Northern Provinces.",
            "Buried in hot ash and coals to roast slowly in the skin.",
            "Tonga, Lozi", null,
            [("Bemba", "Kandolo"), ("Nyanja", "Mbatatesi")], ["kandolo"]),
    ];
}
