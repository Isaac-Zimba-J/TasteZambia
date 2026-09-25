namespace TasteZambia.Shared.Enums;

/// <summary>Who may see a preserved family recipe. Set by the contributor, changeable at any time.</summary>
public enum PrivacyLevel
{
    PrivateToMe = 0,
    SharedWithFamily = 1,
    PublicInArchive = 2,
}

/// <summary>Lifecycle state of a public contribution.</summary>
public enum ContributionStatus
{
    Draft = 0,
    InReview = 1,
    ChangesRequested = 2,
    Published = 3,
    Withdrawn = 4,
}

/// <summary>Progress of one step in the review timeline.</summary>
public enum ReviewState
{
    Pending = 0,
    InProgress = 1,
    Done = 2,
}

/// <summary>Whether a dish's local name or its English name is used as the title.</summary>
public enum TitleLanguage
{
    LocalName = 0,
    English = 1,
}

/// <summary>Where a dish came from. Seeded editorial content, or a published community contribution.</summary>
public enum Provenance
{
    Editorial = 0,
    Community = 1,
}

/// <summary>One line of a contribution's review history.</summary>
public enum ReviewEventKind
{
    Submitted = 0,
    Read = 1,
    ChangesRequested = 2,
    Resubmitted = 3,
    Published = 4,
    Withdrawn = 5,
}

/// <summary>What a stored blob is. Decides its size ceiling and how the app renders it.</summary>
public enum MediaKind
{
    Photo = 0,
    Audio = 1,
}

/// <summary>Where someone stands with a family recipe they were invited to.</summary>
public enum MemberState
{
    Invited = 0,
    Joined = 1,
    Removed = 2,
}

/// <summary>How far a recording has got through transcription. Stage 5 drives the rest.</summary>
public enum TranscriptState
{
    None = 0,
    Pending = 1,
    Transcribing = 2,
    AwaitingApproval = 3,
    Approved = 4,
}
