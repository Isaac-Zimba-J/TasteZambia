using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public Task GoToAsync(string r) => Task.CompletedTask;
    public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
    public Task GoBackAsync() => Task.CompletedTask;
}

/// <summary>
/// A write that succeeds quietly has to say so, and has to leave the screens that show it
/// knowing they are out of date. Before this, adding a recipe looked like nothing had
/// happened until the app was closed and opened again.
/// </summary>
public class ConfirmationTests
{
    [Fact]
    public void TheSignalStartsStill_AndMovesOnlyWhenSomethingIsWritten()
    {
        var signal = new ArchiveSignal();
        var at = signal.Version;

        Assert.Equal(at, signal.Version);   // reading it does not move it

        signal.Changed();

        Assert.NotEqual(at, signal.Version);
    }

    [Fact]
    public async Task SavingAName_ConfirmsItAndMarksTheOtherScreensStale()
    {
        var toast = new RecordingToastService();
        var signal = new ArchiveSignal();
        var profiles = new InMemoryProfileRepository();
        var vm = new ProfileEditViewModel(profiles, new Nav(), toast, signal);
        var before = signal.Version;

        vm.Name = "Mwansa Chanda";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Saved", toast.Last);
        Assert.NotEqual(before, signal.Version);   // Home's initials and the Profile header
    }

    [Fact]
    public async Task AFailedSave_ConfirmsNothingAndLeavesTheSignalAlone()
    {
        var toast = new RecordingToastService();
        var signal = new ArchiveSignal();
        var vm = new ProfileEditViewModel(new OfflineProfileRepository(), new Nav(), toast, signal);
        var before = signal.Version;

        vm.Name = "Mwansa Chanda";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(toast.Shown);                 // nothing was saved, so nothing is confirmed
        Assert.Equal(before, signal.Version);
        Assert.NotEmpty(vm.ErrorMessage);          // the failure stays on screen instead
    }

    [Fact]
    public async Task AddingAFamilyNote_ConfirmsIt()
    {
        var toast = new RecordingToastService();
        var signal = new ArchiveSignal();
        var family = new FakeFamilyService();
        var id = family.Add(FamilyLifecycleTests.SomeRecipe());
        var vm = new FamSharedViewModel(family, new Nav(), toast, signal) { Id = id };
        await vm.LoadAsync();
        var before = signal.Version;

        vm.NewNote = "Mama added bicarbonate to the chikanda.";
        await vm.AddNoteCommand.ExecuteAsync(null);

        Assert.Equal("Note added", toast.Last);
        Assert.NotEqual(before, signal.Version);
    }

    [Fact]
    public async Task InvitingARelative_SaysWhoWasInvited_EvenThoughTheBoxIsClearedFirst()
    {
        var toast = new RecordingToastService();
        var family = new FakeFamilyService();
        var id = family.Add(FamilyLifecycleTests.SomeRecipe());
        var vm = new FamSharedViewModel(family, new Nav(), toast, new ArchiveSignal()) { Id = id };
        await vm.LoadAsync();

        vm.NewMemberName = "Mutinta Mwaba";
        vm.NewMemberRelation = "Sister, Lusaka";
        await vm.InviteCommand.ExecuteAsync(null);

        Assert.Contains("Mutinta Mwaba", toast.Last);
        Assert.Empty(vm.NewMemberName);
    }

    private sealed class OfflineProfileRepository : IProfileRepository
    {
        public Task<Core.Models.UserProfile> GetAsync(CancellationToken ct = default) => Task.FromResult(SeedData.Profile);
        public Task UpdateAsync(string n, string l, string g, CancellationToken ct = default) => throw new HttpRequestException("offline");
        public Task<IReadOnlyList<Core.Models.RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default) => Task.FromResult(SeedData.Collections);
        public Task<IReadOnlyList<Core.Models.Contribution>> GetContributionsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Core.Models.Contribution>>([]);
    }
}
