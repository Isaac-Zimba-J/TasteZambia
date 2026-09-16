namespace TasteZambia.Core.Models;

public sealed record FamilyMember(string Name, string Role, string Status, string BadgeBgHex, string BadgeFgHex);
public sealed record FamilyNote(string Who, string When, string Body);
public sealed record DraftChecklistItem(string Label, string? Detail, bool IsDone)
{
    public bool HasDetail => !string.IsNullOrEmpty(Detail);
}
public sealed record ProvenanceStep(string Label, string Detail, string DotHex);
public sealed record AudioClip(string Speaker, string Duration, bool TranscriptApproved);

public sealed record PreservedRecipe(string Name, string TaughtBy, string Privacy,
                                     string BadgeBgHex, string BadgeFgHex, string Extras);
