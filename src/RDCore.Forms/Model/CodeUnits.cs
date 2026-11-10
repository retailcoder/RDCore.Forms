using RDCore.Forms.Drawing;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Model;

/// <summary>
/// The unit a program's code reads and writes a component's <c>Left</c>, <c>Top</c>, <c>Width</c> and <c>Height</c> in.
/// </summary>
public enum CodeUnit
{
    /// <summary>
    /// Pixels, as the model holds them: the unit of a form made for RDForms.
    /// </summary>
    Pixel,

    /// <summary>
    /// VB6's unit: a control's position and size are in its container's scale, and twips where the container has none (a Frame); a
    /// root's are twips.
    /// </summary>
    VB6,

    /// <summary>
    /// Points, 72 to the inch: VBA's unit.
    /// </summary>
    Point,
}

/// <summary>
/// Conversions of a component's position and size between the pixels the model holds and the unit a program's code reads.
/// </summary>
/// <remarks>
/// <para>
/// In <see cref="CodeUnit.VB6"/>, a control in a form or a PictureBox has its <c>Left</c> and <c>Top</c> as coordinates of its container's
/// scale, its <c>ScaleLeft</c> and <c>ScaleTop</c> included, and its <c>Width</c> and <c>Height</c> as lengths in the scale's units. A
/// length has no sign, even in a scale that runs right to left or bottom to top. A root's position and size are its client area's, in
/// twips. These two rules were not measured with VB6: RDForms sets them.
/// </para>
/// <para>
/// Any other property is in the same unit for code as in the model: a scale's own properties and a Line's ends are in the container's
/// scale already.
/// </para>
/// </remarks>
public static class CodeUnits
{
    private const double PointsPerPixel = 0.75;

    /// <summary>
    /// Whether a property is a position or a size the model holds in pixels: <c>Left</c>, <c>Top</c>, <c>Width</c> or <c>Height</c>.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns><see langword="true"/> for one of the four.</returns>
    public static bool IsExtent(PropertyDefinition property) =>
        ReferenceEquals(property, P.Left) || ReferenceEquals(property, P.Top) || ReferenceEquals(property, P.Width) || ReferenceEquals(property, P.Height);

    /// <summary>
    /// A position or a size in pixels, as a program's code reads it.
    /// </summary>
    /// <param name="component">The component the property is of.</param>
    /// <param name="property">The property: <c>Left</c>, <c>Top</c>, <c>Width</c> or <c>Height</c>.</param>
    /// <param name="pixels">The value, in pixels.</param>
    /// <param name="unit">The code's unit.</param>
    /// <returns>The value in the code's unit; <paramref name="pixels"/> where <paramref name="property"/> is no position or size.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
    public static double ToCode(ComponentInstance component, PropertyDefinition property, double pixels, CodeUnit unit)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        if (!IsExtent(property))
            return pixels;
        switch (unit)
        {
            case CodeUnit.Point:
                return pixels * PointsPerPixel;
            case CodeUnit.VB6:
                var twips = pixels * ScaleModeExtensions.TwipsPerPixel;
                var scale = DrawingScale.Of(component.Container);
                if (ReferenceEquals(property, P.Left))
                    return scale.FromTwips(twips, 0).X;
                if (ReferenceEquals(property, P.Top))
                    return scale.FromTwips(0, twips).Y;
                return twips / Math.Abs(ReferenceEquals(property, P.Width) ? scale.TwipsAcross : scale.TwipsDown);
            default:
                return pixels;
        }
    }

    /// <summary>
    /// A position or a size a program's code writes, in pixels.
    /// </summary>
    /// <param name="component">The component the property is of.</param>
    /// <param name="property">The property: <c>Left</c>, <c>Top</c>, <c>Width</c> or <c>Height</c>.</param>
    /// <param name="value">The value, in the code's unit.</param>
    /// <param name="unit">The code's unit.</param>
    /// <returns>The value in pixels; <paramref name="value"/> where <paramref name="property"/> is no position or size.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
    public static double FromCode(ComponentInstance component, PropertyDefinition property, double value, CodeUnit unit)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        if (!IsExtent(property))
            return value;
        switch (unit)
        {
            case CodeUnit.Point:
                return value / PointsPerPixel;
            case CodeUnit.VB6:
                var scale = DrawingScale.Of(component.Container);
                var twips = ReferenceEquals(property, P.Left) ? scale.ToTwips(value, scale.Top).X
                    : ReferenceEquals(property, P.Top) ? scale.ToTwips(scale.Left, value).Y
                    : value * Math.Abs(ReferenceEquals(property, P.Width) ? scale.TwipsAcross : scale.TwipsDown);
                return twips / ScaleModeExtensions.TwipsPerPixel;
            default:
                return value;
        }
    }
}
