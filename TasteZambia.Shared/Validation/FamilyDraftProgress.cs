namespace TasteZambia.Shared.Validation;

/// <summary>
/// How far a preserved family recipe has got, defined once.
///
/// The API and the app each used to work this out for themselves, with different rules, so a
/// recipe could read "100% complete · 1 thing left" on one line and its checklist disagree on
/// the next. Both now compute from this, which makes the contradiction impossible rather than
/// merely unlikely.
///
/// A photograph and a recording are deliberately not gates. They make a record far richer and
/// the screens ask for them, but a family who only has the words has still preserved something.
/// </summary>
public static class FamilyDraftProgress
{
    /// <summary>One line of the draft checklist. The label is the design's wording.</summary>
    public readonly record struct Step(string Label, bool IsDone);

    public static IReadOnlyList<Step> Steps(
        bool hasNameAndRegion, bool hasTeacherAndStory, bool hasMethod, bool privacyChosen) =>
    [
        new("Recipe name, region and photos", hasNameAndRegion),
        new("Who taught you, and the story", hasTeacherAndStory),
        new("Her method, in her words", hasMethod),
        new("Who can see it", privacyChosen),
    ];

    /// <summary>Four equal parts, so every step is worth the same twenty-five.</summary>
    public static int Percent(
        bool hasNameAndRegion, bool hasTeacherAndStory, bool hasMethod, bool privacyChosen)
        => Steps(hasNameAndRegion, hasTeacherAndStory, hasMethod, privacyChosen).Count(s => s.IsDone) * 25;
}
