// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Line: a control that draws a <see cref="VBClasses.Line"/> component, pixel for pixel as VB6 draws it.
/// </summary>
/// <remarks>
/// <para>
/// Its ends, <c>X1</c>, <c>Y1</c>, <c>X2</c> and <c>Y2</c>, are in its container's scale: the form's or a PictureBox's
/// <c>ScaleMode</c>, and twips in a Frame. A user scale's unit is the container's inside over its <c>ScaleWidth</c> and
/// <c>ScaleHeight</c>, in single precision, from <c>ScaleLeft</c> and <c>ScaleTop</c>. Each end is held as whole twips, rounded half
/// to even, and drawn on the pixel nearest them. The line follows its ends and its container's scale as they change.
/// </para>
/// <para>
/// A line one pixel wide is GDI's: every pixel from the first end up to the second, the second left out, stepping as a midpoint line
/// does, and dashed by its <c>BorderStyle</c> counting pixels from the first end. A wider line is solid whatever its
/// <c>BorderStyle</c> and covers both ends; across, it covers <c>BorderWidth</c> rows centred on the end's row, the extra one above
/// where the width is even, and a vertical line the same in columns, the extra one to the left. Inside Solid draws as Solid, and
/// Transparent draws nothing. Every <c>DrawMode</c> draws as Copy Pen, 13.
/// </para>
/// <para>
/// It covers the rectangle around its ends and pen, and takes no input: a click on it is a click on what is under it.
/// </para>
/// </remarks>
public sealed class VBLine : Control
{
    // Past this many pixels a line is drawn with Avalonia's pen rather than pixel by pixel.
    private const int MaxSteppedLength = 32_768;

    private readonly ComponentInstance instance;
    private readonly double[] twips = new double[4];
    private ComponentInstance? followed;
    private IBrush brush = Brushes.Black;
    private ShapeBorderStyle style = ShapeBorderStyle.Solid;
    private int width = 1;

    /// <summary>
    /// Creates a control that draws a Line.
    /// </summary>
    /// <param name="instance">The Line.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Line.</exception>
    public VBLine(ComponentInstance instance)
    {
        IsHitTestVisible = false;
        this.instance = instance;
        ComponentBinding.Bind(this, instance, VBClasses.Line, Apply);
    }

    /// <summary>
    /// Its first end, in whole twips from its container's inside corner.
    /// </summary>
    public Point Start => new(twips[0], twips[1]);

    /// <summary>
    /// Its second end, in whole twips from its container's inside corner.
    /// </summary>
    public Point End => new(twips[2], twips[3]);

    /// <summary>
    /// How many twips one unit of a Line's container is, across and down, and the container's left and top in its own units.
    /// </summary>
    /// <param name="container">The container, or <see langword="null"/> for none.</param>
    /// <returns>
    /// The scale's twips per unit and origin: twips from 0 in a container with no scale, such as a Frame, and where the scale has no
    /// width or height.
    /// </returns>
    public static (double Across, double Down, double Left, double Top) ScaleOf(ComponentInstance? container)
    {
        if (container is null || !container.Class.Has(P.ScaleMode))
            return (1, 1, 0, 0);
        double left = container.Get(P.ScaleLeft), top = container.Get(P.ScaleTop);
        var mode = container.Get(P.ScaleMode);
        if (mode != ScaleMode.User)
        {
            var (across, down) = mode.TwipsPerUnit();
            return (across, down, left, top);
        }
        double scaleWidth = container.Get(P.ScaleWidth), scaleHeight = container.Get(P.ScaleHeight);
        if (scaleWidth == 0 || scaleHeight == 0)
            return (1, 1, 0, 0);
        // A PictureBox's inside is within its border; a root's Width and Height are its inside.
        var border = container.Class.Has(P.Appearance) && container.Class.Has(P.BorderStyle) && container.Class.Kind != ComponentKind.Root
            ? VBEdge.WidthOf(VBEdge.Of(container.Get(P.BorderStyle), container.Get(P.Appearance))) * 2 * ScaleModeExtensions.TwipsPerPixel
            : 0;
        // VB holds a scale in single precision.
        return ((float)(container.Get(P.Width) - border) / (float)scaleWidth, (float)(container.Get(P.Height) - border) / (float)scaleHeight,
            left, top);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Follow(instance.Container);
        Place();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Follow(null);
        base.OnDetachedFromVisualTree(e);
    }

    private void Follow(ComponentInstance? container)
    {
        if (followed is not null)
            followed.PropertyChanged -= OnContainerChanged;
        followed = container;
        if (followed is not null)
            followed.PropertyChanged += OnContainerChanged;
    }

    private void OnContainerChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        if (e.Property.Name is nameof(P.ScaleMode) or nameof(P.ScaleLeft) or nameof(P.ScaleTop) or nameof(P.ScaleWidth)
            or nameof(P.ScaleHeight) or nameof(P.Width) or nameof(P.Height) or nameof(P.BorderStyle) or nameof(P.Appearance))
            Place();
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.X1) or nameof(P.Y1) or nameof(P.X2) or nameof(P.Y2):
                Place();
                break;
            case nameof(P.BorderWidth):
                width = (short)e.After!;
                Place();
                break;
            case nameof(P.BorderColor):
                brush = ComponentBinding.Brush(e.After);
                InvalidateVisual();
                break;
            // A Line's BorderStyle is the ShapeBorderStyle property, named BorderStyle as in VB.
            case nameof(P.BorderStyle):
                style = (ShapeBorderStyle)e.After!;
                InvalidateVisual();
                break;
            case nameof(P.DrawMode):
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    // The pixel nearest a position in twips. Twips are whole and a pixel is 15 of them, so no position is halfway.
    private static int Pixel(double twips) => (int)Math.Round(twips / ScaleModeExtensions.TwipsPerPixel);

    // A position in the container's units as whole twips, rounded half to even, kept within what a pixel position can hold.
    private static double WholeTwips(double value, double origin, double factor)
    {
        var twips = Math.Round((value - origin) * factor, MidpointRounding.ToEven);
        const double limit = int.MaxValue / 16.0 * ScaleModeExtensions.TwipsPerPixel;
        return double.IsNaN(twips) ? 0 : Math.Clamp(twips, -limit, limit);
    }

    // The rectangle a line covers on its container, in pixels: its pixels and the width of its pen, with a pixel to spare.
    private Rect LineBounds()
    {
        var pad = Math.Max(1, width) + 1;
        int x1 = Pixel(twips[0]), y1 = Pixel(twips[1]), x2 = Pixel(twips[2]), y2 = Pixel(twips[3]);
        return new Rect(Math.Min(x1, x2) - pad, Math.Min(y1, y2) - pad, Math.Abs((double)x2 - x1) + 2 * pad + 1,
            Math.Abs((double)y2 - y1) + 2 * pad + 1);
    }

    private void Place()
    {
        var (across, down, left, top) = ScaleOf(instance.Container);
        twips[0] = WholeTwips(instance.Get(P.X1), left, across);
        twips[1] = WholeTwips(instance.Get(P.Y1), top, down);
        twips[2] = WholeTwips(instance.Get(P.X2), left, across);
        twips[3] = WholeTwips(instance.Get(P.Y2), top, down);
        var bounds = LineBounds();
        Canvas.SetLeft(this, bounds.X);
        Canvas.SetTop(this, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;
        InvalidateVisual();
    }

    private static bool IsOn(double[]? dashes, int step)
    {
        if (dashes is null)
            return true;
        var at = step % (int)dashes.Sum();
        for (var i = 0; i < dashes.Length; i++)
        {
            if (at < dashes[i])
                return i % 2 == 0;
            at -= (int)dashes[i];
        }
        return false;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (style == ShapeBorderStyle.Transparent || width <= 0)
            return;
        var origin = LineBounds();
        int x1 = Pixel(twips[0]) - (int)origin.X, y1 = Pixel(twips[1]) - (int)origin.Y;
        int x2 = Pixel(twips[2]) - (int)origin.X, y2 = Pixel(twips[3]) - (int)origin.Y;
        if (x1 == x2 && y1 == y2)
            return;
        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        var major = Math.Max(dx, dy);

        if (width == 1 && major <= MaxSteppedLength)
        {
            var dashes = VBShape.Dashes(style);
            int sx = Math.Sign(x2 - x1), sy = Math.Sign(y2 - y1);
            // The midpoint line, stepping along the longer axis and along the shorter one only past a half: (300,200) to (310,205)
            // covers 300 and 301 on row 200, then two to a row. The second end is left out.
            var minor = Math.Min(dx, dy);
            var decision = 2 * minor - major;
            int x = x1, y = y1;
            for (var step = 0; step < major; step++)
            {
                if (IsOn(dashes, step))
                    context.FillRectangle(brush, new Rect(x, y, 1, 1));
                if (decision > 0)
                {
                    if (dx >= dy)
                        y += sy;
                    else
                        x += sx;
                    decision -= 2 * major;
                }
                decision += 2 * minor;
                if (dx >= dy)
                    x += sx;
                else
                    y += sy;
            }
            return;
        }

        // Solid, both ends covered, and the width across, centred, the extra one above or to the left.
        var before = width / 2;
        if (y1 == y2)
            context.FillRectangle(brush, new Rect(Math.Min(x1, x2), y1 - before, dx + 1, width));
        else if (x1 == x2)
            context.FillRectangle(brush, new Rect(x1 - before, Math.Min(y1, y2), width, dy + 1));
        else
            // A wide diagonal is GDI's geometric pen with round ends, which this approximates.
            context.DrawLine(new Pen(brush, width, lineCap: PenLineCap.Round), new Point(x1 + 0.5, y1 + 0.5), new Point(x2 + 0.5, y2 + 0.5));
    }
}
