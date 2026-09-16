using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ProfileViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task Initialize_LoadsChandaAndAllFourLists()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chanda Mwaba", vm.Name);
        Assert.Equal("Kitwe, Copperbelt · Bemba, English", vm.Meta);
        Assert.Equal(23, vm.CookedCount);
        Assert.Equal(14, vm.FavouriteCount);
        Assert.Equal(3, vm.ContributedCount);
        Assert.Equal(4, vm.PreservedCount);
        Assert.Equal(4, vm.Collections.Count);
        Assert.Equal(3, vm.Contributions.Count);
        Assert.Equal(5, vm.SettingsRows.Count);
    }

    [Fact]
    public async Task ContributionStatusesGetTheirV2Colours()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Published", vm.Contributions[0].StatusLabel);
        Assert.Equal("#EEF2EC", vm.Contributions[0].StatusBackgroundHex);
        Assert.Equal("#2F6A4D", vm.Contributions[0].StatusTextHex);

        Assert.Equal("In review", vm.Contributions[1].StatusLabel);
        Assert.Equal("#F7EEDA", vm.Contributions[1].StatusBackgroundHex);
        Assert.Equal("#7A5A10", vm.Contributions[1].StatusTextHex);

        Assert.Equal("Draft", vm.Contributions[2].StatusLabel);
        Assert.Equal("#F0ECE4", vm.Contributions[2].StatusBackgroundHex);
        Assert.Equal("#6B5C4A", vm.Contributions[2].StatusTextHex);
    }

    [Fact]
    public async Task EachCollectionOpensItsOwnScreen()
    {
        var nav = new Nav();
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), nav);
        await vm.InitializeAsync();

        foreach (var collection in vm.Collections)
            collection.OpenCommand.Execute(null);

        Assert.Equal(["favs", "wantTry", "cooked", "famList"], nav.Routes);
    }

    [Fact]
    public async Task ActionCardsAndSettingsNavigate()
    {
        var nav = new Nav();
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), nav);
        await vm.InitializeAsync();

        vm.OpenShareCommand.Execute(null);
        vm.OpenFamilyCommand.Execute(null);
        vm.OpenSettingsCommand.Execute(null);

        Assert.Equal(["shareStart", "family", "settings"], nav.Routes);
    }

    [Fact]
    public async Task CollectionTintsMatchTheDesign()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(["#A3452A", "#C07F1E", "#2F6A4D", "#17402F"],
                     vm.Collections.Select(c => c.Tint));
    }
}
