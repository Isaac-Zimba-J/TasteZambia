using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public int BackCount { get; private set; }
    public Task GoToAsync(string r) => Task.CompletedTask;
    public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

public class ShareViewModelTests
{
    private static ShareViewModel Sut(IContributionService? svc = null, INavigationService? nav = null)
        => new(svc ?? TestServices.Contributions(), new InMemoryRegionRepository(), new InMemoryIngredientRepository(), nav ?? new Nav(),
            new FakePhotoPicker(), new MediaUploader(TestServices.NoNetwork(), new InMemoryLocalStore(), TimeProvider.System, new TemporaryAppStorage()));

    /// <summary>Types the walkthrough recipe into the wizard the way a contributor would.</summary>
    private static void TypeWalkthrough(ShareViewModel vm)
    {
        var w = SeedData.WalkthroughShareDraft();
        vm.Draft.LocalName = w.LocalName;
        vm.Draft.EnglishDescription = w.EnglishDescription;
        vm.Province = w.Province;
        vm.MealType = w.MealType;
        vm.Ingredients.Clear();
        foreach (var i in w.Ingredients) vm.Ingredients.Add(new DraftIngredientViewModel { Name = i.DisplayName, Quantity = i.Quantity });
        vm.Steps.Clear();
        foreach (var st in w.Steps) { vm.AddStepCommand.Execute(null); vm.Steps[^1].Text = st; }
        vm.Draft.Origin = w.Origin;
        vm.Draft.CulturalSignificance = w.CulturalSignificance;
        vm.Draft.TraditionalMethod = w.TraditionalMethod;
    }

    [Fact]
    public async Task StartsOnStepOneWithNoBackButton()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("Recipe basics", vm.StepTitle);
        Assert.True(vm.IsStep1);
        Assert.False(vm.CanGoBack);
        Assert.Equal("Continue", vm.NextLabel);
        Assert.True(vm.IsPending);
    }

    [Fact]
    public async Task StartsBlank_WithOneEmptyIngredientAndStepRow_AndTheProvinceList()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("", vm.Draft.LocalName);
        Assert.Equal("", vm.Province);
        Assert.Single(vm.Ingredients);
        Assert.Single(vm.Steps);
        Assert.Equal(1, vm.Steps[0].Number);
        Assert.Contains("Northern", vm.Provinces);
        Assert.Equal(10, vm.Provinces.Count);
        Assert.True(vm.CreditTeacher);
    }

    [Fact]
    public async Task Continue_RefusesAnIncompleteStepAndSaysWhat()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal(1, vm.Step);
        Assert.Equal("Give the dish its local name.", vm.ValidationMessage);

        vm.Draft.LocalName = "Munkoyo";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal("Add a short description in English.", vm.ValidationMessage);

        vm.Draft.EnglishDescription = "A fermented maize drink";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal("Choose the province it comes from.", vm.ValidationMessage);

        vm.Province = "Central";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.Step);
        Assert.Equal("", vm.ValidationMessage);

        await vm.NextCommand.ExecuteAsync(null);   // blank ingredient and step rows do not count
        Assert.Equal(2, vm.Step);
        Assert.Equal("List at least one ingredient.", vm.ValidationMessage);
    }

    [Fact]
    public async Task IngredientRows_LinkToTheArchiveByName_AndBlankRowsAreDropped()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        TypeWalkthrough(vm);
        vm.Ingredients.Add(new DraftIngredientViewModel());   // an empty extra row
        vm.Ingredients.Add(new DraftIngredientViewModel { Name = "Groundnuts", Quantity = "1 cup" });   // English name

        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.Step);
        Assert.Equal(4, vm.Draft.Ingredients.Count);
        Assert.Equal("chibwabwa", vm.Draft.Ingredients[0].IngredientKey);
        Assert.True(vm.Draft.Ingredients[0].IsLinked);
        Assert.Null(vm.Draft.Ingredients[2].IngredientKey);   // Salt: not in the archive
        Assert.Equal("mbalala", vm.Draft.Ingredients[3].IngredientKey);
        Assert.Equal(2, vm.Draft.Steps.Count);
    }

    [Fact]
    public async Task RemovingAStep_Renumbers()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.AddStepCommand.Execute(null);
        vm.AddStepCommand.Execute(null);
        Assert.Equal([1, 2, 3], vm.Steps.Select(s => s.Number));

        vm.RemoveStepCommand.Execute(vm.Steps[0]);

        Assert.Equal([1, 2], vm.Steps.Select(s => s.Number));
    }

    [Fact]
    public async Task Close_KeepsATypedDraft_ButNotAnEmptyOne()
    {
        var svc = TestServices.Contributions();
        var vm = Sut(svc);
        await vm.InitializeAsync();
        await vm.CloseCommand.ExecuteAsync(null);
        Assert.Empty(svc.Drafts);

        vm.Draft.LocalName = "Munkoyo";
        await vm.CloseCommand.ExecuteAsync(null);
        Assert.Single(svc.Drafts);
    }

    [Fact]
    public async Task StepFour_ShowsThePipelineAndTheSubmitLabel()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        TypeWalkthrough(vm);

        for (var i = 0; i < 3; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Step);
        Assert.True(vm.IsStep4);
        Assert.Equal("Submit for review", vm.NextLabel);
        Assert.Equal(4, vm.Pipeline.Count);
        Assert.True(vm.Pipeline[0].IsComplete);
        Assert.False(vm.Pipeline[2].IsComplete);
    }

    [Fact]
    public async Task SubmittingFromStepFour_SwitchesToTheConfirmation()
    {
        var fake = new FakeContributionService();
        var vm = Sut(fake);
        await vm.InitializeAsync();
        TypeWalkthrough(vm);

        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.True(vm.IsSubmitted);
        Assert.False(vm.IsPending);
        Assert.False(vm.IsStep4);
        Assert.Equal("Chibwabwa na Mbalala", vm.SubmittedName);
        Assert.Equal("Northern Province · submitted just now", vm.SubmittedMeta);
        Assert.NotNull(vm.SubmittedId);
        Assert.Single(fake.Submitted);
    }

    [Fact]
    public async Task EveryStepForward_SavesTheDraftLocally()
    {
        var svc = TestServices.Contributions();
        var vm = Sut(svc);
        await vm.InitializeAsync();
        TypeWalkthrough(vm);

        await vm.NextCommand.ExecuteAsync(null);

        var saved = Assert.Single(svc.Drafts);
        Assert.Equal("Chibwabwa na Mbalala", saved.Name);
        Assert.Equal(saved.Id, vm.DraftId);
    }

    [Fact]
    public async Task SubmittingOffline_KeepsTheDraftAndSaysSo()
    {
        var svc = TestServices.Contributions();   // no network
        var vm = Sut(svc);
        await vm.InitializeAsync();
        TypeWalkthrough(vm);

        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.False(vm.IsSubmitted);
        Assert.True(vm.IsStep4);
        Assert.Contains("draft is saved", vm.SubmitError);
        Assert.Single(svc.Drafts);
    }

    [Fact]
    public async Task ContinuingADraft_LoadsItInsteadOfTheWalkthrough()
    {
        var svc = TestServices.Contributions();
        var local = svc.SaveDraft(new ContributionDraft { LocalName = "Munkoyo", Province = "Central" });
        var vm = Sut(svc);
        vm.DraftId = local.Id;

        await vm.InitializeAsync();

        Assert.Equal("Munkoyo", vm.Draft.LocalName);
    }

    [Fact]
    public async Task Reset_ReturnsToStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        vm.ResetCommand.Execute(null);

        Assert.False(vm.IsSubmitted);
        Assert.Equal(1, vm.Step);
    }

    [Fact]
    public async Task Back_NeverGoesBelowStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.BackCommand.Execute(null);

        Assert.Equal(1, vm.Step);
    }

    // ---- The validation message must not outlive the problem ----

    [Fact]
    public async Task TypingTheIngredientItAskedFor_TakesTheComplaintDown()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.Draft.LocalName = "Ifisashi";
        vm.Draft.EnglishDescription = "Groundnuts and greens";
        vm.Province = "Northern";
        vm.NextCommand.Execute(null);

        vm.NextCommand.Execute(null);                       // step 2, nothing filled in
        Assert.Equal("List at least one ingredient.", vm.ValidationMessage);

        vm.Ingredients[0].Name = "Chibwabwa";
        vm.Ingredients[0].Quantity = "2 bundles";

        // Still complaining, but now about the steps - not about the ingredient just typed.
        Assert.Equal("Write at least one cooking step.", vm.ValidationMessage);

        vm.Steps[0].Text = "Boil the greens until soft.";

        Assert.Empty(vm.ValidationMessage);                 // nothing left to say
        vm.NextCommand.Execute(null);
        Assert.Equal(3, vm.Step);
    }

    [Fact]
    public async Task AddingARow_DoesNotByItselfClearTheComplaint()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.Draft.LocalName = "Ifisashi";
        vm.Draft.EnglishDescription = "Groundnuts and greens";
        vm.Province = "Northern";
        vm.NextCommand.Execute(null);
        vm.NextCommand.Execute(null);

        vm.AddIngredientCommand.Execute(null);

        // An empty row is not an ingredient; saying otherwise would be a lie about progress.
        Assert.Equal("List at least one ingredient.", vm.ValidationMessage);
    }

    [Fact]
    public async Task RemovingTheOnlyIngredient_BringsTheComplaintBack()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.Draft.LocalName = "Ifisashi";
        vm.Draft.EnglishDescription = "Groundnuts and greens";
        vm.Province = "Northern";
        vm.NextCommand.Execute(null);
        vm.Ingredients[0].Name = "Chibwabwa";
        vm.Steps[0].Text = "Boil the greens.";
        vm.NextCommand.Execute(null);
        Assert.Equal(3, vm.Step);

        vm.BackCommand.Execute(null);
        vm.RemoveIngredientCommand.Execute(vm.Ingredients[0]);
        vm.NextCommand.Execute(null);

        Assert.Equal("List at least one ingredient.", vm.ValidationMessage);
        Assert.Equal(2, vm.Step);
    }

    [Fact]
    public async Task ARecipeSentForReview_IsConfirmedByName()
    {
        var toast = new RecordingToastService();
        var signal = new ArchiveSignal();
        var vm = new ShareViewModel(new FakeContributionService(), new InMemoryRegionRepository(),
            new InMemoryIngredientRepository(), new Nav(), new FakePhotoPicker(),
            new MediaUploader(TestServices.NoNetwork(), new InMemoryLocalStore(), TimeProvider.System, new TemporaryAppStorage()),
            toast, signal);
        await vm.InitializeAsync();
        TypeWalkthrough(vm);
        var before = signal.Version;

        for (var i = 0; i < 4; i++) await vm.NextCommand.ExecuteAsync(null);

        Assert.True(vm.IsSubmitted);
        Assert.Contains(vm.SubmittedName, toast.Last);
        Assert.Contains("sent for review", toast.Last);
        Assert.NotEqual(before, signal.Version);        // the Profile's counts are stale now
    }
}

public class FamilyViewModelTests
{
    private static FamilyViewModel Sut() => new(TestServices.Contributions(), new FakeFamilyService(), new Nav());

    /// <summary>
    /// Fills what the wizard requires before it will advance, so a test about step titles or
    /// privacy options is not really a test about validation.
    /// </summary>
    private static void FillRequired(FamilyViewModel vm)
    {
        vm.Draft.LocalName = "Ifisashi ya Banakulu";
        vm.Draft.Province = "Northern";
        vm.Draft.TaughtBy = "Banakulu Mwaba, my father's mother";
    }

    [Fact]
    public async Task StartsOnTheRecipeStepWithABlankDraft()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("The recipe", vm.StepTitle);
        Assert.Equal("", vm.Draft.LocalName);
        Assert.Equal("", vm.Draft.Language);
        Assert.False(vm.CanGoBack);
    }

    [Fact]
    public async Task StepTitlesFollowTheDesign()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        FillRequired(vm);
        var titles = new List<string> { vm.StepTitle };
        for (var i = 0; i < 3; i++)
        {
            vm.NextCommand.Execute(null);
            titles.Add(vm.StepTitle);
        }

        Assert.Equal(["The recipe", "Who taught you", "The story", "Who can see it"], titles);
    }

    [Fact]
    public async Task FinalStepOffersThreePrivacyLevelsWithFamilySelected()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        FillRequired(vm);
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        Assert.True(vm.IsStep4);
        Assert.Equal("Save to the archive", vm.NextLabel);
        Assert.Equal(3, vm.PrivacyOptions.Count);
        Assert.True(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.SharedWithFamily, vm.Draft.Privacy);
    }

    [Fact]
    public async Task ChoosingAPrivacyLevel_UpdatesTheDraftAndMovesTheSelection()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        FillRequired(vm);
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        vm.PrivacyOptions[2].SelectCommand.Execute(null);

        Assert.True(vm.PrivacyOptions[2].IsSelected);
        Assert.False(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.PublicInArchive, vm.Draft.Privacy);
    }

    [Fact]
    public async Task NextOnTheFinalStep_DoesNotAdvancePastFour()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        FillRequired(vm);
        for (var i = 0; i < 6; i++) vm.NextCommand.Execute(null);

        Assert.Equal(4, vm.Step);
    }
}
