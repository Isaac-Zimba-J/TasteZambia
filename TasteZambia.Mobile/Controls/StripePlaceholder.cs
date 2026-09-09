namespace TasteZambia.Mobile.Controls;

/// <summary>45-degree two-tone stripe fill standing in for photography we do not have yet.</summary>
public sealed class StripePlaceholder : GraphicsView
{
    public static readonly BindableProperty StripeWidthProperty =
        BindableProperty.Create(nameof(StripeWidth), typeof(double), typeof(StripePlaceholder), 6.0,
            propertyChanged: (b, _, _) => ((StripePlaceholder)b).Invalidate());

    public double StripeWidth
    {
        get => (double)GetValue(StripeWidthProperty);
        set => SetValue(StripeWidthProperty, value);
    }

    public StripePlaceholder()
    {
        Drawable = new StripeDrawable(this);
    }

    private sealed class StripeDrawable(StripePlaceholder owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            var a = Color.FromArgb("#DED4C2");
            var b = Color.FromArgb("#E9E1D3");
            var w = (float)owner.StripeWidth;

            canvas.FillColor = b;
            canvas.FillRectangle(rect);

            canvas.SaveState();
            canvas.ClipRectangle(rect);
            canvas.StrokeColor = a;
            canvas.StrokeSize = w;

            // 135deg stripes: diagonals from bottom-left to top-right, stepping by
            // twice the stripe width so the two tones alternate evenly.
            var span = rect.Width + rect.Height;
            for (var x = -rect.Height; x < span; x += w * 2)
                canvas.DrawLine(rect.X + x, rect.Bottom, rect.X + x + rect.Height, rect.Y);

            canvas.RestoreState();
        }
    }
}
