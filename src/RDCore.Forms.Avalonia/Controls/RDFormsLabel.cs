// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RDCore.Forms.Model;
using RDCore.SDK.Workspace.Pictures;
using P =RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Label: a control that shows a <see cref="ComponentClasses.Label"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its caption's <c>&amp;</c> marks an access key where <c>UseMnemonic</c> is set; otherwise every <c>&amp;</c> is drawn. A
/// <c>BackStyle</c> of Transparent paints no background. A single border is a sunken edge when 3-D, and one pixel when flat.
/// </para>
/// <para>
/// Its <c>Picture</c>, an RD-VBA addition, is laid out against its caption as MSForms' Label lays it out: the two make one block,
/// centred in the label, the picture where its <c>PicturePosition</c> puts it, one pixel from the caption. A picture larger than the
/// room it has is shrunk on that axis. The colour of a bitmap's lower-left pixel is transparent; a GIF's transparent colour is too.
/// </para>
/// <para>
/// It takes no focus. It raises Click, DblClick, MouseDown, MouseMove and MouseUp, MouseUp before Click; Click only where the release
/// is over the label again.
/// </para>
/// </remarks>
public sealed class RDFormsLabel : Control
{
    private readonly ComponentInstance instance;
    private readonly RDFormsEdge edge = new();
    private readonly LabelFace face = new();
    private readonly RDFormsPicture.Holder picture = new();
    private IBrush background = Brushes.Transparent;

    static RDFormsLabel()
    {
        ControlEvents.AttachMouse<RDFormsLabel>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<RDFormsLabel>(ClickRelease.OverTheControl);
    }

    /// <summary>
    /// Creates a control that shows a Label.
    /// </summary>
    /// <param name="instance">The Label.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Label.</exception>
    public RDFormsLabel(ComponentInstance instance)
    {
        this.instance = instance;
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        edge.Child = face;
        ComponentBinding.Bind(this, instance, ComponentClasses.Label, Apply);
    }

    /// <summary>
    /// The text the label draws, its access key marker taken out where it has one.
    /// </summary>
    public string? DrawnText => face.Text?.Text;

    /// <summary>
    /// Where the label draws its picture, inside its border, or <see langword="null"/> where it draws none.
    /// </summary>
    public Rect? PictureBounds => face.PictureBounds;

    /// <summary>
    /// <see cref="FormError.InvalidPicture"/> where its component's <c>Picture</c> is refused and not drawn; otherwise
    /// <see langword="null"/>.
    /// </summary>
    public FormError? PictureError => picture.Error;

    /// <inheritdoc/>
    public override void Render(DrawingContext context) => context.FillRectangle(background, new Rect(Bounds.Size));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Caption) or nameof(P.UseMnemonic) or nameof(P.Alignment) or nameof(P.WordWrap) or nameof(P.AutoSize):
                ShowCaption();
                break;
            case nameof(P.BorderStyle) or nameof(P.Appearance):
                edge.Edge = RDFormsEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance));
                break;
            case nameof(P.BackColor) or nameof(P.BackStyle):
                background = instance.Get(P.BackStyle) == BackStyle.Opaque ? FormPalette.ToBrush(instance.Get(P.BackColor)) : Brushes.Transparent;
                InvalidateVisual();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, FormPalette.ToBrush(instance.Get(P.ForeColor)));
                break;
            case nameof(P.Picture):
                picture.Take((byte[]?)e.After);
                ShowPicture();
                break;
            case nameof(P.PicturePosition):
                ShowPicture();
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    private static bool Wraps(ComponentInstance label) => !label.Get(P.AutoSize) || label.Get(P.WordWrap);

    // An AccessText only where UseMnemonic is set and the caption has a key one can carry; plain text otherwise, so that an
    // underscore in the caption does not become a key VB never gave it.
    private void ShowCaption()
    {
        var caption = instance.Get(P.Caption);
        var parsed = instance.Get(P.UseMnemonic) ? RDFormsCaption.Parse(caption) : (RDFormsCaption.Parsed?)null;
        var text = parsed is { FitsAccessText: true } ? new AccessText { Text = parsed.Value.ForAccessText }
            : new TextBlock { Text = parsed?.Shown ?? caption };
        // A label of a fixed size wraps its caption at its width; WordWrap decides only whether one that sizes itself grows down or across.
        text.TextWrapping = Wraps(instance) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        text.TextAlignment = RDFormsTextBox.ToAvalonia(instance.Get(P.Alignment));
        face.Text = text;
        if ((parsed?.Shown ?? caption) is { Length: > 0 } shown)
            SetValue(global::Avalonia.Automation.AutomationProperties.NameProperty, shown);
        else
            ClearValue(global::Avalonia.Automation.AutomationProperties.NameProperty);
    }

    // MSForms takes the colour of a bitmap's lower-left pixel as transparent; a GIF keeps its own transparent colour.
    private void ShowPicture()
    {
        var mask = picture.Format == PictureFormat.Bitmap ? picture.LowerLeft : null;
        face.Show(picture.MaskedBy(mask), instance.Get(P.PicturePosition));
    }
}

// The inside of a Label: its caption, and its picture laid out against it as MSForms' Label lays them out (measured in Excel). The
// two make one block, centred, or from the edge where it is larger than the label.
internal sealed class LabelFace : Control
{
    // The pixels between the picture and the caption.
    private const double Gap = 1;
    private TextBlock? text;
    private Bitmap? picture;
    private PicturePosition position = PicturePosition.AboveCenter;

    public LabelFace() => ClipToBounds = true;

    public TextBlock? Text
    {
        get => text;
        set
        {
            if (text is not null)
            {
                VisualChildren.Remove(text);
                LogicalChildren.Remove(text);
            }
            text = value;
            if (value is not null)
            {
                VisualChildren.Add(value);
                LogicalChildren.Add(value);
            }
            InvalidateMeasure();
        }
    }

    public Rect? PictureBounds { get; private set; }

    public void Show(Bitmap? picture, PicturePosition position)
    {
        this.picture = picture;
        this.position = position;
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        text?.Measure(availableSize);
        return text?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        PictureBounds = null;
        if (picture is null)
        {
            text?.Arrange(new Rect(finalSize));
            return finalSize;
        }
        var side = position <= PicturePosition.RightBottom;
        var (width, height) = (finalSize.Width, finalSize.Height);
        var pixels = picture.PixelSize;
        // A caption that wraps does so in the width the picture leaves beside it; one that does not is measured whole. The picture is
        // then shrunk to the room the caption leaves.
        var room = side ? Math.Max(0, width - pixels.Width - Gap) : width;
        text?.Measure(new Size(text.TextWrapping == TextWrapping.NoWrap ? double.PositiveInfinity : room, double.PositiveInfinity));
        var (tw, th) = text is null ? (0d, 0d) : (text.DesiredSize.Width, text.DesiredSize.Height);
        var pw = Math.Min(pixels.Width, Math.Max(0, side ? width - tw - Gap : width));
        var ph = Math.Min(pixels.Height, height);
        var (bw, bh) = position switch
        {
            <= PicturePosition.RightBottom => (pw + Gap + tw, Math.Max(ph, th)),
            <= PicturePosition.BelowRight => (Math.Max(pw, tw), ph + Gap + th),
            _ => (Math.Max(pw, tw), Math.Max(ph, th)),
        };
        var (x0, y0) = (Math.Floor(Math.Max(0, (width - bw) / 2)), Math.Floor(Math.Max(0, (height - bh) / 2)));
        // The second word of the position: top, centre or bottom beside the caption; left, centre or right above or below it.
        var along = (int)position % 3;
        Rect pictureAt, textAt;
        switch (position)
        {
            case <= PicturePosition.LeftBottom:
                pictureAt = new Rect(x0, Aligned(y0, bh, ph, along), pw, ph);
                textAt = new Rect(x0 + pw + Gap, Aligned(y0, bh, th, along), tw, th);
                break;
            case <= PicturePosition.RightBottom:
                textAt = new Rect(x0, Aligned(y0, bh, th, along), tw, th);
                pictureAt = new Rect(x0 + tw + Gap, Aligned(y0, bh, ph, along), pw, ph);
                break;
            case <= PicturePosition.AboveRight:
                pictureAt = new Rect(Aligned(x0, bw, pw, along), y0, pw, ph);
                textAt = new Rect(Aligned(x0, bw, tw, along), y0 + ph + Gap, tw, th);
                break;
            case <= PicturePosition.BelowRight:
                textAt = new Rect(Aligned(x0, bw, tw, along), y0, tw, th);
                pictureAt = new Rect(Aligned(x0, bw, pw, along), y0 + th + Gap, pw, ph);
                break;
            default:
                pictureAt = new Rect(Aligned(x0, bw, pw, 1), Aligned(y0, bh, ph, 1), pw, ph);
                textAt = new Rect(Aligned(x0, bw, tw, 1), Aligned(y0, bh, th, 1), tw, th);
                break;
        }
        text?.Arrange(textAt);
        PictureBounds = pictureAt;
        return finalSize;

        static double Aligned(double start, double extent, double length, int along) =>
            start + along switch { 0 => 0, 1 => Math.Floor((extent - length) / 2), _ => extent - length };
    }

    public override void Render(DrawingContext context)
    {
        if (picture is not null && PictureBounds is { Width: > 0, Height: > 0 } at)
            context.DrawImage(picture, new Rect(picture.Size), at);
    }
}
