using CommunityToolkit.Mvvm.ComponentModel;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = "";

    /// <summary>Called by each page's OnAppearing. Override to load data.</summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;
}
