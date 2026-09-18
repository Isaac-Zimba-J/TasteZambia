using TasteZambia.Core.Models;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Services;

public interface IFamilyArchiveService
{
    int PercentComplete { get; }
    AudioClip Recording { get; }
    AudioClip ApprovedRecording { get; }
    PrivacyLevel Privacy { get; }
    void SetPrivacy(PrivacyLevel level);
    IReadOnlyList<DraftChecklistItem> Checklist { get; }
    IReadOnlyList<FamilyMember> Members { get; }
    IReadOnlyList<FamilyNote> Notes { get; }
    IReadOnlyList<ProvenanceStep> Provenance { get; }
    IReadOnlyList<PreservedRecipe> PreservedRecipes { get; }
}

public sealed class FamilyArchiveService : IFamilyArchiveService
{
    public int PercentComplete => 72;

    public AudioClip Recording { get; } = new("Banakulu Mwaba, in Bemba", "12:40", false);
    public AudioClip ApprovedRecording { get; } = new("Her voice, in Bemba", "12:40", true);

    public PrivacyLevel Privacy { get; private set; } = PrivacyLevel.SharedWithFamily;
    public void SetPrivacy(PrivacyLevel level) => Privacy = level;

    public IReadOnlyList<DraftChecklistItem> Checklist { get; } =
    [
        new("Recipe name, region and photos", null, true),
        new("Who taught you, and the story", null, true),
        new("Cooking steps", "Two of four written", false),
        new("Who can see it", "Not chosen yet — private until you do", false),
    ];

    public IReadOnlyList<FamilyMember> Members { get; } =
    [
        new("Chanda Mwaba",  "You · owner",        "Owner",   "#EEF2EC", "#2F6A4D"),
        new("Mutinta Mwaba", "Sister, Lusaka",     "Joined",  "#EEF2EC", "#2F6A4D"),
        new("Aunt Bwalya",   "Mungwi",             "Joined",  "#EEF2EC", "#2F6A4D"),
        new("Kaunda Mwaba",  "Cousin, Manchester", "Invited", "#F7EEDA", "#7A5A10"),
    ];

    public IReadOnlyList<FamilyNote> Notes { get; } =
    [
        new("Aunt Bwalya", "Added a note · 4 Sep",
            "She never used tomato in this. If you add tomato it becomes a different relish and she would have said so."),
        new("Mutinta Mwaba", "Added a photo · 5 Sep",
            "Found the picture from Christmas 2011, the year we all came home. The pot in it is the same clay pot."),
    ];

    public IReadOnlyList<ProvenanceStep> Provenance { get; } =
    [
        new("Preserved privately",               "Written down in March 2026, with her recording.",                  "#2F6A4D"),
        new("Family agreed to publish",          "All four members with access consented in August.",                "#2F6A4D"),
        new("Verified against Northern records", "Reviewed by Namakau Sitali, September 2026.",                      "#2F6A4D"),
        new("Published and credited",            "Listed under Northern Province, linked to chibwabwa and mbalala.", "#C07F1E"),
    ];

    public IReadOnlyList<PreservedRecipe> PreservedRecipes { get; } =
    [
        new("Ifisashi ya Banakulu",  "Banakulu Mwaba, Mungwi", "Public",  "#EEF2EC", "#2F6A4D", "Audio 12:40 · 3 photos · 2 family notes"),
        new("Inkoko ya Bataata",     "my father, Kitwe",       "Family",  "#F7EEDA", "#7A5A10", "1 photo · no audio yet"),
        new("Munkoyo wa Ba Shikulu", "Grandfather, Solwezi",   "Private", "#F0ECE4", "#6B5C4A", "Draft · 40% complete"),
        new("Chikanda ya Ba Mayo",   "my mother, Chinsali",    "Family",  "#F7EEDA", "#7A5A10", "Audio 6:12 · 2 photos"),
    ];
}
