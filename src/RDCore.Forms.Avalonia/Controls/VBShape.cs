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
/// A VB Shape: a control that draws a <see cref="VBClasses.Shape"/> component's figure.
/// </summary>
/// <remarks>
/// <para>
/// A rectangle and an oval fill the control; a square and a circle are as wide as its shorter side, centred in it. A rounded figure's
/// corners have a radius of a tenth of its shorter side.
/// </para>
/// <para>
/// The figure is drawn in three layers: its <c>BackColor</c> where <c>BackStyle</c> is Opaque, then its <c>FillColor</c> as its
/// <c>FillStyle</c> fills it (solid, or lines eight pixels apart), then its border in <c>BorderColor</c>, <c>BorderWidth</c> pixels
/// wide, inside the control. A border one pixel wide is dashed as its <c>BorderStyle</c> says; a wider one is solid, as a GDI pen
/// is. Inside Solid draws as Solid, and Transparent draws no border.
/// </para>
/// <para>
/// It takes no input: a click on it is a click on what is under it.
/// </para>
/// </remarks>
public sealed class VBShape : Control
{
    private ShapeType figure;
    private IBrush back = Brushes.Transparent;
    private bool opaque;
    private IBrush fill = Brushes.Black;
    private FillStyle fillStyle = FillStyle.Transparent;
    private IBrush border = Brushes.Black;
    private ShapeBorderStyle borderStyle = ShapeBorderStyle.Solid;
    private int borderWidth = 1;

    /// <summary>
    /// Creates a control that draws a Shape.
    /// </summary>
    /// <param name="instance">The Shape.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Shape.</exception>
    public VBShape(ComponentInstance instance)
    {
        IsHitTestVisible = false;
        ComponentBinding.Bind(this, instance, VBClasses.Shape, Apply);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (figure is ShapeType.Square or ShapeType.Circle or ShapeType.RoundedSquare)
        {
            var side = Math.Min(bounds.Width, bounds.Height);
            bounds = new Rect((bounds.Width - side) / 2, (bounds.Height - side) / 2, side, side);
        }

        var pen = GdiDrawing.Pen(border, borderWidth, borderStyle);
        // A pen is centred on its path, so the path is set in by half the pen to keep the border inside the control.
        var path = pen is not null ? bounds.Deflate(borderWidth / 2.0) : bounds;
        if (path.Width < 0 || path.Height < 0)
            return;
        var radius = figure is ShapeType.RoundedRectangle or ShapeType.RoundedSquare ? Math.Min(path.Width, path.Height) / 10 : 0;

        if (opaque)
            Draw(context, back, null, path, radius);
        if (fillStyle == FillStyle.Solid)
            Draw(context, fill, null, path, radius);
        else if (fillStyle != FillStyle.Transparent)
            Hatch(context, path, radius);
        if (pen is not null)
            Draw(context, null, pen, path, radius);
    }

    private void Draw(DrawingContext context, IBrush? brush, IPen? pen, Rect path, double radius)
    {
        if (figure is ShapeType.Oval or ShapeType.Circle)
            context.DrawEllipse(brush, pen, path);
        else
            context.DrawRectangle(brush, pen, path, radius, radius);
    }

    // Lines one pixel wide, eight pixels apart from the control's corner, clipped to the figure.
    private void Hatch(DrawingContext context, Rect path, double radius)
    {
        using var clip = figure is ShapeType.Oval or ShapeType.Circle
            ? context.PushGeometryClip(new EllipseGeometry(path))
            : context.PushClip(new RoundedRect(path, radius));
        GdiDrawing.Hatch(context, fill, fillStyle, Bounds.Width, Bounds.Height);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Shape):
                figure = (ShapeType)e.After!;
                break;
            case nameof(P.BackColor):
                back = ComponentBinding.Brush(e.After);
                break;
            case nameof(P.BackStyle):
                opaque = (BackStyle)e.After! == BackStyle.Opaque;
                break;
            case nameof(P.FillColor):
                fill = ComponentBinding.Brush(e.After);
                break;
            case nameof(P.FillStyle):
                fillStyle = (FillStyle)e.After!;
                break;
            case nameof(P.BorderColor):
                border = ComponentBinding.Brush(e.After);
                break;
            // A Shape's BorderStyle is the ShapeBorderStyle property, named BorderStyle as in VB.
            case nameof(P.BorderStyle):
                borderStyle = (ShapeBorderStyle)e.After!;
                break;
            case nameof(P.BorderWidth):
                borderWidth = (short)e.After!;
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                return;
        }
        InvalidateVisual();
    }
}
