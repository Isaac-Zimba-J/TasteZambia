using Microsoft.Maui.Controls.Shapes;

namespace TasteZambia.Mobile.Controls;

/// <summary>
/// Every image in the app goes through this control, so the archive's rule that a
/// missing photo shows stripes and names the shot needed cannot be forgotten.
/// </summary>
public partial class PhotoOrPlaceholder : ContentView
{
    public static readonly BindableProperty ImageAssetProperty =
        BindableProperty.Create(nameof(ImageAsset), typeof(string), typeof(PhotoOrPlaceholder), null,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(PhotoOrPlaceholder), "photo needed",
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(PhotoOrPlaceholder), 16.0,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public static readonly BindableProperty ShowCaptionProperty =
        BindableProperty.Create(nameof(ShowCaption), typeof(bool), typeof(PhotoOrPlaceholder), true,
            propertyChanged: (b, _, _) => ((PhotoOrPlaceholder)b).Apply());

    public string? ImageAsset { get => (string?)GetValue(ImageAssetProperty); set => SetValue(ImageAssetProperty, value); }
    public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool ShowCaption { get => (bool)GetValue(ShowCaptionProperty); set => SetValue(ShowCaptionProperty, value); }

    public PhotoOrPlaceholder()
    {
        InitializeComponent();
        Apply();
    }

    private void Apply()
    {
        Clip.StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };

        var hasPhoto = !string.IsNullOrEmpty(ImageAsset);
        Photo.IsVisible = hasPhoto;
        Placeholder.IsVisible = !hasPhoto;

        if (hasPhoto)
            Photo.Source = ImageSource.FromFile(ImageAsset);

        CaptionLabel.Text = Caption;
        CaptionLabel.IsVisible = ShowCaption && !string.IsNullOrEmpty(Caption);

        SemanticProperties.SetDescription(this, hasPhoto ? "" : Caption);
    }
}
