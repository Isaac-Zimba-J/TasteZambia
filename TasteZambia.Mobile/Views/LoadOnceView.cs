using Microsoft.Extensions.Logging;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views;

/// <summary>
/// A view that binds a ViewModel and initialises it exactly once, the first time it is
/// loaded. Every screen in the app follows this pattern; having it in one place means
/// the awkward part - an async handler on a synchronous event - is written once, and
/// written so a failure is logged rather than escaping as an unhandled crash.
///
/// "Once" holds only while the archive has not changed. A screen that comes back to find
/// <see cref="IArchiveSignal"/> further along than when it loaded reloads itself, so a
/// recipe just submitted or a name just saved is on screen without a pull, and without
/// closing the app - which is what it used to take.
/// </summary>
public abstract class LoadOnceView : ContentView
{
    private bool _loaded;
    private int _loadedAtVersion;

    protected LoadOnceView(BaseViewModel viewModel)
    {
        BindingContext = viewModel;
        Loaded += OnLoadedOnce;
    }

    protected BaseViewModel ViewModel => (BaseViewModel)BindingContext;

    /// <summary>
    /// Runs after the first load. Goes through the ViewModel's LoadAsync so the screen
    /// carries its own loading and error state rather than sitting blank.
    /// </summary>
    protected virtual Task<bool> LoadAsync() => ViewModel.LoadAsync();

    private async void OnLoadedOnce(object? sender, EventArgs e)
    {
        var signal = Handler?.MauiContext?.Services.GetService<IArchiveSignal>();

        try
        {
            if (_loaded)
            {
                // Nothing has been written since; this screen is still current.
                if (signal is null || signal.Version == _loadedAtVersion) return;

                // Refresh rather than Load: it empties what InitializeAsync guards on first,
                // so the rows come back changed instead of doubling.
                await ViewModel.RefreshCommand.ExecuteAsync(null);
                _loadedAtVersion = signal.Version;
                return;
            }

            // Only a successful load counts. Tab views are singletons that are re-attached
            // on every visit, so a failure - the API down at launch, say - is retried the
            // next time the user comes back to the tab rather than leaving it blank forever.
            _loaded = await LoadAsync();
            if (_loaded) _loadedAtVersion = signal?.Version ?? 0;
        }
        catch (Exception ex)
        {
            // An HTTP failure or a bad route parameter must not take the app down.
            // The screen stays empty for now and the cause is in the log.
            Handler?.MauiContext?.Services
                .GetService<ILoggerFactory>()?
                .CreateLogger(GetType())
                .LogError(ex, "Failed to load {View}", GetType().Name);
        }
    }
}
