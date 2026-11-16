using RDCore.Forms.Model;

namespace RDCore.Forms.Drawing;

/// <summary>
/// What a form or a PictureBox draws on: the destination of VB's drawing statements (<c>Line</c>, <c>Circle</c>, <c>PSet</c> and
/// <c>Scale</c>) and of its <c>Cls</c>, <c>Point</c>, <c>Print</c> and <c>PaintPicture</c> methods.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates are in the component's scale, its <c>ScaleMode</c> with its <c>ScaleLeft</c> and <c>ScaleTop</c>, as
/// <see cref="DrawingScale"/> converts them. A colour that is <see langword="null"/> is the component's <c>ForeColor</c>. Lines are
/// drawn <c>DrawWidth</c> pixels wide in its <c>DrawStyle</c>, and figures are filled with its <c>FillColor</c> in its
/// <c>FillStyle</c>. Its <c>DrawMode</c> combines each pixel of a point, a line and a fill with the pixel under it.
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
    /// The horizontal position the next drawing starts from, in the component's scale. The point is kept in whole twips from the
    /// inside's top left corner: a value set is rounded to the nearest twip, and a change of scale keeps the point where it is. A new
    /// surface starts at the corner.
    /// </summary>
    double CurrentX { get; set; }

    /// <summary>
    /// The vertical position the next drawing starts from, in the component's scale, kept as <see cref="CurrentX"/> is.
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

    /// <summary>
    /// Prints an output list at the current point: VB's <c>Print</c> method.
    /// </summary>
    /// <param name="items">The items, in order. An empty list prints nothing.</param>
    /// <param name="endsLine">
    /// Whether the list ends the line, as one with no <c>;</c> or <c>,</c> at its end does: <see cref="CurrentX"/> then goes to the
    /// scale's 0 and <see cref="CurrentY"/> down a line. A list that ends with a comma ends with a <see cref="PrintItem.Tab"/> instead.
    /// </param>
    /// <remarks>
    /// <para>
    /// The text is drawn in the component's <c>Font</c> and <c>ForeColor</c>, its top left corner at the current point, on the
    /// pixel nearest it. <c>FontTransparent</c> False fills each line's cell with the <c>BackColor</c> first. <c>DrawMode</c> does not
    /// apply to text.
    /// </para>
    /// <para>
    /// A column is the font's average character width, and a print zone 14 columns. Columns and zones count from the scale's 0, and
    /// a position is moved to the pixel nearest it. A text item moves <see cref="CurrentX"/> to its end and
    /// <see cref="CurrentY"/> to the pixel it is drawn on. <see cref="PrintItem.Spc"/> moves on its columns, to the column boundary
    /// at or after that. <see cref="PrintItem.Tab"/> with a column moves to the column's start, or to its start on the next line
    /// where the current point is past it; with none, it moves to the next zone boundary past the current point.
    /// </para>
    /// </remarks>
    void Print(IReadOnlyList<PrintItem> items, bool endsLine = true);

    /// <summary>
    /// The width of a text in the component's <c>Font</c>: VB's <c>TextWidth</c> method.
    /// </summary>
    /// <param name="text">The text. A carriage return, a line feed, or both together start a new line.</param>
    /// <returns>The width of its longest line, in the component's horizontal units; 0 for an empty text.</returns>
    double TextWidth(string text);

    /// <summary>
    /// The height of a text in the component's <c>Font</c>: VB's <c>TextHeight</c> method.
    /// </summary>
    /// <param name="text">The text. A carriage return, a line feed, or both together start a new line.</param>
    /// <returns>Its lines times the font's line height, in the component's vertical units; one line for an empty text.</returns>
    double TextHeight(string text);

    /// <summary>
    /// Draws a picture, or a rectangle of it, stretched to a rectangle of the surface: VB's <c>PaintPicture</c> method.
    /// </summary>
    /// <param name="picture">The picture, as its file holds it.</param>
    /// <param name="x1">The destination's left edge, in the component's scale.</param>
    /// <param name="y1">The destination's top edge, in the component's scale.</param>
    /// <param name="width1">The destination's width, or <see langword="null"/> for the source's. A negative width mirrors the picture across.</param>
    /// <param name="height1">The destination's height, or <see langword="null"/> for the source's. A negative height mirrors it down.</param>
    /// <param name="x2">The source's left edge within the picture, in the component's horizontal units, or <see langword="null"/> for 0.</param>
    /// <param name="y2">The source's top edge within the picture, in the component's vertical units, or <see langword="null"/> for 0.</param>
    /// <param name="width2">
    /// The source's width, or <see langword="null"/> for the rest of the picture's width from <paramref name="x2"/>. A negative width mirrors
    /// the source.
    /// </param>
    /// <param name="height2">The source's height, or <see langword="null"/> for the rest of the picture's height from <paramref name="y2"/>.</param>
    /// <param name="opcode">
    /// The raster operation: GDI's ternary raster operation of the opcode's third byte, over the pattern, the source and what shows on the
    /// surface. <c>vbSrcCopy</c> (<c>&amp;HCC0020</c>) unless said otherwise.
    /// </param>
    /// <remarks>
    /// <para>
    /// The picture's size in the scale is its size at 96 pixels an inch. Every position is placed on the pixel nearest it. The destination
    /// covers <paramref name="width1"/> pixels from <paramref name="x1"/>, rightward or, for a negative width, leftward from it; the source
    /// likewise. A destination pixel takes the source pixel under its centre. A part of the source outside the picture draws nothing, so the
    /// destination is cut in proportion; a transparent part of the picture shows what is under it.
    /// </para>
    /// <para>
    /// The pattern is the component's fill: its <c>FillColor</c> where its <c>FillStyle</c> is Solid, and its <c>FillColor</c> lines over its
    /// <c>BackColor</c> where it is hatched. Where the <c>FillStyle</c> is Transparent, an opcode that reads the pattern draws nothing. The
    /// <c>DrawMode</c>, <see cref="CurrentX"/>, <see cref="CurrentY"/> and the component's <c>Picture</c> play no part and are kept.
    /// </para>
    /// </remarks>
    /// <exception cref="FormErrorException">
    /// Error 481 where <paramref name="picture"/> is empty or is not a picture. Error 6 where a position or a size is too large for a
    /// pixel. Error 5 where a width or a height is under half a pixel, or the source lies wholly left of or above the picture, or starts at
    /// its right or bottom edge; a source that starts past that edge draws nothing.
    /// </exception>
    void PaintPicture(byte[] picture, double x1, double y1, double? width1 = null, double? height1 = null, double? x2 = null, double? y2 = null,
        double? width2 = null, double? height2 = null, int opcode = 0xCC0020);
}
