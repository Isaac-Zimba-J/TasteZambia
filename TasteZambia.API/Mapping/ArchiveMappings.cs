using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Contracts.Regions;
using SharedBlockKind = TasteZambia.Shared.Contracts.Culture.ArticleBlockKind;

namespace TasteZambia.API.Mapping;

/// <summary>Entity to DTO in one place, so no slice invents its own shape.</summary>
public static class ArchiveMappings
{
    public static DishDto ToDto(this Dish d) => new()
    {
        Id = d.Id, LocalName = d.LocalName, EnglishName = d.EnglishName,
        Region = d.Region, TimeLabel = d.TimeLabel, Difficulty = d.Difficulty,
        Description = d.Description, ImageAsset = d.ImageAsset,
        PhotoNeededCaption = d.PhotoNeededCaption,
        PrepTime = d.PrepTime, CookTime = d.CookTime,
    };

    public static RecipeDto ToDto(this Recipe r) => new()
    {
        Dish = r.Dish.ToDto(),
        Subtitle = r.Subtitle,
        IsVerified = r.IsVerified,
        CulturalContext = [.. r.CulturalContext.OrderBy(p => p.SortOrder).Select(p => p.Text)],
        Ingredients = [.. r.Ingredients.OrderBy(i => i.SortOrder).Select(i => new RecipeIngredientDto
        {
            IngredientKey = i.IngredientKey, DisplayName = i.DisplayName,
            DisplaySubtitle = i.DisplaySubtitle, Quantity = i.Quantity,
        })],
        Steps = [.. r.Steps.OrderBy(s => s.Number).Select(s => new CookingStepDto(s.Number, s.Title, s.Body))],
        TraditionalMethod = r.Methods.Single(m => m.Kind == MethodKind.Traditional).ToDto(),
        ModernMethod = r.Methods.Single(m => m.Kind == MethodKind.Modern).ToDto(),
        Variations = [.. r.Variations.OrderBy(v => v.SortOrder).Select(v => new RegionalVariationDto(v.Place, v.Description))],
        Contributor = new ContributorDto(r.ContributorName, r.ContributorLocation, r.ContributorAvatarAsset),
    };

    private static MethodNarrativeDto ToDto(this MethodNarrative m)
        => new(m.Heading, [.. m.Paragraphs.OrderBy(p => p.SortOrder).Select(p => p.Text)]);

    public static IngredientDto ToDto(this Ingredient i) => new()
    {
        Key = i.Key, LocalName = i.LocalName, EnglishName = i.EnglishName,
        Description = i.Description, WhereFound = i.WhereFound,
        TraditionalPreparation = i.TraditionalPreparation,
        PendingLanguages = i.PendingLanguages, ImageAsset = i.ImageAsset,
        LocalNames = [.. i.LocalNames.OrderBy(l => l.SortOrder).Select(l => new LocalNameDto(l.Language, l.Name))],
        UsedInDishIds = [.. i.Usages.OrderBy(u => u.SortOrder).Select(u => u.DishId)],
    };

    public static ProvinceDto ToDto(this Province p) => new()
    {
        Name = p.Name, Seat = p.Seat, Blurb = p.Blurb, CookingTradition = p.CookingTradition,
        SignatureFoods = [.. p.Foods.OrderBy(f => f.SortOrder).Select(f => f.Name)],
        CommonIngredients = [.. p.CommonIngredients.OrderBy(i => i.SortOrder).Select(i => i.Name)],
    };

    // The entity enum is a storage concern and the DTO enum a wire contract; both declare
    // the same three members with the same explicit values, which makes this cast safe.
    public static ArticleDto ToDto(this Article a) => new()
    {
        Id = a.Id, Kicker = a.Kicker, Title = a.Title, Author = a.Author, Meta = a.Meta,
        Lede = a.Lede, IsLead = a.IsLead, ImageAsset = a.ImageAsset,
        PhotoNeededCaption = a.PhotoNeededCaption,
        Body = [.. a.Body.OrderBy(b => b.SortOrder).Select(b => new ArticleBlockDto((SharedBlockKind)b.Kind, b.Text))],
        RelatedDishIds = [.. a.RelatedDishes.OrderBy(r => r.SortOrder).Select(r => r.DishId)],
        Audio = a.AudioLabel is null ? null : new AudioNarrationDto(a.AudioLabel, a.AudioDuration ?? "", a.AudioProgress ?? 0),
    };

    public static CategoryDto ToDto(this Category c) => new(c.Name, c.ImageAsset, c.SortOrder);
}
