using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface IContributionService
{
    ContributionDraft StartShareDraft();
    ContributionDraft StartFamilyDraft();
    Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default);
    IReadOnlyList<ReviewStage> ReviewPipeline { get; }
}

public sealed class ContributionService : IContributionService
{
    public IReadOnlyList<ReviewStage> ReviewPipeline => SeedData.ReviewPipeline;

    /// <summary>Pre-filled to match the walkthrough content in the design.</summary>
    public ContributionDraft StartShareDraft() => new()
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

    public ContributionDraft StartFamilyDraft() => new()
    {
        LocalName = "Ifisashi ya Banakulu",
        EnglishDescription = "Pumpkin leaves in groundnuts, the way my grandmother made it",
        Province = "Northern",
        Language = "Bemba",
        TaughtBy = "Banakulu Mwaba, my father's mother",
        TaughtByOrigin = "Mungwi, outside Kasama. She was taught by her own mother.",
        Story = "She cooked this every time we arrived from Kitwe, before we had even put our bags down. She never measured the groundnuts. She said your hand learns the amount and your head forgets it.",
        TraditionalMethod = "Clay pot on the mbaula. Groundnuts pounded, never blended. No tomato. She added the salt at the very end, off the heat.",
        AddToFoodStories = true,
        Privacy = PrivacyLevel.SharedWithFamily,
    };

    public Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default)
        => Task.FromResult(new Contribution(
            draft.LocalName,
            ContributionStatus.InReview,
            $"{draft.Province} Province · submitted just now"));
}
