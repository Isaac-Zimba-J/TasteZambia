using System.Windows.Input;
using FluentIcons.Common;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Controls;

/// <summary>
/// What a screen shows while it has nothing to show: a spinner on first load, or the
/// reason it failed with a way to try again. Visible only when one of those is true.
///
/// The failure is drawn from <see cref="LoadFailure"/> rather than from the message text,
/// so each kind gets its own emblem and heading. A reader who has been asked to slow down
/// should not be told to check a connection that is working.
/// </summary>
public partial class LoadState : ContentView
{
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(LoadState), false, propertyChanged: OnStateChanged);

    public static readonly BindableProperty ErrorMessageProperty =
        BindableProperty.Create(nameof(ErrorMessage), typeof(string), typeof(LoadState), "", propertyChanged: OnStateChanged);

    public static readonly BindableProperty FailureProperty =
        BindableProperty.Create(nameof(Failure), typeof(LoadFailure), typeof(LoadState), LoadFailure.None, propertyChanged: OnStateChanged);

    public static readonly BindableProperty LoadingTextProperty =
        BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(LoadState), "Reading the archive…");

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(LoadState), null);

    public LoadState() => InitializeComponent();

    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string ErrorMessage { get => (string)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }
    public LoadFailure Failure { get => (LoadFailure)GetValue(FailureProperty); set => SetValue(FailureProperty, value); }
    public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    public bool HasError => ErrorMessage.Length > 0;

    /// <summary>
    /// A heading the reader can take in at a glance, before the sentence explaining it.
    /// Falls back to the offline wording, because a failure with no kind set is almost
    /// always the network - and guessing wrong here is cheap.
    /// </summary>
    public string ErrorTitle => Failure switch
    {
        LoadFailure.TooManyRequests => "Just a moment",
        LoadFailure.Unexpected => "Something went wrong",
        _ => "You are offline",
    };

    /// <summary>Typed, not a string: FluentIcon's Icon is an enum, and a bound string is not converted.</summary>
    public Icon EmblemIcon => Failure switch
    {
        LoadFailure.TooManyRequests => Icon.Timer,
        LoadFailure.Unexpected => Icon.Warning,
        _ => Icon.CloudOff,
    };

    /// <summary>
    /// What is still theirs while the archive is unreachable. Said only when it is true:
    /// saved recipes and drafts live on the phone, so being offline does not lose them.
    /// </summary>
    public string Consolation => Failure switch
    {
        LoadFailure.Offline => "Anything you have saved or started writing is on this phone. None of it is lost.",
        _ => "",
    };

    public bool HasConsolation => Consolation.Length > 0;

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (LoadState)bindable;
        view.OnPropertyChanged(nameof(HasError));
        view.OnPropertyChanged(nameof(ErrorTitle));
        view.OnPropertyChanged(nameof(EmblemIcon));
        view.OnPropertyChanged(nameof(Consolation));
        view.OnPropertyChanged(nameof(HasConsolation));

        // The control occupies no space unless it has something to say.
        view.IsVisible = view.IsLoading || view.HasError;
    }
}
