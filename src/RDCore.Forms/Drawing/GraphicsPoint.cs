namespace RDCore.Forms.Drawing;

/// <summary>
/// A point of a drawing statement: <c>(x, y)</c>, or <c>Step (x, y)</c>, relative to where the drawing starts from.
/// </summary>
/// <param name="X">The horizontal coordinate, in the surface's scale.</param>
/// <param name="Y">The vertical coordinate, in the surface's scale.</param>
/// <param name="IsRelative">Whether the point is written with <c>Step</c>, as an offset rather than a position.</param>
public readonly record struct GraphicsPoint(double X, double Y, bool IsRelative = false);

/// <summary>
/// What VB's <c>Line</c> statement draws: its <c>B</c> or <c>BF</c> option.
/// </summary>
public enum LineBox
{
    /// <summary>
    /// A line, with no option.
    /// </summary>
    None = 0,
    /// <summary>
    /// A box, <c>B</c>, filled as the surface's <c>FillStyle</c> says.
    /// </summary>
    Box = 1,
    /// <summary>
    /// A box filled with the line's colour, <c>BF</c>.
    /// </summary>
    FilledBox = 2,
}
