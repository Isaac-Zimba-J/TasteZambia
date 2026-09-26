using TasteZambia.Shared.Validation;

namespace TasteZambia.Core.Tests.Services;

/// <summary>
/// The API and the app used to compute "how complete is this" separately, with different
/// rules, so a recipe could read "100% complete · 1 thing left" or "75% complete · 0 things
/// left". These pin the one definition both now share.
/// </summary>
public class FamilyDraftProgressTests
{
    [Theory]
    [InlineData(false, false, false, false, 0)]
    [InlineData(true, false, false, false, 25)]
    [InlineData(true, true, false, false, 50)]
    [InlineData(true, true, true, false, 75)]
    [InlineData(true, true, true, true, 100)]
    public void EveryStepIsWorthTheSameTwentyFive(bool named, bool taught, bool method, bool privacy, int expected)
        => Assert.Equal(expected, FamilyDraftProgress.Percent(named, taught, method, privacy));

    [Fact]
    public void ThePercentageIsAlwaysTheDoneStepsAndNothingElse()
    {
        // The contradiction this replaced: a number and a list that disagreed. Whatever the
        // combination, one cannot say "complete" while the other still has something left.
        foreach (var named in new[] { true, false })
        foreach (var taught in new[] { true, false })
        foreach (var method in new[] { true, false })
        foreach (var privacy in new[] { true, false })
        {
            var steps = FamilyDraftProgress.Steps(named, taught, method, privacy);
            var percent = FamilyDraftProgress.Percent(named, taught, method, privacy);

            Assert.Equal(4, steps.Count);
            Assert.Equal(steps.Count(s => s.IsDone) * 25, percent);
            Assert.Equal(percent == 100, steps.All(s => s.IsDone));
            Assert.Equal(percent == 0, steps.All(s => !s.IsDone));
        }
    }

    [Fact]
    public void TheStepsKeepTheDesignsWording()
    {
        Assert.Equal(
            ["Recipe name, region and photos", "Who taught you, and the story", "Her method, in her words", "Who can see it"],
            FamilyDraftProgress.Steps(false, false, false, false).Select(s => s.Label));
    }
}
