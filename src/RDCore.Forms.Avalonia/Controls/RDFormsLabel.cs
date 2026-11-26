// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RDCore.Forms.Model;
using RDCore.SDK.Workspace.Pictures;
using P =RDCore.Forms.Model.Properties;
using ModelTextAlignment = RDCore.Forms.Model.TextAlignment;

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
/// Where <c>AutoSize</c> is set, it sets its component's size to its caption's, as VB6 does: as a program sets another caption, sets
/// <c>AutoSize</c>, or changes its <c>Font</c>, <c>BorderStyle</c> or <c>WordWrap</c>, and, where <c>WordWrap</c> is set, sets its
/// <c>Width</c>. It does not as its form loads. With <c>WordWrap</c> it keeps its width and grows down, and shows the lines it counted.
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
    private Wrapped? wrapped;
    private bool fitting;

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

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RDFormsControlAutomationPeer(this, AutomationControlType.Text);

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
        // A value a program set, as against one applied again; and one it changed.
        var set = !ComponentBinding.IsReapplying(instance);
        var changed = set && !Equals(e.Before, e.After);
        switch (e.Property.Name)
        {
            case nameof(P.Caption) or nameof(P.WordWrap):
                if (changed)
                    Fit(widthSet: false);
                ShowCaption();
                break;
            case nameof(P.AutoSize):
                if (set)
                    Fit(widthSet: false);
                ShowCaption();
                break;
            case nameof(P.UseMnemonic) or nameof(P.Alignment):
                ShowCaption();
                break;
            case nameof(P.BorderStyle) or nameof(P.Appearance):
                edge.Edge = RDFormsEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance));
                if (changed && e.Property.Name == nameof(P.BorderStyle) && Fit(widthSet: false))
                    ShowCaption();
                break;
            case nameof(P.Font):
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                if (changed && Fit(widthSet: false))
                    ShowCaption();
                break;
            case nameof(P.Width):
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                if (set && instance.Get(P.WordWrap) && Fit(widthSet: true))
                    ShowCaption();
                break;
            case nameof(P.BackColor) or nameof(P.BackStyle):
                background = instance.Get(P.BackStyle) == BackStyle.Opaque ? ComponentBinding.Brush(this, instance.Get(P.BackColor)) : Brushes.Transparent;
                InvalidateVisual();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, ComponentBinding.Brush(this, instance.Get(P.ForeColor)));
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
        var shown = parsed?.Shown ?? caption;
        // A label that fitted itself to its caption wrapped shows the lines it counted, so that its text takes the height it has.
        var lines = instance.Get(P.AutoSize) && instance.Get(P.WordWrap) && wrapped is { } fit && fit.Shown == shown ? wrapped : null;
        if (lines is not null)
            parsed = parsed is { } key ? new RDFormsCaption.Parsed(lines.Text, lines.At(key.AccessKey)) : null;
        var text = parsed is { FitsAccessText: true } ? new AccessText { Text = parsed.Value.ForAccessText }
            : new TextBlock { Text = lines?.Text ?? shown };
        // A label of a fixed size wraps its caption at its width; WordWrap decides only whether one that sizes itself grows down or across.
        text.TextWrapping = Wraps(instance) && lines is null ? TextWrapping.Wrap : TextWrapping.NoWrap;
        text.TextAlignment = RDFormsTextBox.ToAvalonia(instance.Get(P.Alignment));
        face.Text = text;
        if (shown.Length > 0)
            SetValue(global::Avalonia.Automation.AutomationProperties.NameProperty, shown);
        else
            ClearValue(global::Avalonia.Automation.AutomationProperties.NameProperty);
    }

    // VB6 fits a Label whose AutoSize is set to its caption as shown, in its font, inside its border, and gives it whole pixels: across
    // to its longest line, or, where WordWrap is set, down to its lines wrapped at its width, across only to a line wider than that. A
    // fit from a Width set keeps that width where the lines fit it. Its alignment's edge stays: the left one, the right one or the centre.
    // It answers whether the label was fitted.
    private bool Fit(bool widthSet)
    {
        if (fitting || !instance.Get(P.AutoSize))
            return false;
        var font = PrintFont.Of(instance.Get(P.Font));
        var caption = instance.Get(P.Caption);
        var shown = instance.Get(P.UseMnemonic) ? RDFormsCaption.Parse(caption).Shown : caption;
        var border = 2 * RDFormsEdge.WidthOf(RDFormsEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance)));
        var (left, top, width) = (instance.Get(P.Left), instance.Get(P.Top), instance.Get(P.Width));
        var wraps = instance.Get(P.WordWrap);
        var across = widthSet ? width : Whole(width);
        var lines = Wrapped.Of(font, shown, wraps ? across - border : double.PositiveInfinity);
        wrapped = wraps ? lines : null;
        // An empty caption is a space wide.
        var widest = shown.Length == 0 ? font.Width(" ") : lines.Widths.Max();
        var fitted = !wraps || widest > across - border ? widest + border : across;
        var at = instance.Get(P.Alignment) switch
        {
            ModelTextAlignment.RightJustify => Whole(left + width) - fitted,
            ModelTextAlignment.Center => Whole(left + (width - fitted) / 2),
            _ => Whole(left),
        };
        fitting = true;
        try
        {
            instance.Set(P.Width, fitted).Set(P.Height, lines.Widths.Length * font.Line + border).Set(P.Left, at).Set(P.Top, Whole(top));
        }
        finally
        {
            fitting = false;
        }
        return true;

        static double Whole(double pixels) => Math.Round(pixels, MidpointRounding.AwayFromZero);
    }

    // MSForms takes the colour of a bitmap's lower-left pixel as transparent; a GIF keeps its own transparent colour.
    private void ShowPicture()
    {
        var mask = picture.Format == PictureFormat.Bitmap ? picture.LowerLeft : null;
        face.Show(picture.MaskedBy(mask), instance.Get(P.PicturePosition));
    }

    // A caption in the lines a Label fitted to it counts, each one's width in pixels, and the text it shows them as.
    private sealed class Wrapped
    {
        private readonly List<int> breaks;

        private Wrapped(string shown, List<int> breaks, int[] widths)
        {
            Shown = shown;
            this.breaks = breaks;
            Widths = widths;
            var text = new System.Text.StringBuilder(shown);
            for (var i = breaks.Count - 1; i >= 0; i--)
                text.Insert(breaks[i], '\n');
            Text = text.ToString();
        }

        public string Shown { get; }

        public int[] Widths { get; }

        // The caption with a line feed where each wrapped line starts.
        public string Text { get; }

        // Where a character of the caption is in the text.
        public int? At(int? index) => index is { } at ? at + breaks.Count(start => start <= at) : null;

        // The lines of a caption, wrapped as VB6 wraps a Label's (DrawText's word break, measured): a word stays on a line where the
        // line through it, without the spaces after it, fits the room. A line's width counts the spaces after its last word, and a word
        // wider than the room is alone on its line. A carriage return, a line feed, or both together end a line too.
        public static Wrapped Of(PrintFont font, string shown, double room)
        {
            var breaks = new List<int>();
            var widths = new List<int>();
            for (var start = 0; ; )
            {
                var end = shown.IndexOfAny(['\r', '\n'], start);
                if (end < 0)
                    end = shown.Length;
                // The line's start, and where its next word starts, past the spaces after its last one.
                var (line, next) = (start, start);
                while (next < end)
                {
                    var word = next;
                    while (word < end && shown[word] == ' ')
                        word++;
                    while (word < end && shown[word] != ' ')
                        word++;
                    if (next > line && font.Width(shown[line..word]) > room)
                    {
                        widths.Add(font.Width(shown[line..next]));
                        breaks.Add(next);
                        line = next;
                        continue;
                    }
                    next = word;
                    while (next < end && shown[next] == ' ')
                        next++;
                }
                widths.Add(font.Width(shown[line..end]));
                if (end == shown.Length)
                    return new Wrapped(shown, breaks, [.. widths]);
                start = end + (shown[end] == '\r' && end + 1 < shown.Length && shown[end + 1] == '\n' ? 2 : 1);
            }
        }
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
