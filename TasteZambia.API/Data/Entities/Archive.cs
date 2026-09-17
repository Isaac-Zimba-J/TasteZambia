using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Data.Entities;

/// <summary>Every archive row carries when it last changed. Stage 5's delta sync reads this.</summary>
public abstract class ArchiveEntity
{
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Dish : ArchiveEntity
{
    public required string Id { get; set; }
    public required string LocalName { get; set; }
    public required string EnglishName { get; set; }
    public required string Region { get; set; }
    public required string TimeLabel { get; set; }
    public required string Difficulty { get; set; }
    public required string Description { get; set; }
    public string? ImageAsset { get; set; }
    public string PhotoNeededCaption { get; set; } = "photo needed";
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public int SortOrder { get; set; }

    public Provenance Provenance { get; set; } = Provenance.Editorial;
    /// <summary>Set when this dish was published from a community contribution.</summary>
    public Guid? ContributionId { get; set; }

    public Recipe? Recipe { get; set; }
}

public class Recipe : ArchiveEntity
{
    public int Id { get; set; }
    public required string DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public required string Subtitle { get; set; }
    public bool IsVerified { get; set; }

    public required string ContributorName { get; set; }
    public required string ContributorLocation { get; set; }
    public string? ContributorAvatarAsset { get; set; }

    public List<RecipeParagraph> CulturalContext { get; set; } = [];
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<CookingStep> Steps { get; set; } = [];
    public List<MethodNarrative> Methods { get; set; } = [];
    public List<RegionalVariation> Variations { get; set; } = [];
}

public class RecipeParagraph
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Null for plain items (Salt, Water) that have no archive entry.</summary>
    public string? IngredientKey { get; set; }
    public required string DisplayName { get; set; }
    public required string DisplaySubtitle { get; set; }
    public required string Quantity { get; set; }
}

public class CookingStep
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public enum MethodKind { Traditional = 0, Modern = 1 }

public class MethodNarrative
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public MethodKind Kind { get; set; }
    public required string Heading { get; set; }
    public List<MethodParagraph> Paragraphs { get; set; } = [];
}

public class MethodParagraph
{
    public int Id { get; set; }
    public int MethodNarrativeId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class RegionalVariation
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }
    public required string Place { get; set; }
    public required string Description { get; set; }
}

public class Ingredient : ArchiveEntity
{
    public required string Key { get; set; }
    public required string LocalName { get; set; }
    public required string EnglishName { get; set; }
    public required string Description { get; set; }
    public required string WhereFound { get; set; }
    public required string TraditionalPreparation { get; set; }
    public required string PendingLanguages { get; set; }
    public string? ImageAsset { get; set; }
    public int SortOrder { get; set; }

    public List<IngredientLocalName> LocalNames { get; set; } = [];
    public List<IngredientUsage> Usages { get; set; } = [];
}

public class IngredientLocalName
{
    public int Id { get; set; }
    public required string IngredientKey { get; set; }
    public int SortOrder { get; set; }
    public required string Language { get; set; }
    public required string Name { get; set; }
}

/// <summary>Which dishes an ingredient appears in. Ordered as the archive lists them.</summary>
public class IngredientUsage
{
    public int Id { get; set; }
    public required string IngredientKey { get; set; }
    public required string DishId { get; set; }
    public int SortOrder { get; set; }
}

public class Province : ArchiveEntity
{
    public required string Name { get; set; }
    public required string Seat { get; set; }
    public required string Blurb { get; set; }
    public required string CookingTradition { get; set; }
    public int SortOrder { get; set; }

    public List<ProvinceFood> Foods { get; set; } = [];
    public List<ProvinceIngredient> CommonIngredients { get; set; } = [];
}

public class ProvinceFood
{
    public int Id { get; set; }
    public required string ProvinceName { get; set; }
    public int SortOrder { get; set; }

    /// <summary>The archive's own name for the food; may not match a seeded dish.</summary>
    public required string Name { get; set; }
}

public class ProvinceIngredient
{
    public int Id { get; set; }
    public required string ProvinceName { get; set; }
    public int SortOrder { get; set; }
    public required string Name { get; set; }
}

public class Category : ArchiveEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? ImageAsset { get; set; }
    public int SortOrder { get; set; }
}

public enum ArticleBlockKind { Lede = 0, Paragraph = 1, PullQuote = 2 }

public class Article : ArchiveEntity
{
    public required string Id { get; set; }
    public required string Kicker { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string Meta { get; set; }
    public string? Lede { get; set; }
    public bool IsLead { get; set; }
    public string? ImageAsset { get; set; }
    public string PhotoNeededCaption { get; set; } = "photo needed";
    public int SortOrder { get; set; }

    public string? AudioLabel { get; set; }
    public string? AudioDuration { get; set; }
    public double? AudioProgress { get; set; }

    public List<ArticleBlock> Body { get; set; } = [];
    public List<ArticleRelatedDish> RelatedDishes { get; set; } = [];
}

public class ArticleBlock
{
    public int Id { get; set; }
    public required string ArticleId { get; set; }
    public int SortOrder { get; set; }
    public ArticleBlockKind Kind { get; set; }
    public required string Text { get; set; }
}

public class ArticleRelatedDish
{
    public int Id { get; set; }
    public required string ArticleId { get; set; }
    public required string DishId { get; set; }
    public int SortOrder { get; set; }
}
