// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Line: a control that draws a <see cref="VBClasses.Line"/> component, pixel for pixel as VB6 draws it.
/// </summary>
/// <remarks>
/// <para>
/// Its ends, <c>X1</c>, <c>Y1</c>, <c>X2</c> and <c>Y2</c>, are in its container's scale, as
/// <see cref="DrawingScale.Of(ComponentInstance?)"/> gives it: the form's or a PictureBox's <c>ScaleMode</c>, and twips in a Frame.
/// Each end is held as whole twips, rounded half to even, and drawn on the pixel nearest them. The line follows its ends and its
/// container's scale as they change.
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
        if (VBDrawingLayer.AffectsScale(e.Property))
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

    // The rectangle a line covers on its container, in pixels: its pixels and the width of its pen, with a pixel to spare.
    private Rect LineBounds()
    {
        var pad = Math.Max(1, width) + 1;
        int x1 = VBDrawingLayer.Pixel(twips[0]), y1 = VBDrawingLayer.Pixel(twips[1]);
        int x2 = VBDrawingLayer.Pixel(twips[2]), y2 = VBDrawingLayer.Pixel(twips[3]);
        return new Rect(Math.Min(x1, x2) - pad, Math.Min(y1, y2) - pad, Math.Abs((double)x2 - x1) + 2 * pad + 1,
            Math.Abs((double)y2 - y1) + 2 * pad + 1);
    }

    private void Place()
    {
        var scale = VBDrawingLayer.ScaleOf(instance.Container);
        (twips[0], twips[1]) = VBDrawingLayer.WholeTwips(scale, instance.Get(P.X1), instance.Get(P.Y1));
        (twips[2], twips[3]) = VBDrawingLayer.WholeTwips(scale, instance.Get(P.X2), instance.Get(P.Y2));
        var bounds = LineBounds();
        Canvas.SetLeft(this, bounds.X);
        Canvas.SetTop(this, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;
        InvalidateVisual();
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var origin = LineBounds();
        GdiDrawing.Line(context, brush,
            VBDrawingLayer.Pixel(twips[0]) - (int)origin.X, VBDrawingLayer.Pixel(twips[1]) - (int)origin.Y,
            VBDrawingLayer.Pixel(twips[2]) - (int)origin.X, VBDrawingLayer.Pixel(twips[3]) - (int)origin.Y, width, style);
    }
}
