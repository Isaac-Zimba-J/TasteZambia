using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

public static partial class ArchiveSeeder
{
    private static Province Prov(int order, string name, string seat, string blurb, string tradition,
        string[] foods, string[] ingredients) => new()
    {
        Name = name, SortOrder = order, Seat = seat, Blurb = blurb, CookingTradition = tradition,
        Foods = foods.Select((f, i) => new ProvinceFood { ProvinceName = name, SortOrder = i, Name = f }).ToList(),
        CommonIngredients = ingredients.Select((g, i) => new ProvinceIngredient { ProvinceName = name, SortOrder = i, Name = g }).ToList(),
    };

    private static List<Province> BuildProvinces() =>
    [
        Prov(0, "Central", "Kabwe",
            "Maize country, and the province most associated with free-range village chicken cooked slowly over an open fire.",
            "Chicken is cooked whole in a clay pot set directly in the coals, the broth reduced rather than thickened.",
            ["Inkoko ya Mumushi", "Nshima", "Ifisashi"], ["Maize", "Groundnuts", "Village chicken"]),

        Prov(1, "Copperbelt", "Ndola",
            "The most urban food culture in the country. Mining towns drew people from every province, and the cooking absorbed all of it.",
            "Relishes here carry more tomato and more oil than their village versions, a change that came with wage kitchens.",
            ["Kapenta", "Ifisashi", "Nshima"], ["Kapenta", "Tomatoes", "Cooking oil"]),

        Prov(2, "Eastern", "Chipata",
            "The groundnut heartland. Almost every relish in the province passes through pounded groundnuts at some point.",
            "Groundnuts are roasted in a dry clay pan, winnowed in a flat basket, then pounded in a wooden mortar.",
            ["Chikanda", "Kandolo", "Ifisashi"], ["Groundnuts", "Sweet potatoes", "Pumpkin leaves"]),

        Prov(3, "Luapula", "Mansa",
            "Fish and cassava. The Luapula river, Lake Mweru and the Bangweulu swamps supply most of what is eaten here.",
            "Cassava is soaked for days to ferment, sun-dried on rocks, then pounded into flour for nshima.",
            ["Kapenta", "Katapa", "Nshima"], ["Cassava", "Kapenta", "Cassava leaves"]),

        Prov(4, "Lusaka", "Lusaka",
            "Every province cooks here. The capital is where regional dishes meet, get shortened for time, and turn into street food.",
            "Market kitchens serve nshima with a choice of relishes from four or five provinces at one counter.",
            ["Nshima", "Kapenta", "Delele"], ["Maize meal", "Tomatoes", "Rape leaves"]),

        Prov(5, "Muchinga", "Chinsali",
            "Foraged food. Wild orchid tubers and rainy-season mushrooms are gathered here in quantities found almost nowhere else.",
            "Mushrooms are dried on grass mats above the cooking fire so they keep through the dry season.",
            ["Chikanda", "Nshima", "Ifisashi"], ["Wild orchid tubers", "Bowa mushrooms", "Millet"]),

        Prov(6, "Northern", "Kasama",
            "Bemba cooking at its source. Cassava, groundnuts and pounded leaves, with long cooking times and little added fat.",
            "Leaves are pounded in a mortar rather than chopped, then simmered long enough to lose all bitterness.",
            ["Ifisashi", "Katapa", "Chikanda"], ["Cassava", "Groundnuts", "Cassava leaves"]),

        Prov(7, "North-Western", "Solwezi",
            "Cassava, wild fruit and honey. Home of munkoyo, the fermented root drink served cool from a calabash.",
            "Munkoyo root is crushed and stirred into cooled maize porridge, then left to ferment for two or three days.",
            ["Munkoyo", "Nshima", "Katapa"], ["Munkoyo root", "Cassava", "Wild honey"]),

        Prov(8, "Southern", "Choma",
            "Cattle country. Soured milk sits alongside the relish here in a way it does not elsewhere in Zambia.",
            "Fresh milk is left to sour in a covered gourd and eaten with nshima, needing no cooking at all.",
            ["Delele", "Nshima", "Ifisashi"], ["Maize", "Soured milk", "Okra"]),

        Prov(9, "Western", "Mongu",
            "The Barotse floodplain. Fish from the Zambezi, rice grown on the plain, and wild fruit from the sandveld.",
            "Fish is smoked over a slow fire on raised racks, a method that carries it through the flood season.",
            ["Nshima", "Kapenta", "Delele"], ["Freshwater fish", "Rice", "Wild fruit"]),
    ];
}
