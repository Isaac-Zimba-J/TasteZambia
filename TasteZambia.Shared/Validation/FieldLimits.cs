namespace TasteZambia.Shared.Validation;

/// <summary>
/// Shared field limits so client-side validation and server-side validation
/// can never disagree about what will be accepted.
/// </summary>
public static class FieldLimits
{
    public const int LocalNameMax = 120;
    public const int EnglishNameMax = 160;
    public const int ShortDescriptionMax = 400;
    public const int StoryMax = 4000;
    public const int StepBodyMax = 1000;
    public const int QuantityMax = 60;
    public const int MaxStepsPerRecipe = 40;
    public const int MaxIngredientsPerRecipe = 60;
    public const int MaxPhotosPerRecipe = 10;
}
