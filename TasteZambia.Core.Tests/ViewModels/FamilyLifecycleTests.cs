using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class FamilyLifecycleTests
{
    private static FamilyRecipeDto SomeRecipe() => new(Guid.NewGuid(), "Ifisashi ya Banakulu", "", "Northern", "Bemba",
        "Banakulu Mwaba, Mungwi", "", "She cooked this every time we arrived.", "Clay pot on the mbaula.",
        PrivacyLevel.SharedWithFamily, TranscriptState.None, null, 100, true, DateTimeOffset.UtcNow,
        [], [], []);

    [Fact]
    public async Task TheShelf_ShowsWhatIsPreserved_AndSaysSoWhenNothingIs()
    {
        var family = new FakeFamilyService();
        var vm = new FamStartViewModel(family, new Nav());

        await vm.LoadAsync();
        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.Recipes);

        family.Add(new FamilyRecipeDto(Guid.NewGuid(), "Ifisashi ya Banakulu", "", "Northern", "Bemba",
            "Banakulu Mwaba, Mungwi", "", "She cooked this every time we arrived.", "Clay pot on the mbaula.",
            PrivacyLevel.SharedWithFamily, TranscriptState.None, null, 100, true, DateTimeOffset.UtcNow,
            [], [], []));

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.False(vm.IsEmpty);
        var row = Assert.Single(vm.Recipes);
        Assert.Equal("Ifisashi ya Banakulu", row.Name);
        Assert.Equal("Banakulu Mwaba, Mungwi", row.TaughtBy);
        Assert.Equal("Family", row.Privacy);
    }

    [Fact]
    public async Task ExtrasReadTheWayTheDesignWritesThem()
    {
        Assert.Equal("Audio 12:40 · 3 photos · 2 family notes",
            PreservedRecipe.Extras(hasAudio: true, audioLength: "12:40", photos: 3, notes: 2, percentComplete: 100));
        Assert.Equal("1 photo · no audio yet",
            PreservedRecipe.Extras(hasAudio: false, audioLength: "", photos: 1, notes: 0, percentComplete: 100));
        Assert.Equal("Draft · 40% complete",
            PreservedRecipe.Extras(hasAudio: false, audioLength: "", photos: 0, notes: 0, percentComplete: 40));
    }

    [Fact]
    public async Task InvitingARelative_ShowsTheCodeToShare()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe());
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        vm.NewMemberName = "Mutinta Mwaba";
        vm.NewMemberRelation = "Sister, Lusaka";
        await vm.InviteCommand.ExecuteAsync(null);

        Assert.Equal(8, vm.InviteCode.Length);
        Assert.Contains(vm.Members, m => m.Name == "Mutinta Mwaba" && m.Status == "Invited");
        Assert.Equal("", vm.NewMemberName);   // the field clears, ready for the next one
    }

    [Fact]
    public async Task ChangingPrivacyToPublic_WarnsThatItLeavesTheFamily()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe());
        var vm = new FamPublicViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        await vm.SetPublicCommand.ExecuteAsync(null);

        Assert.Equal(PrivacyLevel.PublicInArchive, family.PrivacyOf(id));
        Assert.Contains("everyone", vm.PrivacyNote, StringComparison.OrdinalIgnoreCase);
    }

    // Pins the "Important" fix: a write the API refuses because the caller is not the owner
    // (a 404, same as "not found") must not be reported as a connectivity problem.

    [Fact]
    public async Task RemovingAMember_WhenTheApiRefusesTheWrite_SaysSoRatherThanBlamingTheConnection()
    {
        var family = new FakeFamilyService { DenyWrites = true };
        var id = family.Add(SomeRecipe());
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        await vm.RemoveMemberCommand.ExecuteAsync(new FamilyMember(Guid.NewGuid(), "Mutinta", "Sister", "Joined", "", ""));

        Assert.Equal(BaseViewModel.NotAllowedMessage, vm.ActionError);
    }

    [Fact]
    public async Task PublishingToEveryone_WhenTheApiRefusesTheWrite_SaysSoRatherThanBlamingTheConnection()
    {
        var family = new FakeFamilyService { DenyWrites = true };
        var id = family.Add(SomeRecipe());
        var vm = new FamPublicViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        await vm.SetPublicCommand.ExecuteAsync(null);

        Assert.Equal(BaseViewModel.NotAllowedMessage, vm.PrivacyNote);
        Assert.Equal(PrivacyLevel.SharedWithFamily, family.PrivacyOf(id));   // unchanged - the refusal was not applied locally
    }
}
