using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Validation;

namespace TasteZambia.Core.ViewModels;

/// <summary>One editable ingredient row. Linked to the archive when the name matches an ingredient there.</summary>
public sealed partial class DraftIngredientViewModel : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLinked))] private string _name = "";
    [ObservableProperty] private string _quantity = "";

    /// <summary>Archive key when the typed name matches; set by the wizard, not the user.</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLinked))] private string? _ingredientKey;
    [ObservableProperty] private string _subtitle = "";

    public bool IsLinked => !string.IsNullOrEmpty(IngredientKey);

    public RecipeIngredient ToModel() => new()
    {
        IngredientKey = IngredientKey,
        DisplayName = Name.Trim(),
        DisplaySubtitle = Subtitle,
        Quantity = Quantity.Trim(),
    };
}

public sealed partial class DraftStepViewModel : ObservableObject
{
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private int _number;
}

/// <summary>One photo tile in the strip. Pending until the archive hands back an id.</summary>
public sealed partial class DraftPhotoViewModel : ObservableObject
{
    [ObservableProperty] private string _localPath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    private Guid? _mediaId;

    /// <summary>Still queued, or the reader just took it and the upload hasn't answered yet.</summary>
    public bool IsPending => MediaId is null;

    /// <summary>Set only while this photo sits in <see cref="IMediaUploader"/>'s retry queue, so
    /// removing it can cancel that queued send. Null once it is sent, rejected, or never queued.</summary>
    public Guid? QueuedLocalId { get; set; }
}

public sealed partial class ShareViewModel(
    IContributionService contributions,
    IRegionRepository regions,
    IIngredientRepository ingredientArchive,
    INavigationService navigation,
    IPhotoPicker photoPicker,
    IMediaUploader mediaUploader) : BaseViewModel(navigation)
{
    public static readonly IReadOnlyList<string> MealTypes =
        ["Relish", "Staple", "Meat dish", "Fish", "Vegetable dish", "Snack", "Drink", "Dessert"];

    public ContributionDraft Draft { get; private set; } = new();
    public ObservableCollection<ReviewStage> Pipeline { get; } = [];
    public ObservableCollection<string> Provinces { get; } = [];
    public ObservableCollection<DraftIngredientViewModel> Ingredients { get; } = [];
    public ObservableCollection<DraftStepViewModel> Steps { get; } = [];
    public ObservableCollection<DraftPhotoViewModel> Photos { get; } = [];

    private IReadOnlyList<Ingredient> _archiveIngredients = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private int _step = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    private bool _isSubmitted;

    [ObservableProperty] private string _submittedName = "";
    [ObservableProperty] private string _submittedMeta = "";
    [ObservableProperty] private string _submitError = "";

    /// <summary>What still needs filling on this step; empty when the step is complete.</summary>
    [ObservableProperty] private string _validationMessage = "";

    [ObservableProperty] private string _draftsLabel = "";

    /// <summary>Route parameter: continue an existing local draft instead of starting blank.</summary>
    public Guid? DraftId { get; set; }

    /// <summary>The contribution's id once submitted; the Review screen opens on it.</summary>
    public Guid? SubmittedId { get; private set; }

    public string StepTitle => SeedData.ShareSteps[Step - 1];
    public bool IsPending => !IsSubmitted;
    public bool IsStep1 => IsPending && Step == 1;
    public bool IsStep2 => IsPending && Step == 2;
    public bool IsStep3 => IsPending && Step == 3;
    public bool IsStep4 => IsPending && Step == 4;
    public bool CanGoBack => Step > 1;
    public string NextLabel => Step == 4 ? "Submit for review" : "Continue";

    // Province and meal type bind to Draft directly; the pickers need a selected-item shape.
    public string Province
    {
        get => Draft.Province;
        set { Draft.Province = value ?? ""; OnPropertyChanged(); }
    }

    public string MealType
    {
        get => Draft.MealType;
        set { Draft.MealType = value ?? ""; OnPropertyChanged(); }
    }

    public bool CreditTeacher
    {
        get => Draft.CreditTeacher;
        set { Draft.CreditTeacher = value; OnPropertyChanged(); }
    }

    public override async Task InitializeAsync()
    {
        if (Provinces.Count == 0)
            foreach (var p in await regions.GetAllAsync()) Provinces.Add(p.Name);
        if (_archiveIngredients.Count == 0)
            _archiveIngredients = await ingredientArchive.GetAllAsync();
        if (Pipeline.Count == 0)
            foreach (var stage in contributions.ReviewPipeline) Pipeline.Add(stage);

        Draft = DraftId is { } id && contributions.FindDraft(id) is { } local
            ? local.Draft
            : contributions.StartShareDraft();
        LoadRowsFromDraft();
        OnPropertyChanged(nameof(Draft));
        OnPropertyChanged(nameof(Province));
        OnPropertyChanged(nameof(MealType));
        OnPropertyChanged(nameof(CreditTeacher));
        RefreshDraftsLabel();
    }

    private void LoadRowsFromDraft()
    {
        Ingredients.Clear();
        foreach (var i in Draft.Ingredients)
            Ingredients.Add(new DraftIngredientViewModel { Name = i.DisplayName, Quantity = i.Quantity, IngredientKey = i.IngredientKey, Subtitle = i.DisplaySubtitle });
        Steps.Clear();
        foreach (var s in Draft.Steps) AddStepRow(s);
        if (Ingredients.Count == 0) AddIngredient();
        if (Steps.Count == 0) AddStep();

        // An uploaded photo's display copy is never referenced by the persisted draft (only
        // PendingPhotoPaths is); once these rows are replaced it would just be litter.
        PurgeUploadedDisplayCopies(Photos);
        Photos.Clear();
        // Uploaded photos have no local file left to show a thumbnail from; still queued
        // ones do, as long as the cache survived (it is deleted once the upload lands).
        foreach (var id in Draft.PhotoIds) Photos.Add(new DraftPhotoViewModel { MediaId = id });
        foreach (var path in Draft.PendingPhotoPaths.Where(File.Exists)) Photos.Add(new DraftPhotoViewModel { LocalPath = path });
    }

    private void SyncRowsToDraft()
    {
        foreach (var row in Ingredients) LinkToArchive(row);
        Draft.Ingredients = Ingredients.Where(i => i.Name.Trim().Length > 0).Select(i => i.ToModel()).ToList();
        Draft.Steps = Steps.Select(s => s.Text.Trim()).Where(t => t.Length > 0).ToList();
        Draft.PhotoIds = Photos.Where(p => p.MediaId is not null).Select(p => p.MediaId!.Value).ToList();
        Draft.PendingPhotoPaths = Photos.Where(p => p.MediaId is null).Select(p => p.LocalPath).ToList();
    }

    /// <summary>A typed name that matches an archive ingredient (by key, local or English name) links to it.</summary>
    private void LinkToArchive(DraftIngredientViewModel row)
    {
        var name = row.Name.Trim();
        var match = _archiveIngredients.FirstOrDefault(a =>
            string.Equals(a.Key, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(a.LocalName, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(a.EnglishName, name, StringComparison.OrdinalIgnoreCase));
        row.IngredientKey = match?.Key;
        row.Subtitle = match is null ? row.Subtitle : (string.Equals(match.LocalName, name, StringComparison.OrdinalIgnoreCase) ? match.EnglishName : match.LocalName);
    }

    [RelayCommand] private void ToggleCredit() => CreditTeacher = !CreditTeacher;

    [RelayCommand] private void AddIngredient() => Ingredients.Add(new DraftIngredientViewModel());
    [RelayCommand] private void RemoveIngredient(DraftIngredientViewModel row) => Ingredients.Remove(row);

    [RelayCommand] private void AddStep() => AddStepRow("");

    [RelayCommand]
    private void RemoveStep(DraftStepViewModel row)
    {
        Steps.Remove(row);
        Renumber();
    }

    private void AddStepRow(string text)
    {
        Steps.Add(new DraftStepViewModel { Text = text, Number = Steps.Count + 1 });
    }

    private void Renumber()
    {
        for (var i = 0; i < Steps.Count; i++) Steps[i].Number = i + 1;
    }

    [RelayCommand] private Task AddPhoto() => AttachAsync(photoPicker.CapturePhotoAsync);
    [RelayCommand] private Task ChoosePhoto() => AttachAsync(photoPicker.PickPhotoAsync);

    [RelayCommand]
    private void RemovePhoto(DraftPhotoViewModel photo)
    {
        Photos.Remove(photo);
        if (File.Exists(photo.LocalPath)) File.Delete(photo.LocalPath);
        // A reader who deleted a photograph must not have it land in the archive anyway.
        if (photo.QueuedLocalId is { } localId) mediaUploader.Cancel(localId);
        SyncRowsToDraft();
    }

    private async Task AttachAsync(Func<CancellationToken, Task<PickedFile?>> pick)
    {
        var picked = await pick(default);
        if (picked is null) return;   // the reader backed out; nothing to keep

        // A copy on this device before anything touches the network: if the upload
        // never comes back, the picture the reader just took must still be here.
        var localPath = Path.Combine(Path.GetTempPath(), $"tz-photo-{Guid.NewGuid():N}{MediaLimits.ExtensionFor(picked.ContentType)}");
        await using (var source = await picked.Open(default))
        await using (var copy = File.Create(localPath))
            await source.CopyToAsync(copy);

        var photo = new DraftPhotoViewModel { LocalPath = localPath };
        Photos.Add(photo);

        var reopened = new PickedFile(picked.FileName, picked.ContentType, _ => Task.FromResult<Stream>(File.OpenRead(localPath)));
        var beforeCount = mediaUploader.Pending.Count;
        photo.MediaId = await mediaUploader.UploadAsync(reopened, MediaKind.Photo, default);
        // Nothing but "a new entry appeared" tells us this photo is the one that got queued;
        // UploadAsync's signature is pinned by MediaUploaderTests and can't hand the id back.
        if (photo.MediaId is null && mediaUploader.Pending.Count > beforeCount)
            photo.QueuedLocalId = mediaUploader.Pending[^1].LocalId;
        SyncRowsToDraft();
    }

    /// <summary>Uploaded photos keep a display copy only for as long as this instance shows them;
    /// it is never in the persisted draft, so once the rows move on it is pure litter.</summary>
    private static void PurgeUploadedDisplayCopies(IEnumerable<DraftPhotoViewModel> photos)
    {
        foreach (var p in photos)
            if (p.MediaId is not null && p.LocalPath.Length > 0 && File.Exists(p.LocalPath))
                File.Delete(p.LocalPath);
    }

    /// <summary>What this step still needs, or null when it is ready. Steps 3 and 4 have no required fields.</summary>
    public string? Validate()
    {
        SyncRowsToDraft();
        return Step switch
        {
            1 when Draft.LocalName.Trim().Length == 0 => "Give the dish its local name.",
            1 when Draft.EnglishDescription.Trim().Length == 0 => "Add a short description in English.",
            1 when Draft.Province.Length == 0 => "Choose the province it comes from.",
            2 when Draft.Ingredients.Count == 0 => "List at least one ingredient.",
            2 when Draft.Steps.Count == 0 => "Write at least one cooking step.",
            _ => null,
        };
    }

    [RelayCommand]
    private async Task Next()
    {
        if (Validate() is { } problem)
        {
            ValidationMessage = problem;
            return;
        }
        ValidationMessage = "";

        // Every step forward lands the draft locally first, so nothing typed is ever lost.
        DraftId = contributions.SaveDraft(Draft, DraftId).Id;
        RefreshDraftsLabel();

        if (Step < 4)
        {
            Step++;
            return;
        }

        try
        {
            var contribution = await contributions.SubmitAsync(Draft);
            SubmittedId = contribution.Id;
            SubmittedName = contribution.Name;
            SubmittedMeta = contribution.Meta;
            SubmitError = "";
            IsSubmitted = true;
            RefreshDraftsLabel();
            // The recipe is away; nothing here needs a local display copy of an uploaded
            // photo any more (the still-pending ones keep theirs until they too go).
            PurgeUploadedDisplayCopies(Photos);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            SubmitError = "Could not reach the archive. Your draft is saved; try again when you are online.";
        }
    }

    private void RefreshDraftsLabel()
    {
        var n = contributions.Drafts.Count;
        DraftsLabel = n == 0 ? "" : n == 1 ? "1 draft" : $"{n} drafts";
    }

    [RelayCommand]
    private Task ViewStatus()
        => SubmittedId is { } id
            ? Navigation.GoToAsync("shareReview", new Dictionary<string, object> { ["id"] = id })
            : Task.CompletedTask;

    [RelayCommand] private Task OpenDrafts() => Navigation.GoToAsync("shareDraft");

    [RelayCommand]
    private void Back()
    {
        SyncRowsToDraft();
        ValidationMessage = "";
        Step = Math.Max(1, Step - 1);
    }

    [RelayCommand]
    private void Reset()
    {
        IsSubmitted = false;
        DraftId = null;
        SubmittedId = null;
        Draft = contributions.StartShareDraft();
        LoadRowsFromDraft();
        OnPropertyChanged(nameof(Draft));
        OnPropertyChanged(nameof(Province));
        OnPropertyChanged(nameof(MealType));
        OnPropertyChanged(nameof(CreditTeacher));
        Step = 1;
    }

    [RelayCommand]
    private Task Close()
    {
        // Leaving mid-way keeps what was typed, as long as something was.
        SyncRowsToDraft();
        if (Draft.LocalName.Trim().Length > 0)
            DraftId = contributions.SaveDraft(Draft, DraftId).Id;
        PurgeUploadedDisplayCopies(Photos);
        return Navigation.GoBackAsync();
    }
}
