using Microsoft.Extensions.Logging;
using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views.Onboarding;

namespace TasteZambia.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly OnboardingViewModel _viewModel;

    private static readonly Dictionary<string, Type> Routes = new()
    {
        ["splash"]    = typeof(SplashView),
        ["intro"]     = typeof(IntroView),
        ["onbLang"]   = typeof(OnbLanguageView),
        ["onbWho"]    = typeof(OnbWhoView),
        ["onbTaste"]  = typeof(OnbTasteView),
        ["onbNotify"] = typeof(OnbNotifyView),
        ["onbReady"]  = typeof(OnbReadyView),
    };

    private readonly List<View> _stack = [];

    public OnboardingPage(IServiceProvider services, OnboardingViewModel viewModel)
    {
        InitializeComponent();
        _services = services;
        _viewModel = viewModel;

        // One ViewModel is shared by all seven screens, so it is initialised here, once,
        // before the first screen shows - not by any individual view.
        Loaded += OnLoadedOnce;
        Navigate("splash");
    }

    private bool _loaded;

    private async void OnLoadedOnce(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _services.GetService<ILoggerFactory>()?.CreateLogger<OnboardingPage>()
                .LogError(ex, "Failed to initialise onboarding");
        }
    }

    public bool Handles(string route) => Routes.ContainsKey(route);

    public void Navigate(string route)
    {
        if (!Routes.TryGetValue(route, out var type)) return;
        if (_services.GetService(type) is not View view) return;

        if (Region.Content is View current)
            _stack.Add(current);

        Region.Content = view;
    }

    public bool GoBack()
    {
        if (_stack.Count == 0) return false;

        var previous = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        Region.Content = previous;
        return true;
    }

    protected override bool OnBackButtonPressed() => GoBack() || base.OnBackButtonPressed();
}
