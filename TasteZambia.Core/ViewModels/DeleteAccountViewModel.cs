using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

/// <summary>
/// Deleting an account. There is nothing behind this one, so the screen is built to be
/// understood before it is used: it says what goes, what stays and why, and it will not act
/// until the reader has typed the word.
/// </summary>
public sealed partial class DeleteAccountViewModel(
    IAccountService accounts,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>What the reader has to type. Deliberately not a button alone.</summary>
    public const string RequiredWord = "DELETE";

    [ObservableProperty] private string _deviceId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private string _typed = "";

    [ObservableProperty] private string _errorMessage = "";

    /// <summary>Set once it has happened, so the screen stops offering to do it again.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    private bool _isDone;

    [ObservableProperty] private string _summary = "";

    public bool IsPending => !IsDone;

    public bool CanDelete => string.Equals(Typed.Trim(), RequiredWord, StringComparison.OrdinalIgnoreCase);

    public override async Task InitializeAsync()
        => DeviceId = await accounts.DeviceIdAsync();

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        // CanExecute only greys the button out; ExecuteAsync does not consult it. For something
        // with nothing behind it, the guard belongs here too and not only in the binding.
        if (!CanDelete) return;

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            Summary = Describe(await accounts.DeleteAsync());
            IsDone = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Nothing was deleted, here or in the archive. Saying so matters: a reader who
            // believed this had worked would stop expecting their recipes to exist.
            ErrorMessage = "Could not reach the archive, so nothing has been deleted. Try again when you are online.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// What actually happened, in the reader's terms. A published recipe staying in the archive
    /// is not what "delete everything" sounds like it would do, so it is said plainly.
    /// </summary>
    internal static string Describe(Shared.Contracts.Me.DeleteAccountResultDto r)
    {
        var lines = new List<string>();

        if (r.DraftsDeleted > 0)
            lines.Add(r.DraftsDeleted == 1
                ? "1 recipe waiting for review was deleted."
                : $"{r.DraftsDeleted} recipes waiting for review were deleted.");

        if (r.FamilyRecipesDeleted > 0)
            lines.Add(r.FamilyRecipesDeleted == 1
                ? "1 family recipe you preserved was deleted, along with its photographs and recordings."
                : $"{r.FamilyRecipesDeleted} family recipes you preserved were deleted, along with their photographs and recordings.");

        if (r.FamilyRecipesLeft > 0)
            lines.Add(r.FamilyRecipesLeft == 1
                ? "You were removed from 1 family recipe someone else preserved."
                : $"You were removed from {r.FamilyRecipesLeft} family recipes other people preserved.");

        if (r.PublishedRecipesAnonymised > 0)
            lines.Add(r.PublishedRecipesAnonymised == 1
                ? "1 recipe you contributed stays in the archive, with your name removed from it."
                : $"{r.PublishedRecipesAnonymised} recipes you contributed stay in the archive, with your name removed from them.");

        if (r.NotesAnonymised > 0)
            lines.Add(r.NotesAnonymised == 1
                ? "1 note you added to another family's recipe stays with them, without your name."
                : $"{r.NotesAnonymised} notes you added to other families' recipes stay with them, without your name.");

        lines.Add("Your profile, your saved recipes and this account are gone.");

        return string.Join("\n\n", lines);
    }

    /// <summary>The only way on. There is no account left to go back to.</summary>
    [RelayCommand] private Task Finish() => Navigation.GoToAsync("//home");

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
