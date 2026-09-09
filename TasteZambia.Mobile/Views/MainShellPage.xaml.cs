using TasteZambia.Mobile.Views.Sections;

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
    };

    /// <summary>Views pushed over the current section, most recent last.</summary>
    private readonly List<View> _stack = [];

    private string _section = "home";

    public MainShellPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        Navigate("//home", null);
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
