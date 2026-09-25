using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

/// <summary>
/// The reader's own name, location and languages. Everything they contribute is credited
/// with what is typed here, which is why it is worth asking for at all.
/// </summary>
public sealed partial class ProfileEditViewModel(
    IProfileRepository profiles,
    INavigationService navigation) : BaseViewModel(navigation)
{
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))] private string _name = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private string _languages = "";
    [ObservableProperty] private string _errorMessage = "";

    public bool CanSave => Name.Trim().Length > 0;

    public override async Task InitializeAsync()
    {
        var profile = await profiles.GetAsync();

        // The placeholder name is not something the reader chose, so do not put it in the box.
        Name = profile.Name == Data.Http.HttpProfileRepository.DefaultName ? "" : profile.Name;
        Location = profile.Location;
        Languages = profile.Languages;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        try
        {
            await profiles.UpdateAsync(Name, Location, Languages);
            ErrorMessage = "";
            await Navigation.GoBackAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = "Could not reach the archive. Try again when you are online.";
        }
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
