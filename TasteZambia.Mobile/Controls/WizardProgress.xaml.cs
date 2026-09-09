namespace TasteZambia.Mobile.Controls;

public partial class WizardProgress : ContentView
{
    public static readonly BindableProperty StepTitleProperty =
        BindableProperty.Create(nameof(StepTitle), typeof(string), typeof(WizardProgress), "");

    public static readonly BindableProperty StepNumberProperty =
        BindableProperty.Create(nameof(StepNumber), typeof(int), typeof(WizardProgress), 1,
            propertyChanged: (b, _, _) => ((WizardProgress)b).Refresh());

    public static readonly BindableProperty StepCountProperty =
        BindableProperty.Create(nameof(StepCount), typeof(int), typeof(WizardProgress), 4,
            propertyChanged: (b, _, _) => ((WizardProgress)b).Refresh());

    public string StepTitle { get => (string)GetValue(StepTitleProperty); set => SetValue(StepTitleProperty, value); }
    public int StepNumber { get => (int)GetValue(StepNumberProperty); set => SetValue(StepNumberProperty, value); }
    public int StepCount { get => (int)GetValue(StepCountProperty); set => SetValue(StepCountProperty, value); }

    public string StepLabel => $"Step {StepNumber} of {StepCount}";

    public WizardProgress()
    {
        InitializeComponent();
        Refresh();
    }

    /// <summary>
    /// The fill is sized by grid star weights rather than a pixel width, so it stays
    /// correct at any screen width without the control needing to measure itself.
    /// </summary>
    private void Refresh()
    {
        OnPropertyChanged(nameof(StepLabel));

        if (Fill?.Parent is not Grid grid || grid.ColumnDefinitions.Count < 2)
            return;

        var done = Math.Clamp(StepNumber, 0, Math.Max(1, StepCount));
        var remaining = Math.Max(0, StepCount - done);

        grid.ColumnDefinitions[0].Width = new GridLength(done, GridUnitType.Star);
        grid.ColumnDefinitions[1].Width = new GridLength(remaining, GridUnitType.Star);
        Fill.IsVisible = done > 0;
    }
}
