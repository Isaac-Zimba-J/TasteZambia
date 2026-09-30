using TasteZambia.Mobile.Views.Tabs;
using TasteZambia.Mobile.Views.Details;
using TasteZambia.Mobile.Views.Share;
using TasteZambia.Mobile.Views.Family;
using TasteZambia.Mobile.Views.Collections;
using TasteZambia.Mobile.Services;

namespace TasteZambia.Mobile.Views;

public partial class MainShellPage : ContentPage
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Route -> the view that renders it and the nav section it belongs to.
    /// Detail routes keep their parent tab lit, matching the design's NAV table.
    /// </summary>
    private static readonly Dictionary<string, (Type View, string Section)> Routes = new()
    {
        ["home"]    = (typeof(HomeView),    "home"),
        ["explore"] = (typeof(ExploreView), "explore"),
        ["regions"] = (typeof(RegionsView), "regions"),
        ["culture"] = (typeof(CultureView), "culture"),
        ["profile"] = (typeof(ProfileView), "profile"),

        // Detail routes keep their parent tab lit.
        ["recipe"]      = (typeof(RecipeView),      "explore"),
        ["ingredients"] = (typeof(IngredientsView), "explore"),
        ["ingredient"]  = (typeof(IngredientView),  "explore"),
        ["story"]       = (typeof(StoryView),       "culture"),
        ["share"]       = (typeof(ShareView),       "profile"),
        ["family"]      = (typeof(FamilyView),      "profile"),
        ["shareStart"]     = (typeof(ShareStartView),     "profile"),
        ["shareDraft"]     = (typeof(ShareDraftsView),    "profile"),
        ["shareReview"]    = (typeof(ShareReviewView),    "profile"),
        ["shareChanges"]   = (typeof(ShareChangesView),   "profile"),
        ["sharePublished"] = (typeof(SharePublishedView), "profile"),
        ["famStart"]  = (typeof(FamStartView),  "profile"),
        ["famDraft"]  = (typeof(FamDraftView),  "profile"),
        ["famSaved"]  = (typeof(FamSavedView),  "profile"),
        ["famShared"] = (typeof(FamSharedView), "profile"),
        ["famPublic"] = (typeof(FamPublicView), "profile"),
        ["favs"]     = (typeof(FavouritesView),    "profile"),
        ["wantTry"]  = (typeof(WantToTryView),     "profile"),
        ["cooked"]   = (typeof(CookedView),        "profile"),
        ["famList"]  = (typeof(FamilyRecipesView), "profile"),
        ["settings"]    = (typeof(SettingsView),     "profile"),
        ["profileEdit"] = (typeof(ProfileEditView),  "profile"),
    };

    /// <summary>Views pushed over the current section, most recent last.</summary>
    private readonly List<View> _stack = [];

    private string _section = "home";

    /// <summary>Which confirmation is currently on screen; a newer one supersedes it.</summary>
    private int _toastGeneration;

    public MainShellPage(IServiceProvider services, ToastService toasts)
    {
        InitializeComponent();
        _services = services;
        toasts.Requested += ShowToast;
        Navigate("//home", null);
    }

    /// <summary>
    /// Fades a confirmation in over the content, above the nav bar, and takes it away again.
    /// Deliberately short: it says the archive heard them, and is never the only place
    /// something important is written.
    /// </summary>
    private void ShowToast(string message)
    {
        var generation = ++_toastGeneration;

        Dispatcher.Dispatch(async () =>
        {
            ToastText.Text = message;
            Toast.IsVisible = true;
            await Toast.FadeToAsync(1, 160, Easing.CubicOut);
            await Task.Delay(2600);

            // A second confirmation arrived while this one was up; it owns the banner now.
            if (generation != _toastGeneration) return;

            await Toast.FadeToAsync(0, 220, Easing.CubicIn);
            if (generation == _toastGeneration) Toast.IsVisible = false;
        });
    }

    public void Navigate(string route, IDictionary<string, object>? parameters)
    {
        var isRoot = route.StartsWith("//");
        var key = isRoot ? route[2..] : route;

        if (!Routes.TryGetValue(key, out var entry))
            return;

        // Selecting the tab you are already on is a no-op, not a rebuild.
        if (isRoot && _stack.Count == 0 && _section == entry.Section && Region.Content is not null)
            return;

        if (_services.GetService(entry.View) is not View view)
            return;

        if (isRoot)
            _stack.Clear();
        else if (Region.Content is View current)
            _stack.Add(current);

        if (parameters is not null && view.BindingContext is not null)
            ApplyParameters(view, parameters);

        _section = entry.Section;
        Region.Content = view;
        NavBar.ActiveSection = _section;
    }

    public bool GoBack()
    {
        if (_stack.Count == 0)
            return false;

        var previous = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        Region.Content = previous;
        return true;
    }

    /// <summary>Android's back button pops our own stack before leaving the app.</summary>
    protected override bool OnBackButtonPressed() => GoBack() || base.OnBackButtonPressed();

    private static void ApplyParameters(View view, IDictionary<string, object> parameters)
    {
        var target = view.BindingContext!;
        var type = target.GetType();

        foreach (var (name, value) in parameters)
        {
            var property = type.GetProperty(name[..1].ToUpperInvariant() + name[1..]);
            if (property?.CanWrite == true)
                property.SetValue(target, value);
        }
    }
}
