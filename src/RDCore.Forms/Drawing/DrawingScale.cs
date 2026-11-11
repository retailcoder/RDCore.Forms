using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Drawing;

/// <summary>
/// A container's scale: how many twips one of its units is, across and down, and the coordinates of its inside's top left corner.
/// </summary>
/// <param name="TwipsAcross">The twips in one horizontal unit; negative where the scale runs right to left.</param>
/// <param name="TwipsDown">The twips in one vertical unit; negative where the scale runs bottom to top.</param>
/// <param name="Left">The coordinate of the inside's left edge, the container's <c>ScaleLeft</c>.</param>
/// <param name="Top">The coordinate of the inside's top edge, the container's <c>ScaleTop</c>.</param>
public readonly record struct DrawingScale(double TwipsAcross, double TwipsDown, double Left, double Top)
{
    /// <summary>
    /// Twips from the inside's corner: the scale of a container that has none, such as a Frame.
    /// </summary>
    public static DrawingScale Twips => new(1, 1, 0, 0);

    /// <summary>
    /// The scale of a container: its <c>ScaleMode</c>, <c>ScaleLeft</c> and <c>ScaleTop</c>, and for a user scale its
    /// <c>ScaleWidth</c> and <c>ScaleHeight</c> over its inside, in single precision as VB holds a scale.
    /// </summary>
    /// <param name="container">The container, or <see langword="null"/> for none.</param>
    /// <param name="insideWidth">The container's inside width, in twips: within its border, if it has one.</param>
    /// <param name="insideHeight">The container's inside height, in twips.</param>
    /// <returns>
    /// The scale. It is <see cref="Twips"/> for no container, for a container with no <c>ScaleMode</c> such as a Frame, and for a
    /// user scale whose <c>ScaleWidth</c> or <c>ScaleHeight</c> is 0.
    /// </returns>
    public static DrawingScale Of(ComponentInstance? container, double insideWidth, double insideHeight)
    {
        if (container is null || !container.Class.Has(P.ScaleMode))
            return Twips;
        double left = container.Get(P.ScaleLeft), top = container.Get(P.ScaleTop);
        var mode = container.Get(P.ScaleMode);
        if (mode != ScaleMode.User)
        {
            var (across, down) = mode.TwipsPerUnit();
            return new(across, down, left, top);
        }
        double scaleWidth = container.Get(P.ScaleWidth), scaleHeight = container.Get(P.ScaleHeight);
        if (scaleWidth == 0 || scaleHeight == 0)
            return Twips;
        return new((float)insideWidth / (float)scaleWidth, (float)insideHeight / (float)scaleHeight, left, top);
    }

    /// <summary>
    /// The scale of a container, over its inside as <see cref="InsideOf"/> measures it.
    /// </summary>
    /// <param name="container">The container, or <see langword="null"/> for none.</param>
    /// <returns>The scale, as <see cref="Of(ComponentInstance?, double, double)"/> gives it.</returns>
    public static DrawingScale Of(ComponentInstance? container)
    {
        if (container is null)
            return Twips;
        var (width, height) = InsideOf(container);
        return Of(container, width, height);
    }

    /// <summary>
    /// A component's inside, in twips: its <c>Width</c> and <c>Height</c>, within its border where its <c>BorderStyle</c> draws one. A root's
    /// <c>Width</c> and <c>Height</c> are its inside already.
    /// </summary>
    /// <param name="component">The component.</param>
    /// <returns>The inside's width and height in twips, 0 where the component has no size.</returns>
    /// <remarks>
    /// A border is a pixel wide where the component's <c>Appearance</c> is Flat, and two pixels wide where it is 3D.
    /// </remarks>
    public static (double Width, double Height) InsideOf(ComponentInstance component)
    {
        ArgumentNullException.ThrowIfNull(component);
        double width = component.Class.Has(P.Width) ? component.Get(P.Width) : 0, height = component.Class.Has(P.Height) ? component.Get(P.Height) : 0;
        if (component.Class.Kind != ComponentKind.Root && component.Class.Has(P.Appearance) && component.Class.Has(P.BorderStyle)
            && component.Get(P.BorderStyle) != Model.VB.BorderStyle.None)
        {
            var border = component.Get(P.Appearance) == Model.VB.Appearance.Flat ? 2 : 4;
            (width, height) = (Math.Max(0, width - border), Math.Max(0, height - border));
        }
        return (width * ScaleModeExtensions.TwipsPerPixel, height * ScaleModeExtensions.TwipsPerPixel);
    }

    /// <summary>
    /// A point in the scale as twips from the inside's corner.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    /// <returns>The twips across and down.</returns>
    public (double X, double Y) ToTwips(double x, double y) => ((x - Left) * TwipsAcross, (y - Top) * TwipsDown);

    /// <summary>
    /// Twips from the inside's corner as a point in the scale.
    /// </summary>
    /// <param name="x">The twips across.</param>
    /// <param name="y">The twips down.</param>
    /// <returns>The coordinates.</returns>
    public (double X, double Y) FromTwips(double x, double y) => (x / TwipsAcross + Left, y / TwipsDown + Top);
}
