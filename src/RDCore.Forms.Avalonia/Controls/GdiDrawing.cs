// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Media;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

// What a Line, a Shape and a drawing surface draw as GDI draws it: pens, their dashes, and hatched fills.
internal static class GdiDrawing
{
    // Past this many pixels a one-pixel line is drawn with Avalonia's pen rather than pixel by pixel.
    private const int MaxSteppedLength = 32_768;

    // The spacing of a hatched fill's lines.
    public const int HatchSpacing = 8;

    // The runs of pixels on and off a one-pixel GDI pen draws for a style, from where it starts (measured with VB6's Line).
    public static double[]? Dashes(ShapeBorderStyle style) => style switch
    {
        ShapeBorderStyle.Dash => [18, 6],
        ShapeBorderStyle.Dot => [3, 3],
        ShapeBorderStyle.DashDot => [9, 6, 3, 6],
        ShapeBorderStyle.DashDotDot => [9, 3, 3, 3, 3, 3],
        _ => null,
    };

    // A DrawStyle's pen, as the ShapeBorderStyle of the same name.
    public static ShapeBorderStyle AsBorderStyle(DrawStyle style) => style switch
    {
        DrawStyle.Dash => ShapeBorderStyle.Dash,
        DrawStyle.Dot => ShapeBorderStyle.Dot,
        DrawStyle.DashDot => ShapeBorderStyle.DashDot,
        DrawStyle.DashDotDot => ShapeBorderStyle.DashDotDot,
        DrawStyle.Transparent => ShapeBorderStyle.Transparent,
        DrawStyle.InsideSolid => ShapeBorderStyle.InsideSolid,
        _ => ShapeBorderStyle.Solid,
    };

    // A pen width wide in a style: dashed only where it is one pixel wide, as a GDI pen is; null where the style draws nothing.
    public static IPen? Pen(IBrush brush, int width, ShapeBorderStyle style)
    {
        if (style == ShapeBorderStyle.Transparent || width <= 0)
            return null;
        var dashes = width == 1 ? Dashes(style) : null;
        return new Pen(brush, width, dashes is null ? null : new DashStyle(dashes, 0));
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

    // A line between two pixels, as VB6 draws a Line (measured).
    //  - One pixel wide: every pixel from the first end up to the second, the second left out, stepping as a midpoint line does, and
    //    dashed by its style counting pixels from the first end.
    //  - Wider: solid whatever its style, and covering both ends; across, width rows centred on the end's, the extra one above where
    //    the width is even, and a vertical line the same in columns, the extra one to the left.
    public static void Line(DrawingContext context, IBrush brush, int x1, int y1, int x2, int y2, int width, ShapeBorderStyle style)
    {
        if (style == ShapeBorderStyle.Transparent || width <= 0 || x1 == x2 && y1 == y2)
            return;
        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        var major = Math.Max(dx, dy);

        if (width == 1 && major <= MaxSteppedLength)
        {
            var dashes = Dashes(style);
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

        var before = width / 2;
        if (y1 == y2)
            context.FillRectangle(brush, new Rect(Math.Min(x1, x2), y1 - before, dx + 1, width));
        else if (x1 == x2)
            context.FillRectangle(brush, new Rect(x1 - before, Math.Min(y1, y2), width, dy + 1));
        else
            // A wide diagonal is GDI's geometric pen with round ends, which this approximates.
            context.DrawLine(new Pen(brush, width, lineCap: PenLineCap.Round), new Point(x1 + 0.5, y1 + 0.5), new Point(x2 + 0.5, y2 + 0.5));
    }

    // A hatched fill's lines over a width and a height from the origin, one pixel wide and eight apart: the caller clips them to the
    // figure. A diagonal line covers the pixels whose x + y (rising) or x - y (falling) is a multiple of the spacing.
    public static void Hatch(DrawingContext context, IBrush brush, FillStyle style, double width, double height)
    {
        using var aliased = context.PushRenderOptions(new RenderOptions { EdgeMode = EdgeMode.Aliased });
        var pen = new Pen(brush, 1);
        if (style is FillStyle.HorizontalLine or FillStyle.Cross)
            for (var y = 0; y < height; y += HatchSpacing)
                context.DrawLine(pen, new Point(0, y + 0.5), new Point(width, y + 0.5));
        if (style is FillStyle.VerticalLine or FillStyle.Cross)
            for (var x = 0; x < width; x += HatchSpacing)
                context.DrawLine(pen, new Point(x + 0.5, 0), new Point(x + 0.5, height));
        var span = width + height;
        if (style is FillStyle.UpwardDiagonal or FillStyle.DiagonalCross)
            for (var start = 0; start < span; start += HatchSpacing)
                context.DrawLine(pen, new Point(start + 0.5, 0.5), new Point(start + 0.5 - span, 0.5 + span));
        if (style is FillStyle.DownwardDiagonal or FillStyle.DiagonalCross)
            for (var start = -(int)height / HatchSpacing * HatchSpacing; start < width; start += HatchSpacing)
                context.DrawLine(pen, new Point(start + 0.5, 0.5), new Point(start + 0.5 + span, 0.5 + span));
    }
}
