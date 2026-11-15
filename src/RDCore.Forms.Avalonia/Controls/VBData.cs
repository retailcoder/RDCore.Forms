using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Data control: a control that draws a <see cref="VBClasses.Data"/> component's four buttons and its caption, as VB6 draws
/// them.
/// </summary>
/// <remarks>
/// <para>
/// It opens no database: its <see cref="Database"/> and <see cref="Recordset"/> are Nothing, its buttons move through no records,
/// and <see cref="UpdateRecord"/> and <see cref="UpdateControls"/> are error 91.
/// </para>
/// <para>
/// The look, as measured with VB6 at 96 dpi:
/// </para>
/// <list type="bullet">
/// <item>3-D: a ring of one pixel in the button face's colour, then a sunken edge two pixels deep; the parts are laid out three pixels
/// in. Flat: the parts are laid out one pixel in, under a border of one pixel in the window frame's colour.</item>
/// <item>The buttons are 19 pixels wide and as tall as the parts' box: the first two at its left, the last two at its right, with a
/// column of one pixel before the third, in the button face's colour (3-D) or the frame's (flat). The caption's strip fills what is
/// left between the second button and that column. Where the control is too narrow for them all, the last two buttons and the column
/// are drawn over the first two.</item>
/// <item>The caption is in <c>ForeColor</c>, or the grey text colour where the control is disabled, on <c>BackColor</c>: three
/// pixels in from the strip's left, centred down it by its font's line height, clipped to the strip. The buttons do not change with
/// either colour, or with <c>Enabled</c>.</item>
/// <item>Each glyph is 11 rows tall, centred on the middle row of the control's height in pixels, rounded down (row 11 of 23 or of 24, counted from 0): a triangle six pixels wide at its middle
/// row, and on the outer buttons a bar two pixels wide.</item>
/// </list>
/// </remarks>
public sealed class VBData : Control
{
    private const double ButtonWidth = 19;

    // Where each glyph is, from its button's left edge: the triangle's base column, the way it points, and the bar's first column.
    private static readonly (double Base, int Toward, double? Bar)[] Glyphs = [(13, -1, 5), (11, -1, null), (6, 1, null), (5, 1, 12)];

    private string caption = "";
    private Appearance appearance = Appearance.ThreeD;
    private IBrush back = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.Window));
    private IBrush fore = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.WindowText));
    private FormFont font = FormFont.Default;

    /// <summary>
    /// Creates a control that draws a Data control.
    /// </summary>
    /// <param name="instance">The Data control.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Data control.</exception>
    public VBData(ComponentInstance instance)
    {
        ClipToBounds = true;
        ComponentBinding.Bind(this, instance, VBClasses.Data, Apply);
    }

    /// <summary>
    /// The state of the current record's edit: always 0, <c>dbEditNone</c>, as no record is current. A program cannot set it: 383.
    /// </summary>
    public short EditMode => 0;

    /// <summary>
    /// The database the control opened: always Nothing. A program cannot set it: 383.
    /// </summary>
    public object? Database => null;

    /// <summary>
    /// The recordset the control opened: always Nothing.
    /// </summary>
    public object? Recordset => null;

    /// <summary>
    /// <c>UpdateRecord</c>: writes the bound controls' values to the current record.
    /// </summary>
    /// <exception cref="FormErrorException">The control has no recordset: 91, Object variable or With block variable not set.</exception>
    public void UpdateRecord() => throw new FormErrorException(FormError.ObjectVariableNotSet);

    /// <summary>
    /// <c>UpdateControls</c>: gives the bound controls the current record's values again.
    /// </summary>
    /// <exception cref="FormErrorException">The control has no recordset: 91, Object variable or With block variable not set.</exception>
    public void UpdateControls() => throw new FormErrorException(FormError.ObjectVariableNotSet);

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var (w, h) = (Math.Floor(Bounds.Width), Math.Floor(Bounds.Height));
        if (w <= 0 || h <= 0)
            return;
        var flat = appearance == Appearance.Flat;
        var parts = flat ? new Rect(1, 1, w - 1, h - 1) : new Rect(3, 3, w - 6, h - 6);
        if (!flat)
        {
            Ring(context, new Rect(0, 0, w, h), SystemColor.ButtonFace, SystemColor.ButtonFace);
            Ring(context, new Rect(1, 1, w - 2, h - 2), SystemColor.ButtonShadow, SystemColor.ButtonHighlight);
            Ring(context, new Rect(2, 2, w - 4, h - 4), SystemColor.ThreeDDarkShadow, SystemColor.ThreeDLight);
        }
        if (parts.Width > 0 && parts.Height > 0)
        {
            using (context.PushClip(parts))
                DrawParts(context, parts, flat, centre: Math.Floor((h - 1) / 2));
        }
        if (flat)
            Ring(context, new Rect(0, 0, w, h), SystemColor.WindowFrame, SystemColor.WindowFrame);
    }

    // The first two buttons, the strip, then the column and the last two buttons over them.
    private void DrawParts(DrawingContext context, Rect parts, bool flat, double centre)
    {
        var right = parts.Right;
        Button(context, parts, 0, parts.X, centre);
        Button(context, parts, 1, parts.X + ButtonWidth, centre);
        var strip = new Rect(parts.X + 2 * ButtonWidth, parts.Y, right - parts.X - 4 * ButtonWidth - 1, parts.Height);
        if (strip.Width > 0)
            Strip(context, strip);
        context.FillRectangle(Brush(flat ? SystemColor.WindowFrame : SystemColor.ButtonFace), new Rect(right - 2 * ButtonWidth - 1, parts.Y, 1, parts.Height));
        Button(context, parts, 2, right - 2 * ButtonWidth, centre);
        Button(context, parts, 3, right - ButtonWidth, centre);
    }

    // A push button's face and raised edge, then its glyph in the button text's colour.
    private static void Button(DrawingContext context, Rect parts, int index, double x, double centre)
    {
        var face = new Rect(x, parts.Y, ButtonWidth, parts.Height);
        context.FillRectangle(Brush(SystemColor.ButtonFace), face);
        Ring(context, face, SystemColor.ButtonHighlight, SystemColor.ThreeDDarkShadow);
        Ring(context, face.Deflate(1), SystemColor.ThreeDLight, SystemColor.ButtonShadow);
        var ink = Brush(SystemColor.ButtonText);
        var (baseColumn, toward, bar) = Glyphs[index];
        for (var d = -5; d <= 5; d++)
        {
            var width = 6 - Math.Abs(d);
            var left = toward < 0 ? x + baseColumn - width + 1 : x + baseColumn;
            context.FillRectangle(ink, new Rect(left, centre + d, width, 1));
        }
        if (bar is { } column)
            context.FillRectangle(ink, new Rect(x + column, centre - 5, 2, 11));
    }

    private void Strip(DrawingContext context, Rect strip)
    {
        context.FillRectangle(back, strip);
        if (caption.Length == 0)
            return;
        var text = new FormattedText(caption, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FormFonts.ToFontFamily(font), font.Italic ? FontStyle.Italic : FontStyle.Normal, font.Bold ? FontWeight.Bold : FontWeight.Normal),
            FormFonts.ToFontSize(font), IsEffectivelyEnabled ? fore : Brush(SystemColor.GrayText));
        var top = strip.Y + Math.Floor((strip.Height - FormFonts.LineHeight(font)) / 2);
        using (context.PushClip(strip))
            context.DrawText(text, new Point(strip.X + 3, top));
    }

    // One pixel ring around a rectangle: its top and left in one colour, then its bottom and right in another over them.
    private static void Ring(DrawingContext context, Rect r, SystemColor topLeft, SystemColor bottomRight)
    {
        if (r.Width <= 0 || r.Height <= 0)
            return;
        var light = Brush(topLeft);
        var dark = Brush(bottomRight);
        context.FillRectangle(light, new Rect(r.X, r.Y, r.Width, 1));
        context.FillRectangle(light, new Rect(r.X, r.Y, 1, r.Height));
        context.FillRectangle(dark, new Rect(r.X, r.Bottom - 1, r.Width, 1));
        context.FillRectangle(dark, new Rect(r.Right - 1, r.Y, 1, r.Height));
    }

    private static IBrush Brush(SystemColor color) => FormPalette.ToBrush(FormColor.FromSystem(color));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Caption):
                caption = (string)e.After!;
                break;
            case nameof(P.Appearance):
                appearance = (Appearance)e.After!;
                break;
            case nameof(P.BackColor):
                back = ComponentBinding.Brush(e.After);
                break;
            case nameof(P.ForeColor):
                fore = ComponentBinding.Brush(e.After);
                break;
            case nameof(P.Font):
                font = (FormFont)e.After!;
                break;
            case nameof(P.Enabled):
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                return;
        }
        InvalidateVisual();
    }
}
