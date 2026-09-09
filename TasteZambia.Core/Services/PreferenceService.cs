namespace TasteZambia.Core.Services;

public enum TitleLanguage { LocalName, English }

/// <summary>A title and its explanation, already ordered per the user's preference.</summary>
public readonly record struct DisplayName(string Title, string Subtitle);

public interface IPreferenceService
{
    TitleLanguage TitleLanguage { get; set; }
    bool ShowVerificationBadge { get; set; }
    DisplayName Resolve(string localName, string englishName);
    event EventHandler? Changed;
}

public sealed class PreferenceService : IPreferenceService
{
    private TitleLanguage _titleLanguage = TitleLanguage.LocalName;
    private bool _showVerificationBadge = true;

    public event EventHandler? Changed;

    public TitleLanguage TitleLanguage
    {
        get => _titleLanguage;
        set
        {
            if (_titleLanguage == value) return;
            _titleLanguage = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ShowVerificationBadge
    {
        get => _showVerificationBadge;
        set
        {
            if (_showVerificationBadge == value) return;
            _showVerificationBadge = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The archive rule: the local name is the title and English is the explanation,
    /// unless the reader has explicitly asked for English titles.
    /// </summary>
    public DisplayName Resolve(string localName, string englishName)
        => _titleLanguage == TitleLanguage.English
            ? new DisplayName(englishName, localName)
            : new DisplayName(localName, englishName);
}
