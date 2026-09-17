using Microsoft.Extensions.Logging;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views;

/// <summary>
/// A view that binds a ViewModel and initialises it exactly once, the first time it is
/// loaded. Every screen in the app follows this pattern; having it in one place means
/// the awkward part - an async handler on a synchronous event - is written once, and
/// written so a failure is logged rather than escaping as an unhandled crash.
/// </summary>
public abstract class LoadOnceView : ContentView
{
    private bool _loaded;

    protected LoadOnceView(BaseViewModel viewModel)
    {
        BindingContext = viewModel;
        Loaded += OnLoadedOnce;
    }

    protected BaseViewModel ViewModel => (BaseViewModel)BindingContext;

    /// <summary>Runs after the first load. Override to do more than initialise the ViewModel.</summary>
    protected virtual Task LoadAsync() => ViewModel.InitializeAsync();

    private async void OnLoadedOnce(object? sender, EventArgs e)
    {
        if (_loaded) return;

        try
        {
            await LoadAsync();

            // Only a successful load counts. Tab views are singletons that are re-attached
            // on every visit, so a failure - the API down at launch, say - is retried the
            // next time the user comes back to the tab rather than leaving it blank forever.
            _loaded = true;
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
