using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Enums;
using TasteZambia.Core.Models;

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
    public async Task ANewReader_HasEmptyCountsAndNoContributions()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Taste Zambia reader", vm.Name);
        Assert.Equal("", vm.Meta);
        Assert.False(vm.HasProfileDetails);
        Assert.Equal(0, vm.CookedCount);
        Assert.Equal(0, vm.FavouriteCount);
        Assert.Equal(0, vm.ContributedCount);
        Assert.Equal(0, vm.PreservedCount);
        Assert.Equal(4, vm.Collections.Count);   // the shelf stays; its counts are real
        Assert.Empty(vm.Contributions);
        Assert.False(vm.HasContributions);
        Assert.Equal(5, vm.SettingsRows.Count);
    }

    [Fact]
    public async Task SavingAProfile_ShowsTheNameAndDetails()
    {
        var repository = new InMemoryProfileRepository();
        await repository.UpdateAsync("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English");

        var vm = new ProfileViewModel(repository, new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chanda Mwaba", vm.Name);
        Assert.Equal("Kitwe, Copperbelt · Bemba, English", vm.Meta);
        Assert.True(vm.HasProfileDetails);
    }

    [Theory]
    [InlineData(ContributionStatus.Published, "Published", "#EEF2EC", "#2F6A4D")]
    [InlineData(ContributionStatus.InReview, "In review", "#F7EEDA", "#7A5A10")]
    [InlineData(ContributionStatus.ChangesRequested, "Changes requested", "#F7EEDA", "#7A5A10")]
    [InlineData(ContributionStatus.Draft, "Draft", "#F0ECE4", "#6B5C4A")]
    public void ContributionStatusesGetTheirV2Colours(ContributionStatus status, string label, string background, string text)
    {
        var row = new ContributionRowViewModel(new Contribution(Guid.NewGuid(), "Munkoyo", status, "Central Province"), new Nav());

        Assert.Equal(label, row.StatusLabel);
        Assert.Equal(background, row.StatusBackgroundHex);
        Assert.Equal(text, row.StatusTextHex);
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

        Assert.Equal(["share", "famStart", "settings"], nav.Routes);
    }

    [Fact]
    public async Task CollectionTintsMatchTheDesign()
    {
        var vm = new ProfileViewModel(new InMemoryProfileRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(["#A3452A", "#C07F1E", "#2F6A4D", "#17402F"],
                     vm.Collections.Select(c => c.Tint));
    }

    // ---- The editor's Save button ----

    [Fact]
    public void TheSaveButton_LightsUpAsSoonAsAReaderTypesAName()
    {
        var vm = new ProfileEditViewModel(new InMemoryProfileRepository(), new Nav());
        var announced = new List<string?>();
        vm.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

        Assert.False(vm.CanSave);

        vm.Name = "Mwansa";

        Assert.True(vm.CanSave);
        // The button's look is bound to CanSave, not to the command, so the view has to hear it.
        Assert.Contains(nameof(vm.CanSave), announced);
        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void TheSaveButton_GoesBackToGrey_WhenTheNameIsEmptiedAgain()
    {
        var vm = new ProfileEditViewModel(new InMemoryProfileRepository(), new Nav());
        vm.Name = "Mwansa";

        var announced = new List<string?>();
        vm.PropertyChanged += (_, e) => announced.Add(e.PropertyName);
        vm.Name = "   ";   // whitespace is not a name

        Assert.False(vm.CanSave);
        Assert.Contains(nameof(vm.CanSave), announced);
        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Saving_KeepsWhatTheReaderTyped_AndGoesBack()
    {
        var profiles = new InMemoryProfileRepository();
        var vm = new ProfileEditViewModel(profiles, new Nav());
        await vm.InitializeAsync();

        Assert.Equal("", vm.Name);   // the placeholder name is not something they chose

        vm.Name = " Mwansa Chanda ";
        vm.Location = "Kasama";
        vm.Languages = "Bemba, English";
        await vm.SaveCommand.ExecuteAsync(null);

        var saved = await profiles.GetAsync();
        Assert.Equal("Mwansa Chanda", saved.Name);
        Assert.Equal("Kasama", saved.Location);
        Assert.Equal("Bemba, English", saved.Languages);
        Assert.Equal("", vm.ErrorMessage);
    }
}
