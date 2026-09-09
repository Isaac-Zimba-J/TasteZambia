using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed class ContributionRowViewModel(Contribution contribution)
{
    public string Name { get; } = contribution.Name;
    public string Meta { get; } = contribution.Meta;

    public string StatusLabel { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "Published",
        ContributionStatus.InReview => "In review",
        _ => "Draft",
    };

    public string StatusBackgroundHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#EEF2EC",
        ContributionStatus.InReview => "#F7EEDA",
        _ => "#F0ECE4",
    };

    public string StatusTextHex { get; } = contribution.Status switch
    {
        ContributionStatus.Published => "#2F6A4D",
        ContributionStatus.InReview => "#7A5A10",
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

    public override async Task InitializeAsync()
    {
        if (Collections.Count > 0) return;

        var profile = await profiles.GetAsync();

        Name = profile.Name;
        Meta = $"{profile.Location} · {profile.Languages}";
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
            Contributions.Add(new ContributionRowViewModel(contribution));

        foreach (var row in SeedData.SettingsRows)
            SettingsRows.Add(row);
    }

    [RelayCommand] private Task OpenShare() => Navigation.GoToAsync("share");
    [RelayCommand] private Task OpenFamily() => Navigation.GoToAsync("family");
    [RelayCommand] private Task OpenSettings() => Navigation.GoToAsync("settings");
}
