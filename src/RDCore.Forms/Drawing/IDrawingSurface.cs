using RDCore.Forms.Model;

namespace RDCore.Forms.Drawing;

/// <summary>
/// What a form or a PictureBox draws on: the destination of VB's drawing statements (<c>Line</c>, <c>Circle</c>, <c>PSet</c> and
/// <c>Scale</c>) and of its <c>Cls</c> and <c>Point</c> methods.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates are in the component's scale, its <c>ScaleMode</c> with its <c>ScaleLeft</c> and <c>ScaleTop</c>, as
/// <see cref="DrawingScale"/> converts them. A colour that is <see langword="null"/> is the component's <c>ForeColor</c>. Lines are
/// drawn <c>DrawWidth</c> pixels wide in its <c>DrawStyle</c>, and figures are filled with its <c>FillColor</c> in its
/// <c>FillStyle</c>. Its <c>DrawMode</c> combines each pixel of a line and of a fill with the pixel under it.
/// </para>
/// <para>
/// What is drawn is kept, and shows under the component's controls. Each drawing statement moves <see cref="CurrentX"/> and
/// <see cref="CurrentY"/> to where it ends, and a <see cref="GraphicsPoint"/> that is relative is measured from there.
/// </para>
/// </remarks>
public interface IDrawingSurface
{
    /// <summary>
    /// The form's root or the PictureBox the surface draws for.
    /// </summary>
    ComponentInstance Component { get; }

    /// <summary>
    /// The horizontal position the next drawing starts from, in the component's scale. A new surface starts at 0.
    /// </summary>
    double CurrentX { get; set; }

    /// <summary>
    /// The vertical position the next drawing starts from, in the component's scale. A new surface starts at 0.
    /// </summary>
    double CurrentY { get; set; }

    /// <summary>
    /// Draws a line or a box: VB's <c>Line</c> statement.
    /// </summary>
    /// <param name="from">The first point, or <see langword="null"/> to start at <see cref="CurrentX"/> and <see cref="CurrentY"/>.</param>
    /// <param name="to">The second point. A relative one is measured from the first point.</param>
    /// <param name="color">The colour, or <see langword="null"/> for the component's <c>ForeColor</c>.</param>
    /// <param name="box">
    /// Whether to draw the line, the box whose corners are the two points (filled as the component's <c>FillStyle</c> says), or the box
    /// filled with <paramref name="color"/>.
    /// </param>
    void Line(GraphicsPoint? from, GraphicsPoint to, FormColor? color = null, LineBox box = LineBox.None);

    /// <summary>
    /// Draws a circle, an ellipse, an arc or a sector: VB's <c>Circle</c> statement.
    /// </summary>
    /// <param name="centre">The centre.</param>
    /// <param name="radius">The radius, in the component's horizontal units.</param>
    /// <param name="color">The colour, or <see langword="null"/> for the component's <c>ForeColor</c>.</param>
    /// <param name="start">
    /// Where an arc starts, in radians counterclockwise from three o'clock, or <see langword="null"/> for 0. A negative angle also
    /// draws a radius to that end; with both negative, the sector is closed and filled.
    /// </param>
    /// <param name="end">Where an arc ends, as <paramref name="start"/>, or <see langword="null"/> for 2π.</param>
    /// <param name="aspect">
    /// Its height over its width, or <see langword="null"/> for 1, a circle. Below 1 the radius is the horizontal one, and above 1 the
    /// vertical one.
    /// </param>
    /// <exception cref="FormErrorException">
    /// Error 5 where <paramref name="start"/> or <paramref name="end"/> is outside -2π to 2π.
    /// </exception>
    void Circle(GraphicsPoint centre, double radius, FormColor? color = null, double? start = null, double? end = null, double? aspect = null);

    /// <summary>
    /// Draws a point: VB's <c>PSet</c> statement. A <c>DrawWidth</c> above 1 draws a dot that wide, centred on the point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="color">The colour, or <see langword="null"/> for the component's <c>ForeColor</c>.</param>
    void PSet(GraphicsPoint point, FormColor? color = null);

    /// <summary>
    /// Sets the component's scale back to twips from its inside corner: VB's <c>Scale</c> statement with no coordinates.
    /// </summary>
    void Scale();

    /// <summary>
    /// Sets a user scale on the component: VB's <c>Scale (x1, y1)-(x2, y2)</c>. The first point is the inside's top left corner and
    /// the second its bottom right one.
    /// </summary>
    /// <param name="x1">The left edge's coordinate.</param>
    /// <param name="y1">The top edge's coordinate.</param>
    /// <param name="x2">The right edge's coordinate.</param>
    /// <param name="y2">The bottom edge's coordinate.</param>
    void Scale(double x1, double y1, double x2, double y2);

    /// <summary>
    /// Clears what was drawn, and sets <see cref="CurrentX"/> and <see cref="CurrentY"/> to 0: VB's <c>Cls</c> method.
    /// </summary>
    void Cls();

    /// <summary>
    /// The colour at a point: VB's <c>Point</c> method.
    /// </summary>
    /// <param name="x">The point's horizontal coordinate, in the component's scale.</param>
    /// <param name="y">The point's vertical coordinate, in the component's scale.</param>
    /// <returns>
    /// The RGB colour drawn there, or else the pixel of the component's <c>Picture</c> there, or else its <c>BackColor</c>, as a VB
    /// colour (red in the low byte); -1 where the point is outside the component's inside.
    /// </returns>
    int Point(double x, double y);
}
