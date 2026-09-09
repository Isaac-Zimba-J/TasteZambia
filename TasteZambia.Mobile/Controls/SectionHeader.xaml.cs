using System.Windows.Input;

namespace TasteZambia.Mobile.Controls;

public partial class SectionHeader : ContentView
{
    public static readonly BindableProperty KickerProperty =
        BindableProperty.Create(nameof(Kicker), typeof(string), typeof(SectionHeader), null,
            propertyChanged: (b, _, _) => ((SectionHeader)b).OnPropertyChanged(nameof(HasKicker)));

    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(SectionHeader), "");

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(SectionHeader), null,
            propertyChanged: (b, _, _) => ((SectionHeader)b).OnPropertyChanged(nameof(HasAction)));

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(SectionHeader), null);

    public string? Kicker { get => (string?)GetValue(KickerProperty); set => SetValue(KickerProperty, value); }
    public string TitleText { get => (string)GetValue(TitleTextProperty); set => SetValue(TitleTextProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

    public bool HasKicker => !string.IsNullOrEmpty(Kicker);
    public bool HasAction => !string.IsNullOrEmpty(ActionText);

    public SectionHeader() => InitializeComponent();
}
