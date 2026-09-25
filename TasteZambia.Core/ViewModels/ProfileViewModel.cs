using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Enums;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ContributionRowViewModel(Contribution contribution, INavigationService navigation)
{
    public Guid Id { get; } = contribution.Id;
    public string Name { get; } = contribution.Name;
    public string Meta { get; } = contribution.Meta;
    public bool IsReal => contribution.Id != Guid.Empty;

    /// <summary>Each status has its own screen in the design: published, questions, or the review timeline.</summary>
    [RelayCommand]
    private Task Open()
    {
        if (!IsReal) return Task.CompletedTask;
        var route = contribution.Status switch
        {
            ContributionStatus.Published => "sharePublished",
            ContributionStatus.ChangesRequested => "shareChanges",
            _ => "shareReview",
        };
        return navigation.GoToAsync(route, new Dictionary<string, object> { ["id"] = contribution.Id });
    }

    public string StatusLabel { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "Published",
        ContributionStatus.InReview => "In review",
        ContributionStatus.ChangesRequested => "Changes requested",
        ContributionStatus.Withdrawn => "Withdrawn",
        _ => "Draft",
    };

    public string StatusBackgroundHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#EEF2EC",
        ContributionStatus.InReview or ContributionStatus.ChangesRequested => "#F7EEDA",
        _ => "#F0ECE4",
    };

    public string StatusTextHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#2F6A4D",
        ContributionStatus.InReview or ContributionStatus.ChangesRequested => "#7A5A10",
        _ => "#6B5C4A",
    };
}

public sealed partial class CollectionRowViewModel(
    RecipeCollection collection, string route, INavigationService navigation)
{
    public string Label { get; } = collection.Label;
    public string CountLabel { get; } = collection.CountLabel;
    public string Tint { get; } = collection.Tint;

    [RelayCommand]
    private Task Open() => navigation.GoToAsync(route);
}

public sealed partial class ProfileViewModel(
    IProfileRepository profiles,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Collection order matches the design; each opens its own screen.</summary>
    private static readonly string[] CollectionRoutes = ["favs", "wantTry", "cooked", "famList"];

    public ObservableCollection<CollectionRowViewModel> Collections { get; } = [];
    public ObservableCollection<ContributionRowViewModel> Contributions { get; } = [];
    public ObservableCollection<string> SettingsRows { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _meta = "";
    [ObservableProperty] private string _avatarAsset = "";
    [ObservableProperty] private int _cookedCount;
    [ObservableProperty] private int _favouriteCount;
    [ObservableProperty] private int _contributedCount;
    [ObservableProperty] private int _preservedCount;
    [ObservableProperty] private bool _hasContributions;
    [ObservableProperty] private bool _hasProfileDetails;

    protected override bool HasContent => Collections.Count > 0;

    protected override void ClearForReload()
    {
        Collections.Clear();
        Contributions.Clear();
        SettingsRows.Clear();
    }

    public override async Task InitializeAsync()
    {
        if (Collections.Count > 0) return;

        var profile = await profiles.GetAsync();

        Name = profile.Name;
        Meta = string.Join(" · ", new[] { profile.Location, profile.Languages }.Where(x => x.Length > 0));
        HasProfileDetails = Meta.Length > 0;
        AvatarAsset = profile.AvatarAsset;
        CookedCount = profile.CookedCount;
        FavouriteCount = profile.FavouriteCount;
        ContributedCount = profile.ContributedCount;
        PreservedCount = profile.PreservedCount;

        var index = 0;
        foreach (var collection in await profiles.GetCollectionsAsync())
        {
            var route = index < CollectionRoutes.Length ? CollectionRoutes[index] : "favs";
            Collections.Add(new CollectionRowViewModel(collection, route, Navigation));
            index++;
        }

        foreach (var contribution in await profiles.GetContributionsAsync())
            Contributions.Add(new ContributionRowViewModel(contribution, Navigation));

        HasContributions = Contributions.Count > 0;

        foreach (var row in SeedData.SettingsRows)
            SettingsRows.Add(row);
    }

    // Straight into the wizard: Profile already has its own Preserve button, so the
    // Share/Preserve chooser would only repeat the choice the user just made.
    [RelayCommand] private Task OpenShare() => Navigation.GoToAsync("share");
    [RelayCommand] private Task OpenFamily() => Navigation.GoToAsync("famStart");
    [RelayCommand] private Task OpenSettings() => Navigation.GoToAsync("settings");
    [RelayCommand] private Task EditProfile() => Navigation.GoToAsync("profileEdit");
}
