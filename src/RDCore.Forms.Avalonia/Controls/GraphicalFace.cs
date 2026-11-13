using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

// The face of a graphical CommandButton, CheckBox or OptionButton, as VB6 draws it: the caption at the bottom, the picture centred
// above it (centred in the whole face where there is no caption), and both one pixel right and down while the button is down. A button
// that is down shows its DownPicture where it has one, and a disabled button its DisabledPicture, or else its Picture greyed.
internal sealed class GraphicalFace : Control
{
    // How much of its colour a disabled button's caption keeps, and its Picture where it has no DisabledPicture.
    private const double GreyedOpacity = 0.5;

    private readonly VBPicture.Holder picture = new();
    private readonly VBPicture.Holder downPicture = new();
    private readonly VBPicture.Holder disabledPicture = new();
    private Control? caption;
    private bool isDown;

    public GraphicalFace() => ClipToBounds = true;

    public bool IsDown
    {
        get => isDown;
        set
        {
            if (isDown == value)
                return;
            isDown = value;
            InvalidateArrange();
            InvalidateVisual();
        }
    }

    // The picture drawn now, and whether it is drawn greyed.
    internal (Bitmap? Picture, bool Greyed) Shown => !IsEffectivelyEnabled
        ? disabledPicture.Picture is { } disabled ? (disabled, false) : (picture.Picture, true)
        : (isDown ? downPicture.Picture ?? picture.Picture : picture.Picture, false);

    public void ShowCaption(string text)
    {
        if (caption is not null)
        {
            LogicalChildren.Remove(caption);
            VisualChildren.Remove(caption);
        }
        caption = VBCaption.AsShown(text).Length == 0 ? null : VBCaption.Present(text);
        if (caption is not null)
        {
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            caption.Opacity = CaptionOpacity;
            LogicalChildren.Add(caption);
            VisualChildren.Add(caption);
        }
        InvalidateMeasure();
    }

    // Takes the value of one of the pictures it draws.
    public void Take(PropertyDefinition property, byte[]? bytes)
    {
        var holder = ReferenceEquals(property, P.DownPicture) ? downPicture
            : ReferenceEquals(property, P.DisabledPicture) ? disabledPicture
            : picture;
        if (holder.Take(bytes))
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsEffectivelyEnabledProperty)
            return;
        if (caption is not null)
            caption.Opacity = CaptionOpacity;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        caption?.Measure(availableSize);
        var text = caption?.DesiredSize ?? default;
        var pixels = Shown.Picture?.PixelSize ?? default;
        return new Size(Math.Max(text.Width, pixels.Width), text.Height + pixels.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (caption is not null)
        {
            var height = caption.DesiredSize.Height;
            caption.Arrange(new Rect(Offset, Math.Max(0, finalSize.Height - height) + Offset, finalSize.Width, height));
        }
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var (shown, greyed) = Shown;
        if (shown is null)
            return;
        var pixels = shown.PixelSize;
        var above = caption is null ? Bounds.Height : Math.Max(0, Bounds.Height - caption.DesiredSize.Height);
        var x = Math.Floor((Bounds.Width - pixels.Width) / 2) + Offset;
        var y = Math.Floor((above - pixels.Height) / 2) + Offset;
        using (context.PushTransform(Matrix.CreateTranslation(x, y)))
        {
            if (!greyed)
                VBPicture.Draw(context, shown, new Size(pixels.Width, pixels.Height));
            else
                using (context.PushOpacity(GreyedOpacity))
                    VBPicture.Draw(context, shown, new Size(pixels.Width, pixels.Height));
        }
    }

    // Whether a property is one of the pictures a graphical button draws, which a Standard one ignores.
    public static bool IsDrawnByFace(PropertyDefinition property) =>
        ReferenceEquals(property, P.Picture) || ReferenceEquals(property, P.DownPicture) || ReferenceEquals(property, P.DisabledPicture);

    // Shows a button's caption: on its face where it is graphical, or as its content.
    public static void ShowCaption(ContentControl button, GraphicalFace? face, string caption)
    {
        if (face is null)
        {
            VBCaption.ShowIn(button, caption);
            return;
        }
        face.ShowCaption(caption);
        VBCaption.NameFor(button, caption);
    }

    // Shows the face in a button: it takes the whole of the button inside its border. A disabled graphical button keeps its BackColor
    // and its DisabledPicture at full colour, so the button is kept from the theme's disabled fade and the face greys what VB6 greys.
    public static void ShowIn(ContentControl button, GraphicalFace face)
    {
        button.Opacity = 1;
        button.Padding = default;
        button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        button.VerticalContentAlignment = VerticalAlignment.Stretch;
        button.Content = face;
    }

    private double Offset => isDown ? 1 : 0;

    private double CaptionOpacity => IsEffectivelyEnabled ? 1 : GreyedOpacity;
}
