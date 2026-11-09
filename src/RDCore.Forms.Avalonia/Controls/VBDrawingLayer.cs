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
/// are eight pixels apart from the surface's corner. Every <c>DrawMode</c> draws as Copy Pen, 13.
/// </para>
/// <para>
/// While it is shown, <see cref="DrawingSurfaces.Of"/> gives it for its component. It takes no input.
/// </para>
/// </remarks>
public sealed class VBDrawingLayer : Control, IDrawingSurface
{
    private RenderTargetBitmap? bitmap;
    // A copy of the bitmap that Point reads, made when Point is called after a drawing.
    private WriteableBitmap? readBack;
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

    // Whether a change to a container's property changes its scale.
    internal static bool AffectsScale(PropertyDefinition property) => property.Name is nameof(P.ScaleMode) or nameof(P.ScaleLeft)
        or nameof(P.ScaleTop) or nameof(P.ScaleWidth) or nameof(P.ScaleHeight) or nameof(P.Width) or nameof(P.Height)
        or nameof(P.BorderStyle) or nameof(P.Appearance);

    // A component's inside, in twips: a PictureBox's is within its border, and a root's Width and Height are its inside.
    internal static (double Width, double Height) InsideTwips(ComponentInstance component)
    {
        double width = component.Class.Has(P.Width) ? component.Get(P.Width) : 0, height = component.Class.Has(P.Height) ? component.Get(P.Height) : 0;
        if (component.Class.Kind == ComponentKind.Root || !component.Class.Has(P.Appearance) || !component.Class.Has(P.BorderStyle))
            return (width, height);
        var border = VBEdge.WidthOf(VBEdge.Of(component.Get(P.BorderStyle), component.Get(P.Appearance))) * 2 * ScaleModeExtensions.TwipsPerPixel;
        return (Math.Max(0, width - border), Math.Max(0, height - border));
    }

    // The scale of a container, null for none.
    internal static DrawingScale ScaleOf(ComponentInstance? container)
    {
        if (container is null)
            return DrawingScale.Twips;
        var (width, height) = InsideTwips(container);
        return DrawingScale.Of(container, width, height);
    }

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
        Draw(context =>
        {
            if (box == LineBox.None)
            {
                GdiDrawing.Line(context, brush, px1, py1, px2, py2, width, style);
                return;
            }
            var pixels = new Rect(Math.Min(px1, px2), Math.Min(py1, py2), Math.Abs(px2 - px1) + 1, Math.Abs(py2 - py1) + 1);
            if (box == LineBox.FilledBox)
            {
                context.FillRectangle(brush, pixels);
                return;
            }
            Fill(context, new RectangleGeometry(pixels));
            if (GdiDrawing.Pen(brush, width, style) is { } pen)
                context.DrawRectangle(null, pen, PenPath(pixels, width, style));
        });
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

        Draw(context =>
        {
            if (closed)
                Fill(context, figure);
            if (pen is not null)
                context.DrawGeometry(null, pen, figure);
        });
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
        Draw(context =>
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
        bitmap?.Dispose();
        bitmap = null;
        readBack?.Dispose();
        readBack = null;
        CurrentX = 0;
        CurrentY = 0;
        InvalidateVisual();
    }

    /// <inheritdoc/>
    public int Point(double x, double y)
    {
        var (px, py) = ToPixel(x, y);
        if (px < 0 || py < 0 || px >= inside.Width || py >= inside.Height)
            return -1;
        var back = FormPalette.ToColor(Component.Get(P.BackColor));
        var colour = Read(px, py) is { A: > 0 } drawn ? drawn : back;
        return colour.R | colour.G << 8 | colour.B << 16;
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

    // Fills a figure with the component's FillColor in its FillStyle.
    private void Fill(DrawingContext context, Geometry figure)
    {
        var style = Component.Class.Has(P.FillStyle) ? Component.Get(P.FillStyle) : FillStyle.Transparent;
        if (style == FillStyle.Transparent)
            return;
        var brush = FormPalette.ToBrush(Component.Get(P.FillColor));
        if (style == FillStyle.Solid)
        {
            context.DrawGeometry(brush, null, figure);
            return;
        }
        using var clip = context.PushGeometryClip(figure);
        GdiDrawing.Hatch(context, brush, style, Math.Max(inside.Width, bitmap?.PixelSize.Width ?? 0),
            Math.Max(inside.Height, bitmap?.PixelSize.Height ?? 0));
    }

    // Draws on the bitmap, grown first to the inside where the inside is larger.
    private void Draw(Action<DrawingContext> draw)
    {
        var size = new PixelSize(Math.Max(1, Math.Max(inside.Width, bitmap?.PixelSize.Width ?? 0)),
            Math.Max(1, Math.Max(inside.Height, bitmap?.PixelSize.Height ?? 0)));
        if (bitmap is null || bitmap.PixelSize != size)
        {
            var grown = new RenderTargetBitmap(size, new Vector(96, 96));
            using (var copy = grown.CreateDrawingContext(true))
                if (bitmap is not null)
                    copy.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height));
            bitmap?.Dispose();
            bitmap = grown;
        }
        using (var context = bitmap.CreateDrawingContext(false))
        using (context.PushRenderOptions(new RenderOptions { EdgeMode = EdgeMode.Aliased }))
            draw(context);
        readBack?.Dispose();
        readBack = null;
        InvalidateVisual();
    }

    // The colour drawn at a pixel, transparent where nothing is.
    private Color? Read(int x, int y)
    {
        if (bitmap is null || x >= bitmap.PixelSize.Width || y >= bitmap.PixelSize.Height)
            return null;
        if (readBack is null)
        {
            readBack = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
            using var target = readBack.Lock();
            bitmap.CopyPixels(target);
        }
        using var buffer = readBack.Lock();
        var value = Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
        var (b, g, r, a) = ((byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24));
        if (buffer.Format == PixelFormats.Rgba8888)
            (r, b) = (b, r);
        // A pixel the bitmap holds premultiplied, with no smoothing, is opaque or empty; one in between is undone to its colour.
        return a is 0 or 255 ? Color.FromArgb(a, r, g, b)
            : Color.FromArgb(a, (byte)(r * 255 / a), (byte)(g * 255 / a), (byte)(b * 255 / a));
    }
}
