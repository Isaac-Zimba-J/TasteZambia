using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _title = "";

    /// <summary>Called by each page's OnAppearing. Override to load data.</summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Pull-to-refresh. InitializeAsync is written to run once - it returns early when its
    /// collections are already filled - so refreshing empties them first and loads again.
    /// A screen with nothing to reload simply re-runs Initialize.
    /// </summary>
    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        try
        {
            ClearForReload();
            await InitializeAsync();
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
