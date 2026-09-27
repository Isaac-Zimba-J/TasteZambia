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
        // It must not claim a place in the public archive, because opening a family recipe
        // does not create one - it only widens who may read it.
        Assert.DoesNotContain("in the public archive.", vm.PrivacyNote);
        Assert.Contains("link", vm.PrivacyNote, StringComparison.OrdinalIgnoreCase);
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

    // ---- The owner is on the member list. Every screen that counts who can see a recipe
    // ---- used to count them twice, or not notice they were there at all. ----

    [Fact]
    public async Task ARecipeNobodyWasInvitedTo_SaysJustYou()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with { Members = [FakeFamilyService.Owner] });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        // It used to read "FAMILY ONLY · 2 PEOPLE" with nobody invited.
        Assert.Equal("FAMILY ONLY · JUST YOU", vm.AccessBadge);
    }

    [Fact]
    public async Task OneInvitedRelative_CountsAsOneOther()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with
        {
            Members = [FakeFamilyService.Owner, new FamilyMemberDto(Guid.NewGuid(), "Mutinta Mwaba", "Sister", MemberState.Joined, false)],
        });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        Assert.Equal("FAMILY ONLY · YOU AND 1 OTHER", vm.AccessBadge);
    }

    [Fact]
    public async Task TheOwnersOwnRow_ReadsAsThemAndCannotBeRemoved()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with
        {
            Members = [FakeFamilyService.Owner, new FamilyMemberDto(Guid.NewGuid(), "Mutinta", "Sister", MemberState.Joined, false)],
        });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        var owner = Assert.Single(vm.Members, m => m.IsOwner);
        Assert.Equal("You", owner.Status);
        Assert.False(owner.CanRemove);          // the archive always refuses; do not offer it
        Assert.True(vm.Members.Single(m => !m.IsOwner).CanRemove);
    }

    [Fact]
    public async Task ProvenanceSaysNotYetShared_UntilSomeoneElseIsOnIt()
    {
        var family = new FakeFamilyService();
        var alone = family.Add(SomeRecipe() with { Members = [FakeFamilyService.Owner] });
        var vm = new FamPublicViewModel(family, new Nav()) { Id = alone };
        await vm.LoadAsync();

        // This branch was unreachable: the owner made the count always at least one.
        Assert.Contains(vm.Provenance, p => p.Label == "Not yet shared");

        var shared = family.Add(SomeRecipe() with
        {
            Members = [FakeFamilyService.Owner, new FamilyMemberDto(Guid.NewGuid(), "Mutinta", "Sister", MemberState.Joined, false)],
        });
        var sharedVm = new FamPublicViewModel(family, new Nav()) { Id = shared };
        await sharedVm.LoadAsync();

        var step = Assert.Single(sharedVm.Provenance, p => p.Label == "Shared with family");
        Assert.Equal("1 other person has access.", step.Detail);   // was "1 member have access"
    }

    [Fact]
    public async Task TwoInvitedRelatives_ReadAsPlural()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with
        {
            Members =
            [
                FakeFamilyService.Owner,
                new FamilyMemberDto(Guid.NewGuid(), "Mutinta", "Sister", MemberState.Joined, false),
                new FamilyMemberDto(Guid.NewGuid(), "Bwalya", "Aunt", MemberState.Joined, false),
            ],
        });
        var vm = new FamPublicViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        Assert.Equal("2 other people have access.", Assert.Single(vm.Provenance, p => p.Label == "Shared with family").Detail);
    }

    // ---- The wizard had no validation, so the archive's "you left the name empty" arrived
    // ---- as "Could not reach the archive. Check your connection." ----

    [Fact]
    public async Task TheWizardSaysWhatIsMissing_RatherThanLettingTheArchiveRefuse()
    {
        var family = new FakeFamilyService();
        var vm = new FamilyViewModel(TestServices.Contributions(), family, new Nav());
        await vm.LoadAsync();
        vm.Draft.LocalName = "";
        vm.Draft.Province = "";

        await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.Step);   // it did not advance
        Assert.Equal("Give the recipe the name your family calls it.", vm.SubmitError);

        vm.Draft.LocalName = "Ifisashi ya Banakulu";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal("Choose the province it comes from.", vm.SubmitError);

        vm.Draft.Province = "Northern";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.Step);
        Assert.Equal("", vm.SubmitError);
    }

    [Fact]
    public async Task WhoTaughtYou_IsWhatTheRecordIsFor_SoItIsRequired()
    {
        var family = new FakeFamilyService();
        var vm = new FamilyViewModel(TestServices.Contributions(), family, new Nav());
        await vm.LoadAsync();
        vm.Draft.LocalName = "Ifisashi ya Banakulu";
        vm.Draft.Province = "Northern";
        vm.Draft.TaughtBy = "";

        await vm.NextCommand.ExecuteAsync(null);   // to step 2
        await vm.NextCommand.ExecuteAsync(null);   // refused

        Assert.Equal(2, vm.Step);
        Assert.Contains("who taught you", vm.SubmitError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GoingBack_ClearsWhateverWasComplainedAbout()
    {
        var family = new FakeFamilyService();
        var vm = new FamilyViewModel(TestServices.Contributions(), family, new Nav());
        await vm.LoadAsync();
        vm.Draft.LocalName = "Ifisashi";
        vm.Draft.Province = "Northern";
        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);
        Assert.NotEqual("", vm.SubmitError);

        vm.BackCommand.Execute(null);

        Assert.Equal("", vm.SubmitError);
    }

    [Fact]
    public async Task ARefusalFromTheArchive_IsNotBlamedOnTheConnection()
    {
        var family = new FakeFamilyService { RefuseCreate = true };
        var vm = new FamilyViewModel(TestServices.Contributions(), family, new Nav());
        await vm.LoadAsync();
        vm.Draft.LocalName = "Ifisashi ya Banakulu";
        vm.Draft.Province = "Northern";
        vm.Draft.TaughtBy = "Banakulu Mwaba";

        for (var i = 0; i < 4; i++) await vm.NextCommand.ExecuteAsync(null);

        Assert.NotEqual(BaseViewModel.OfflineMessage, vm.SubmitError);
        Assert.Contains("archive", vm.SubmitError, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Notes were read-only while the privacy screen promised relatives could add them,
    // ---- and a recipe you had lost access to rendered as a blank page. ----

    [Fact]
    public async Task ARelativeCanAddANote_AndItAppearsWhereTheFamilyReadsIt()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with { Members = [FakeFamilyService.Owner] });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        Assert.False(vm.AddNoteCommand.CanExecute(null));   // nothing typed yet

        vm.NewNote = "She never used tomato in this.";
        Assert.True(vm.AddNoteCommand.CanExecute(null));

        await vm.AddNoteCommand.ExecuteAsync(null);

        Assert.Contains(vm.Notes, n => n.Body == "She never used tomato in this.");
        Assert.Equal("", vm.NewNote);        // the box clears, ready for the next one
        Assert.Equal("", vm.ActionError);
    }

    [Fact]
    public async Task ANoteOfNothingButSpaces_IsNotSent()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with { Members = [FakeFamilyService.Owner] });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        vm.NewNote = "   ";

        Assert.False(vm.AddNoteCommand.CanExecute(null));
    }

    [Fact]
    public async Task ARecipeYouCanNoLongerOpen_SaysSoRatherThanGoingBlank()
    {
        var family = new FakeFamilyService();
        var gone = Guid.NewGuid();   // never added: the archive answers 404

        foreach (var vm in new BaseViewModel[]
                 {
                     new FamSharedViewModel(family, new Nav()) { Id = gone },
                     new FamPublicViewModel(family, new Nav()) { Id = gone },
                 })
        {
            Assert.True(await vm.LoadAsync());   // a 404 is not a failure to load

            // Before this, LoadError stayed empty, HasContent was false and IsFirstLoad was
            // false - so LoadState showed nothing at all and the page was an empty shell.
            Assert.Equal(BaseViewModel.NoLongerAvailableMessage, vm.LoadError);
            Assert.True(vm.HasLoadError);
        }
    }

    [Fact]
    public async Task AFailedNote_SaysWhichKindOfFailureItWas()
    {
        var family = new FakeFamilyService { RefuseNotes = true };
        var id = family.Add(SomeRecipe() with { Members = [FakeFamilyService.Owner] });
        var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
        await vm.LoadAsync();

        vm.NewNote = "Something worth keeping.";
        await vm.AddNoteCommand.ExecuteAsync(null);

        Assert.NotEqual(BaseViewModel.OfflineMessage, vm.ActionError);
        Assert.NotEqual("", vm.ActionError);
        Assert.Equal("Something worth keeping.", vm.NewNote);   // not cleared: it was never kept
    }

    // ---- Opening a family recipe up widens who may read it. It does not list it in the
    // ---- public archive, and the screen used to say that it did. ----

    [Fact]
    public async Task OpeningARecipeUp_DoesNotClaimItIsListedInTheArchive()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with { Privacy = PrivacyLevel.PublicInArchive, Members = [FakeFamilyService.Owner] });
        var vm = new FamPublicViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        // PublishedDishId is never set from this screen, and the shelf query keeps a
        // stranger's opened-up recipe off every shelf - so "listed in the public archive"
        // described somewhere nobody could reach.
        Assert.DoesNotContain(vm.Provenance, p => p.Label == "Published and credited");
        Assert.Contains(vm.Provenance, p => p.Label == "Open to anyone with the link");

        var listing = Assert.Single(vm.Provenance, p => p.Label == "Listed in the public archive");
        Assert.Contains("Not yet", listing.Detail);
        Assert.Equal("#D8CDB9", listing.DotHex);   // the pending tone, not the gold of done
    }

    [Fact]
    public async Task AFamilyOnlyRecipe_SaysNobodyOutsideCanRead()
    {
        var family = new FakeFamilyService();
        var id = family.Add(SomeRecipe() with { Privacy = PrivacyLevel.SharedWithFamily, Members = [FakeFamilyService.Owner] });
        var vm = new FamPublicViewModel(family, new Nav()) { Id = id };

        await vm.LoadAsync();

        Assert.Contains(vm.Provenance, p => p.Label == "Not yet opened up");
        Assert.Equal("Only your family can see it right now.", vm.PrivacyNote);
    }
}
