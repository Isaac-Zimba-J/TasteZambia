using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

/// <summary>
/// Why a screen has nothing on it. The reader is owed a different picture and a different
/// sentence for each of these: "check your connection" is unhelpful when the connection is
/// fine and the archive simply asked them to wait.
/// </summary>
public enum LoadFailure
{
    None = 0,
    Offline = 1,
    TooManyRequests = 2,
    Unexpected = 3,
}

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    /// <summary>Shown when the archive cannot be reached. Deliberately says what to do next.</summary>
    public const string OfflineMessage = "Could not reach the archive. Check your connection and try again.";

    /// <summary>
    /// Shown when the archive answered 429. Telling this reader to check their connection
    /// would be wrong twice over - it is working, and there is nothing for them to fix.
    /// </summary>
    public const string TooManyRequestsMessage =
        "The archive is asking everyone to slow down for a moment. Try again in a few minutes.";

    /// <summary>Shown when the failure is not the network's fault. The detail is in the log.</summary>
    public const string UnexpectedMessage = "Something went wrong loading this. Try again.";

    /// <summary>Shown when the API refuses a write because it is not the caller's to make. The
    /// server answers this the same as "not found" so it does not confirm the recipe exists.</summary>
    public const string NotAllowedMessage = "Only the person who preserved this recipe can change that.";

    /// <summary>
    /// Shown when a recipe the reader had is no longer theirs to open - they were removed from
    /// it, or it is gone. A 404 is not an exception, so without this the screen stays blank and
    /// says nothing at all.
    /// </summary>
    public const string NoLongerAvailableMessage =
        "This recipe is no longer shared with you. Whoever preserved it may have removed your access.";

    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstLoad))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    [NotifyPropertyChangedFor(nameof(IsFirstLoad))]
    private string _loadError = "";

    /// <summary>Which kind of failure it was, so the screen can draw the right thing.</summary>
    [ObservableProperty]
    private LoadFailure _failure;

    [ObservableProperty]
    private string _title = "";

    public bool HasLoadError => LoadError.Length > 0;

    /// <summary>True while the screen has nothing to show yet - the moment a spinner earns its place.</summary>
    public bool IsFirstLoad => IsLoading && !HasContent;

    /// <summary>Whether anything is on screen already. Override wherever a load fills a collection.</summary>
    protected virtual bool HasContent => false;

    /// <summary>Called by each page's OnAppearing. Override to load data.</summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Runs <see cref="InitializeAsync"/> with the loading and error state the screen binds to.
    /// Returns false when it failed, so the caller can retry rather than treat the screen as loaded.
    /// </summary>
    public async Task<bool> LoadAsync()
    {
        IsLoading = true;
        LoadError = "";
        Failure = LoadFailure.None;
        try
        {
            await InitializeAsync();
            OnPropertyChanged(nameof(IsFirstLoad));
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // The archive is up and answered. Saying "check your connection" here would send
            // the reader to fix something that is not broken.
            Failure = LoadFailure.TooManyRequests;
            LoadError = TooManyRequestsMessage;
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Failure = LoadFailure.Offline;
            LoadError = OfflineMessage;
            return false;
        }
        catch (Exception)
        {
            Failure = LoadFailure.Unexpected;
            LoadError = UnexpectedMessage;
            throw;   // the host logs it; the screen already shows the reader something honest
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Pull-to-refresh, and the retry button. InitializeAsync is written to run once - it
    /// returns early when its collections are already filled - so refreshing empties them
    /// first and loads again.
    /// </summary>
    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        try
        {
            ClearForReload();
            await LoadAsync();
        }
        finally
        {
            // The spinner must stop even when the API is unreachable, or the screen
            // is stuck mid-pull with no way back.
            IsRefreshing = false;
        }
    }

    /// <summary>Empty whatever InitializeAsync guards on. Override wherever that guard exists.</summary>
    protected virtual void ClearForReload() { }
}
