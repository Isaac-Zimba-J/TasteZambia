using System.Windows.Input;

namespace TasteZambia.Mobile.Controls;

/// <summary>
/// What a screen shows while it has nothing to show: a spinner on first load, or the
/// reason it failed with a way to try again. Visible only when one of those is true.
/// </summary>
public partial class LoadState : ContentView
{
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(LoadState), false, propertyChanged: OnStateChanged);

    public static readonly BindableProperty ErrorMessageProperty =
        BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(LoadState), "", propertyChanged: OnStateChanged);

    public static readonly BindableProperty LoadingTextProperty =
        BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(LoadState), "Reading the archive…");

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(LoadState), null);

    public LoadState() => InitializeComponent();

    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string ErrorMessage { get => (string)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }
    public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    public bool HasError => ErrorMessage.Length > 0;

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (LoadState)bindable;
        view.OnPropertyChanged(nameof(HasError));

        // The control occupies no space unless it has something to say.
        view.IsVisible = view.IsLoading || view.HasError;
    }
}
