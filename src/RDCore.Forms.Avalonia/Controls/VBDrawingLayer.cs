using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// The drawing surface of a form or a PictureBox: the layer its drawing statements draw on, under its controls.
/// </summary>
/// <remarks>
/// <para>
/// It covers the component's inside, and keeps what is drawn on it in a bitmap whatever the component's <c>AutoRedraw</c>, as large
/// as the inside has been. Where <c>AutoRedraw</c> is False, the component raises <c>Paint</c> once the surface is shown, and each time
/// its inside grows.
/// </para>
/// <para>
/// It draws as GDI does, with no smoothing. A point is placed on the pixel nearest its twips, rounded half to even. A line is drawn as a
/// Line control draws it. A box covers both of its corners. Lines are dashed only where they are one pixel wide, and a fill's lines
/// are eight pixels apart from the surface's corner. The component's <c>DrawMode</c> combines each pixel of a line and of a fill with
/// the pixel under it, GDI's raster operation of that number: what is drawn there, or else the <see cref="Picture"/>'s pixel over the
/// <c>BackColor</c>.
/// </para>
/// <para>
/// A <c>Picture</c> set on the component erases what is drawn, and so does a <c>BackColor</c> set on a PictureBox, or a new
/// <c>BackColor</c> on a form. <see cref="CurrentX"/> and <see cref="CurrentY"/> are kept.
/// </para>
/// <para>
/// While it is shown, <see cref="DrawingSurfaces.Of"/> gives it for its component. It takes no input.
/// </para>
/// </remarks>
public sealed class VBDrawingLayer : Control, IDrawingSurface
{
    private RenderTargetBitmap? bitmap;
    // The bitmap's pixels as Point and the raster operations read them, copied when they are read after a drawing.
    private int[]? drawn;
    private Bitmap? picture;
    // The picture's pixels as its layer draws them, copied when they are first read.
    private int[]? pictureDrawn;
    private PixelSize pictureSize;
    private PixelSize inside;
    private bool attached;
    private bool paintPending;

    /// <summary>
    /// Creates the drawing surface of a form's root or a PictureBox.
    /// </summary>
    /// <param name="component">The form's root or the PictureBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> is <see langword="null"/>.</exception>
    public VBDrawingLayer(ComponentInstance component)
    {
        ArgumentNullException.ThrowIfNull(component);
        Component = component;
        IsHitTestVisible = false;
        Focusable = false;
        ClipToBounds = true;
        SizeToInside();
    }

    /// <inheritdoc/>
    public ComponentInstance Component { get; }

    /// <inheritdoc/>
    public double CurrentX { get; set; }

    /// <inheritdoc/>
    public double CurrentY { get; set; }

    /// <summary>
    /// The size of its component's inside, in pixels.
    /// </summary>
    public PixelSize Inside => inside;

    /// <summary>
    /// The picture drawn under the surface, at its size in pixels from the top left corner, or <see langword="null"/> for none. Its
    /// owner sets it as the component's <c>Picture</c> is decoded; <c>Point</c> and the raster operations read its pixels. Setting it
    /// erases nothing: the component's <c>Picture</c> set does.
    /// </summary>
    public Bitmap? Picture
    {
        get => picture;
        set
        {
            picture = value;
            pictureDrawn = null;
        }
    }

    // Whether a change to a container's property changes its scale.
    internal static bool AffectsScale(PropertyDefinition property) => property.Name is nameof(P.ScaleMode) or nameof(P.ScaleLeft)
        or nameof(P.ScaleTop) or nameof(P.ScaleWidth) or nameof(P.ScaleHeight) or nameof(P.Width) or nameof(P.Height)
        or nameof(P.BorderStyle) or nameof(P.Appearance);

    // A component's inside, in twips: a PictureBox's is within its border, and a root's Width and Height are its inside.
    internal static (double Width, double Height) InsideTwips(ComponentInstance component) => DrawingScale.InsideOf(component);

    // The scale of a container, null for none.
    internal static DrawingScale ScaleOf(ComponentInstance? container) => DrawingScale.Of(container);

    // A point in a scale as whole twips, rounded half to even, kept within what a pixel position can hold.
    internal static (double X, double Y) WholeTwips(DrawingScale scale, double x, double y)
    {
        var (across, down) = scale.ToTwips(x, y);
        return (Whole(across), Whole(down));

        static double Whole(double value)
        {
            const double limit = int.MaxValue / 16.0 * ScaleModeExtensions.TwipsPerPixel;
            var twips = Math.Round(value, MidpointRounding.ToEven);
            return double.IsNaN(twips) ? 0 : Math.Clamp(twips, -limit, limit);
        }
    }

    // The pixel nearest a position in twips. Twips are whole and a pixel is 15 of them, so no position is halfway.
    internal static int Pixel(double twips) => (int)Math.Round(twips / ScaleModeExtensions.TwipsPerPixel);

    /// <inheritdoc/>
    public void Line(GraphicsPoint? from, GraphicsPoint to, FormColor? color = null, LineBox box = LineBox.None)
    {
        var (x1, y1) = from is { } start ? Resolve(start, CurrentX, CurrentY) : (CurrentX, CurrentY);
        var (x2, y2) = Resolve(to, x1, y1);
        CurrentX = x2;
        CurrentY = y2;
        var brush = BrushOf(color);
        var (px1, py1) = ToPixel(x1, y1);
        var (px2, py2) = ToPixel(x2, y2);
        var (width, style) = (DrawWidth, BorderStyle);
        var area = Around(Math.Min(px1, px2), Math.Min(py1, py2), Math.Max(px1, px2), Math.Max(py1, py2), width);
        if (box == LineBox.None)
        {
            Draw(area, null, context => GdiDrawing.Line(context, brush, px1, py1, px2, py2, width, style));
            return;
        }
        var pixels = new Rect(Math.Min(px1, px2), Math.Min(py1, py2), Math.Abs(px2 - px1) + 1, Math.Abs(py2 - py1) + 1);
        if (box == LineBox.FilledBox)
        {
            Draw(area, null, context => context.FillRectangle(brush, pixels));
            return;
        }
        var pen = GdiDrawing.Pen(brush, width, style);
        Draw(area, FillOf(new RectangleGeometry(pixels)),
            pen is null ? null : context => context.DrawRectangle(null, pen, PenPath(pixels, width, style)));
    }

    /// <inheritdoc/>
    public void Circle(GraphicsPoint centre, double radius, FormColor? color = null, double? start = null, double? end = null, double? aspect = null)
    {
        if (start is < -2 * Math.PI or > 2 * Math.PI || end is < -2 * Math.PI or > 2 * Math.PI)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        var (x, y) = Resolve(centre, CurrentX, CurrentY);
        CurrentX = x;
        CurrentY = y;
        var (px, py) = ToPixel(x, y);
        var centrePoint = new Point(px + 0.5, py + 0.5);
        var r = Math.Abs(radius * ScaleOf(Component).TwipsAcross) / ScaleModeExtensions.TwipsPerPixel;
        var ratio = Math.Abs(aspect ?? 1);
        var (rx, ry) = ratio < 1 ? (r, r * ratio) : (r / ratio, r);
        var brush = BrushOf(color);
        var pen = GdiDrawing.Pen(brush, DrawWidth, BorderStyle);

        Geometry figure;
        var closed = true;
        if (start is null && end is null)
            figure = new EllipseGeometry(new Rect(centrePoint.X - rx, centrePoint.Y - ry, 2 * rx, 2 * ry));
        else
        {
            double from = Math.Abs(start ?? 0), to = Math.Abs(end ?? 2 * Math.PI);
            bool fromCentre = start < 0, toCentre = end < 0;
            closed = fromCentre && toCentre;
            var sweep = to - from;
            if (sweep <= 0)
                sweep += 2 * Math.PI;
            Point At(double angle) => new(centrePoint.X + rx * Math.Cos(angle), centrePoint.Y - ry * Math.Sin(angle));
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
            {
                path.BeginFigure(fromCentre ? centrePoint : At(from), closed);
                if (fromCentre)
                    path.LineTo(At(from));
                path.ArcTo(At(to), new Size(rx, ry), 0, sweep > Math.PI, SweepDirection.CounterClockwise);
                if (toCentre)
                    path.LineTo(centrePoint);
                path.EndFigure(closed);
            }
            figure = geometry;
        }

        var reach = DrawWidth + 2;
        var area = Around((int)Math.Floor(centrePoint.X - rx), (int)Math.Floor(centrePoint.Y - ry), (int)Math.Ceiling(centrePoint.X + rx),
            (int)Math.Ceiling(centrePoint.Y + ry), reach);
        Draw(area, closed ? FillOf(figure) : null, pen is null ? null : context => context.DrawGeometry(null, pen, figure));
    }

    /// <inheritdoc/>
    public void PSet(GraphicsPoint point, FormColor? color = null)
    {
        var (x, y) = Resolve(point, CurrentX, CurrentY);
        CurrentX = x;
        CurrentY = y;
        var (px, py) = ToPixel(x, y);
        var brush = BrushOf(color);
        var width = DrawWidth;
        Draw(Around(px, py, px, py, width), null, context =>
        {
            if (width <= 1)
                context.FillRectangle(brush, new Rect(px, py, 1, 1));
            else
                context.DrawEllipse(brush, null, new Point(px + 0.5, py + 0.5), width / 2.0, width / 2.0);
        });
    }

    /// <inheritdoc/>
    public void Scale()
    {
        var (width, height) = InsideTwips(Component);
        Component.Set(P.ScaleMode, ScaleMode.Twip).Set(P.ScaleLeft, 0d).Set(P.ScaleTop, 0d).Set(P.ScaleWidth, width).Set(P.ScaleHeight, height);
    }

    /// <inheritdoc/>
    public void Scale(double x1, double y1, double x2, double y2) =>
        Component.Set(P.ScaleMode, ScaleMode.User).Set(P.ScaleLeft, x1).Set(P.ScaleTop, y1).Set(P.ScaleWidth, x2 - x1).Set(P.ScaleHeight, y2 - y1);

    /// <inheritdoc/>
    public void Cls()
    {
        Erase();
        CurrentX = 0;
        CurrentY = 0;
    }

    /// <inheritdoc/>
    public int Point(double x, double y)
    {
        var (px, py) = ToPixel(x, y);
        if (px < 0 || py < 0 || px >= inside.Width || py >= inside.Height)
            return -1;
        var colour = Under(px, py);
        // As a VB colour: red in the low byte.
        return (colour >> 16 & 0xFF) | (colour & 0xFF00) | (colour & 0xFF) << 16;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (bitmap is not null)
            context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attached = true;
        Component.PropertyChanged += OnComponentChanged;
        DrawingSurfaces.Register(this);
        SizeToInside();
        PostPaint();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        attached = false;
        Component.PropertyChanged -= OnComponentChanged;
        DrawingSurfaces.Unregister(this);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnComponentChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        // VB6 erases a PictureBox's drawing for any BackColor set, and a form's for a new one only.
        if (ReferenceEquals(e.Property, P.Picture)
            || ReferenceEquals(e.Property, P.BackColor) && (Component.Class != VBClasses.Form || !Equals(e.Before, e.After)))
        {
            Erase();
            return;
        }
        if (e.Property.Name is not (nameof(P.Width) or nameof(P.Height) or nameof(P.BorderStyle) or nameof(P.Appearance)))
            return;
        var before = inside;
        SizeToInside();
        if (inside.Width > before.Width || inside.Height > before.Height)
            PostPaint();
    }

    // Sizes the layer to its component's inside.
    private void SizeToInside()
    {
        var (width, height) = InsideTwips(Component);
        inside = new PixelSize(Math.Max(0, Pixel(width)), Math.Max(0, Pixel(height)));
        Width = inside.Width;
        Height = inside.Height;
    }

    // Refresh raises Paint inside the call, where AutoRedraw is False.
    internal void PaintNow()
    {
        if (attached && !(Component.Class.Has(P.AutoRedraw) && Component.Get(P.AutoRedraw)))
            VBControl.RaiseFor(this, Component, VBEvents.Paint);
    }

    // One Paint waits at a time, as Windows keeps one paint message for a window: a surface that grows across and down at once, as a
    // window does under its user's drag, raises one.
    private void PostPaint()
    {
        if (paintPending || Component.Class.Has(P.AutoRedraw) && Component.Get(P.AutoRedraw))
            return;
        paintPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            paintPending = false;
            if (attached)
                VBControl.RaiseFor(this, Component, VBEvents.Paint);
        }, DispatcherPriority.Background);
    }

    private int DrawWidth => Component.Class.Has(P.DrawWidth) ? Component.Get(P.DrawWidth) : 1;

    private ShapeBorderStyle BorderStyle =>
        GdiDrawing.AsBorderStyle(Component.Class.Has(P.DrawStyle) ? Component.Get(P.DrawStyle) : DrawStyle.Solid);

    private IBrush BrushOf(FormColor? color) => FormPalette.ToBrush(color ?? Component.Get(P.ForeColor));

    private static (double X, double Y) Resolve(GraphicsPoint point, double fromX, double fromY) =>
        point.IsRelative ? (fromX + point.X, fromY + point.Y) : (point.X, point.Y);

    private (int X, int Y) ToPixel(double x, double y)
    {
        var (across, down) = WholeTwips(ScaleOf(Component), x, y);
        return (Pixel(across), Pixel(down));
    }

    // A box's pen is centred on its outermost pixels, and Inside Solid's within them.
    private static Rect PenPath(Rect pixels, int width, ShapeBorderStyle style)
    {
        var path = new Rect(pixels.X + 0.5, pixels.Y + 0.5, Math.Max(0, pixels.Width - 1), Math.Max(0, pixels.Height - 1));
        return style == ShapeBorderStyle.InsideSolid && width > 1 ? path.Deflate((width - 1) / 2.0) : path;
    }

    // What fills a figure with the component's FillColor in its FillStyle; null where it is Transparent.
    private Action<DrawingContext>? FillOf(Geometry figure)
    {
        var style = Component.Class.Has(P.FillStyle) ? Component.Get(P.FillStyle) : FillStyle.Transparent;
        if (style == FillStyle.Transparent)
            return null;
        var brush = FormPalette.ToBrush(Component.Get(P.FillColor));
        if (style == FillStyle.Solid)
            return context => context.DrawGeometry(brush, null, figure);
        var (width, height) = (Math.Max(inside.Width, bitmap?.PixelSize.Width ?? 0), Math.Max(inside.Height, bitmap?.PixelSize.Height ?? 0));
        return context =>
        {
            using var clip = context.PushGeometryClip(figure);
            GdiDrawing.Hatch(context, brush, style, width, height);
        };
    }

    // The pixels a figure from (left, top) to (right, bottom) can cover, with a pen that wide around it.
    private static PixelRect Around(int left, int top, int right, int bottom, int width) =>
        new(left - width - 1, top - width - 1, right - left + 2 * width + 3, bottom - top + 2 * width + 3);

    private short DrawMode => Component.Class.Has(P.DrawMode) ? Component.Get(P.DrawMode) : (short)13;

    // Draws a figure, its fill and its edge, on the bitmap in the component's DrawMode. Copy Pen draws them on it directly, the edge
    // over the fill. Another mode draws them on their own, within the area they can cover, and combines each pixel with the pixel under
    // it: the edge's colour where the edge covers it, as GDI's fill stops under the edge, and the fill's elsewhere.
    private void Draw(PixelRect area, Action<DrawingContext>? fill, Action<DrawingContext>? edge)
    {
        var mode = DrawMode;
        if (mode == 11 || fill is null && edge is null)
            return;
        Grow();
        if (mode == 13)
        {
            using (var context = bitmap!.CreateDrawingContext(false))
            using (context.PushRenderOptions(Aliased))
            {
                fill?.Invoke(context);
                edge?.Invoke(context);
            }
            drawn = null;
        }
        else
            Combine(mode, area.Intersect(new PixelRect(bitmap!.PixelSize)), fill, edge);
        InvalidateVisual();
    }

    // The raster operation of a figure within an area of the bitmap. The copy Point reads is kept up to date, so a run of figures reads
    // the bitmap once.
    private void Combine(int mode, PixelRect area, Action<DrawingContext>? fill, Action<DrawingContext>? edge)
    {
        if (area.Width <= 0 || area.Height <= 0)
            return;
        int[]? Alone(Action<DrawingContext>? draw)
        {
            if (draw is null)
                return null;
            using var alone = new RenderTargetBitmap(area.Size, new Vector(96, 96));
            using (var context = alone.CreateDrawingContext(true))
            using (context.PushRenderOptions(Aliased))
            using (context.PushTransform(Matrix.CreateTranslation(-area.X, -area.Y)))
                draw(context);
            return PixelsOf(alone);
        }
        var (filled, edged) = (Alone(fill), Alone(edge));
        var width = bitmap!.PixelSize.Width;
        using var combined = new WriteableBitmap(area.Size, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using (var target = combined.Lock())
        {
            var row = new int[area.Width];
            for (var y = 0; y < area.Height; y++)
            {
                for (var x = 0; x < area.Width; x++)
                {
                    var at = y * area.Width + x;
                    var source = edged is { } e && (uint)e[at] >> 24 != 0 ? e[at] : filled?[at] ?? 0;
                    if ((uint)source >> 24 == 0)
                    {
                        row[x] = 0;
                        continue;
                    }
                    var (atX, atY) = (area.X + x, area.Y + y);
                    row[x] = unchecked((int)0xFF000000) | RasterOperation(mode, Under(atX, atY), Unpremultiplied(source));
                    drawn![atY * width + atX] = row[x];
                }
                Marshal.Copy(row, 0, target.Address + y * target.RowBytes, area.Width);
            }
        }
        using var onto = bitmap.CreateDrawingContext(false);
        using (onto.PushRenderOptions(Aliased))
            onto.DrawImage(combined, new Rect(area.X, area.Y, area.Width, area.Height));
    }

    private static readonly RenderOptions Aliased = new() { EdgeMode = EdgeMode.Aliased, BitmapInterpolationMode = BitmapInterpolationMode.None };

    // GDI's raster operation of a DrawMode, on the RGB of what is there and of the pen.
    internal static int RasterOperation(int mode, int there, int pen)
    {
        var (d, p) = (there & 0xFFFFFF, pen & 0xFFFFFF);
        var result = mode switch
        {
            1 => 0,
            2 => ~(d | p),
            3 => d & ~p,
            4 => ~p,
            5 => p & ~d,
            6 => ~d,
            7 => d ^ p,
            8 => ~(d & p),
            9 => d & p,
            10 => ~(d ^ p),
            11 => d,
            12 => d | ~p,
            14 => p | ~d,
            15 => d | p,
            16 => 0xFFFFFF,
            _ => p,
        };
        return result & 0xFFFFFF;
    }

    // Grows the bitmap to the inside where the inside is larger, keeping what is drawn.
    private void Grow()
    {
        var size = new PixelSize(Math.Max(1, Math.Max(inside.Width, bitmap?.PixelSize.Width ?? 0)),
            Math.Max(1, Math.Max(inside.Height, bitmap?.PixelSize.Height ?? 0)));
        if (bitmap is not null && bitmap.PixelSize == size)
            return;
        var grown = new RenderTargetBitmap(size, new Vector(96, 96));
        using (var copy = grown.CreateDrawingContext(true))
            if (bitmap is not null)
                copy.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
        bitmap?.Dispose();
        bitmap = grown;
        drawn = null;
    }

    // Clears what is drawn.
    private void Erase()
    {
        bitmap?.Dispose();
        bitmap = null;
        drawn = null;
        InvalidateVisual();
    }

    // What shows at a pixel, as RGB: what is drawn there, or else the picture's pixel over the BackColor.
    private int Under(int x, int y)
    {
        if (bitmap is not null && x < bitmap.PixelSize.Width && y < bitmap.PixelSize.Height)
        {
            drawn ??= PixelsOf(bitmap);
            var pixel = drawn[y * bitmap.PixelSize.Width + x];
            if ((uint)pixel >> 24 != 0)
                return Unpremultiplied(pixel);
        }
        var back = FormPalette.ToColor(Component.Get(P.BackColor));
        var backRgb = back.R << 16 | back.G << 8 | back.B;
        if (picture is null)
            return backRgb;
        if (pictureDrawn is null)
        {
            pictureSize = picture.PixelSize;
            using var shown = new RenderTargetBitmap(pictureSize, new Vector(96, 96));
            using (var context = shown.CreateDrawingContext(true))
            using (context.PushRenderOptions(Aliased))
                VBPicture.Draw(context, picture, new Size(pictureSize.Width, pictureSize.Height));
            pictureDrawn = PixelsOf(shown);
        }
        if (x >= pictureSize.Width || y >= pictureSize.Height)
            return backRgb;
        // Premultiplied, so the picture's channels already hold its share; the BackColor shows through the rest.
        var over = pictureDrawn[y * pictureSize.Width + x];
        var alpha = (uint)over >> 24;
        int Channel(int shift) => Math.Min(255, (over >> shift & 0xFF) + (int)((backRgb >> shift & 0xFF) * (255 - alpha) / 255));
        return Channel(16) << 16 | Channel(8) << 8 | Channel(0);
    }

    // A pixel that is opaque, or empty, is its colour; one in between is undone to its colour.
    private static int Unpremultiplied(int pixel)
    {
        var alpha = (uint)pixel >> 24;
        if (alpha is 0 or 255)
            return pixel & 0xFFFFFF;
        int Channel(int shift) => (int)Math.Min(255, (pixel >> shift & 0xFF) * 255 / alpha);
        return Channel(16) << 16 | Channel(8) << 8 | Channel(0);
    }

    // A bitmap's pixels, as premultiplied 0xAARRGGBB, row after row.
    private static int[] PixelsOf(Bitmap source)
    {
        var size = source.PixelSize;
        using var copy = new WriteableBitmap(size, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        source.CopyPixels(buffer);
        var pixels = new int[size.Width * size.Height];
        for (var y = 0; y < size.Height; y++)
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, pixels, y * size.Width, size.Width);
        if (buffer.Format == PixelFormats.Rgba8888)
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = (int)((uint)pixels[i] & 0xFF00FF00) | (pixels[i] & 0xFF) << 16 | (pixels[i] >> 16 & 0xFF);
        return pixels;
    }
}
