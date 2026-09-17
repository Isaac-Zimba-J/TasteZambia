using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Contracts.Regions;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// Wire contracts to the mobile domain models. Kept separate on purpose: the models
/// carry presentation helpers (MetaLabel, HasPhoto) that do not belong in a contract,
/// and the API is free to reshape a response without touching a ViewModel.
/// </summary>
internal static class DtoMappings
{
    public static Dish ToModel(this DishDto d) => new()
    {
        Id = d.Id, LocalName = d.LocalName, EnglishName = d.EnglishName,
        Region = d.Region, TimeLabel = d.TimeLabel, Difficulty = d.Difficulty,
        Description = d.Description, ImageAsset = d.ImageAsset,
        PhotoNeededCaption = d.PhotoNeededCaption,
        PrepTime = d.PrepTime, CookTime = d.CookTime,
    };

    public static RecipeDetail ToModel(this RecipeDto r) => new()
    {
        Dish = r.Dish.ToModel(),
        Subtitle = r.Subtitle,
        IsVerified = r.IsVerified,
        CulturalContext = r.CulturalContext,
        Ingredients = r.Ingredients.Select(i => new RecipeIngredient
        {
            IngredientKey = i.IngredientKey, DisplayName = i.DisplayName,
            DisplaySubtitle = i.DisplaySubtitle, Quantity = i.Quantity,
        }).ToList(),
        Steps = r.Steps.Select(s => new CookingStep(s.Number, s.Title, s.Body)).ToList(),
        TraditionalMethod = new MethodNarrative(r.TraditionalMethod.Heading, r.TraditionalMethod.Paragraphs),
        ModernMethod = new MethodNarrative(r.ModernMethod.Heading, r.ModernMethod.Paragraphs),
        Variations = r.Variations.Select(v => new RegionalVariation(v.Place, v.Description)).ToList(),
        Contributor = new Contributor(r.Contributor.Name, r.Contributor.Location, r.Contributor.AvatarAsset),
    };

    public static Ingredient ToModel(this IngredientDto i) => new()
    {
        Key = i.Key, LocalName = i.LocalName, EnglishName = i.EnglishName,
        Description = i.Description, WhereFound = i.WhereFound,
        TraditionalPreparation = i.TraditionalPreparation,
        PendingLanguages = i.PendingLanguages, ImageAsset = i.ImageAsset,
        LocalNames = i.LocalNames.Select(l => new LocalName(l.Language, l.Name)).ToList(),
        UsedInDishIds = i.UsedInDishIds,
    };

    public static Province ToModel(this ProvinceDto p) => new()
    {
        Name = p.Name, Seat = p.Seat, Blurb = p.Blurb, CookingTradition = p.CookingTradition,
        SignatureFoods = p.SignatureFoods, CommonIngredients = p.CommonIngredients,
    };

    public static Article ToModel(this ArticleDto a) => new()
    {
        Id = a.Id, Kicker = a.Kicker, Title = a.Title, Author = a.Author, Meta = a.Meta,
        Lede = a.Lede, IsLead = a.IsLead, ImageAsset = a.ImageAsset,
        PhotoNeededCaption = a.PhotoNeededCaption,
        Body = a.Body.Select(b => new ArticleBlock((Models.ArticleBlockKind)b.Kind, b.Text)).ToList(),
        RelatedDishIds = a.RelatedDishIds,
        Audio = a.Audio is null ? null : new AudioNarration(a.Audio.Label, a.Audio.Duration, a.Audio.Progress),
    };

    public static Category ToModel(this CategoryDto c) => new(c.Name, c.ImageAsset);
}
