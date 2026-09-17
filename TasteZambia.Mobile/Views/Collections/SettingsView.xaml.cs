using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Collections;

public partial class SettingsView : LoadOnceView
{
    public SettingsView(SettingsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
