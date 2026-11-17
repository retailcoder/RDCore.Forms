using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// PaintPicture as measured with VB6 on a grey (&H808080) PictureBox of 100 by 100 pixels, in pixels unless a row says otherwise. The
// picture is 20 by 20 pixels in four quadrants: RGB(18, 52, 86) at the top left (563412 as a VB colour), RGB(200, 100, 50) at the top
// right (3264C8), RGB(255, 255, 0) at the bottom left (FFFF) and RGB(0, 128, 255) at the bottom right (FF8000). A row is the error
// number, then what Point reads at each of its points, as hex.
[TestClass]
public sealed class PaintPictureTests
{
    private static readonly FormColor Grey = FormColor.FromRgb(0x80, 0x80, 0x80), Pen = FormColor.FromRgb(0x50, 0xA0, 0xF0),
        Yellow = FormColor.FromRgb(0xFF, 0xFF, 0x00);

    private static readonly byte[] Quadrants =
        Png(20, 20, [.. Enumerable.Range(0, 400).Select(i => (i / 20 < 10, i % 20 < 10) switch
        {
            (true, true) => 0xFF123456u,
            (true, false) => 0xFFC86432u,
            (false, true) => 0xFFFFFF00u,
            _ => 0xFF0080FFu,
        })]);

    private static ComponentInstance Box() =>
        new ComponentInstance(ComponentClasses.PictureBox, "Picture1").Set(P.Width, 100d).Set(P.Height, 100d).Set(P.BorderStyle, BorderStyle.None)
            .Set(P.ScaleMode, ScaleMode.Pixel).Set(P.BackColor, Grey);

    private delegate void Painting(IDrawingSurface surface, byte[] picture);

    // Each row on a cleared surface: its label, the error its painting raised (0 for none), and Point at each of its points.
    private static List<string> Rows(RDFormsDrawingLayer surface, params (string Label, Painting Paint, (double X, double Y)[] Points)[] rows)
    {
        var lines = new List<string>();
        foreach (var (label, paint, points) in rows)
        {
            surface.Cls();
            var error = 0;
            try
            {
                paint(surface, Quadrants);
            }
            catch (FormErrorException e)
            {
                error = e.Error.Number;
            }
            lines.Add(string.Join("\t", [label, error.ToString(), .. points.Select(p => surface.Point(p.X, p.Y).ToString("X"))]));
        }
        return lines;
    }

    [TestMethod]
    public void TheDestination_IsThePicturesSize_OrStretched_AndANegativeSizeMirrorsIt()
    {
        var rows = UI.Run(() => Rows(new RDFormsDrawingLayer(Box()),
            ("at 30,30", (s, p) => s.PaintPicture(p, 30, 30), [(32, 32), (45, 32), (32, 45), (45, 45), (50, 50), (29, 29)]),
            ("edges", (s, p) => s.PaintPicture(p, 30, 30), [(30, 30), (49, 49), (50, 49), (49, 50)]),
            ("40 x 40", (s, p) => s.PaintPicture(p, 30, 30, 40, 40), [(35, 35), (55, 35), (35, 55), (55, 55), (69, 69), (70, 70)]),
            ("Width1 alone", (s, p) => s.PaintPicture(p, 30, 30, 40), [(35, 35), (55, 35), (35, 45), (55, 45), (35, 50), (70, 35)]),
            ("Height1 alone", (s, p) => s.PaintPicture(p, 30, 30, null, 40), [(35, 35), (45, 35), (35, 55), (45, 55), (50, 35), (35, 70)]),
            ("10 x 10", (s, p) => s.PaintPicture(p, 30, 30, 10, 10), [(31, 31), (37, 31), (31, 37), (37, 37), (40, 40)]),
            ("Width1 -20", (s, p) => s.PaintPicture(p, 50, 30, -20, 20),
                [(29, 32), (30, 32), (31, 32), (32, 32), (47, 32), (49, 32), (50, 32), (51, 32), (32, 47), (47, 47)]),
            ("Height1 -20", (s, p) => s.PaintPicture(p, 30, 50, 20, -20), [(32, 32), (47, 32), (32, 47), (47, 47), (32, 50), (32, 29)]),
            ("at 30.6,30.4", (s, p) => s.PaintPicture(p, 30.6, 30.4),
                [(30, 30), (31, 30), (30, 31), (31, 31), (49, 32), (50, 32), (51, 32), (32, 49), (32, 50)]),
            ("at -10,-10", (s, p) => s.PaintPicture(p, -10, -10), [(0, 0), (9, 9), (10, 10)]),
            ("at 90,90", (s, p) => s.PaintPicture(p, 90, 90), [(91, 91), (99, 99)]),
            ("3 across", (s, p) => s.PaintPicture(p, 30, 30, 3, 20), [(30, 32), (31, 32), (32, 32)]),
            ("2 across", (s, p) => s.PaintPicture(p, 30, 30, 2, 20), [(30, 32), (31, 32)]),
            ("1 across", (s, p) => s.PaintPicture(p, 30, 30, 1, 20), [(30, 32), (30, 45)]),
            ("30 across", (s, p) => s.PaintPicture(p, 30, 30, 30, 20), [(44, 32), (45, 32), (46, 32)]),
            ("50 across", (s, p) => s.PaintPicture(p, 30, 30, 50, 20), [(54, 32), (55, 32), (56, 32)])));

        Expect.Lines(
        [
            "at 30,30\t0\t563412\t3264C8\tFFFF\tFF8000\t808080\t808080",
            "edges\t0\t563412\tFF8000\t808080\t808080",
            "40 x 40\t0\t563412\t3264C8\tFFFF\tFF8000\tFF8000\t808080",
            "Width1 alone\t0\t563412\t3264C8\tFFFF\tFF8000\t808080\t808080",
            "Height1 alone\t0\t563412\t3264C8\tFFFF\tFF8000\t808080\t808080",
            "10 x 10\t0\t563412\t3264C8\tFFFF\tFF8000\t808080",
            "Width1 -20\t0\t808080\t808080\t3264C8\t3264C8\t563412\t563412\t563412\t808080\tFF8000\tFFFF",
            "Height1 -20\t0\tFFFF\tFF8000\t563412\t3264C8\t563412\t808080",
            "at 30.6,30.4\t0\t808080\t563412\t808080\t563412\t3264C8\t3264C8\t808080\tFFFF\t808080",
            "at -10,-10\t0\tFF8000\tFF8000\t808080",
            "at 90,90\t0\t563412\t563412",
            "3 across\t0\t563412\t3264C8\t3264C8",
            "2 across\t0\t563412\t3264C8",
            "1 across\t0\t3264C8\tFF8000",
            "30 across\t0\t563412\t3264C8\t3264C8",
            "50 across\t0\t563412\t3264C8\t3264C8",
        ], rows);
    }

    [TestMethod]
    public void TheSource_DefaultsToTheRestOfThePicture_AndIsCutToIt()
    {
        var rows = UI.Run(() => Rows(new RDFormsDrawingLayer(Box()),
            ("q2", (s, p) => s.PaintPicture(p, 30, 30, 10, 10, 10, 0, 10, 10), [(32, 32), (38, 38), (40, 40)]),
            ("from 10,10", (s, p) => s.PaintPicture(p, 30, 30, null, null, 10, 10), [(32, 32), (38, 38), (41, 41), (45, 45), (50, 50)]),
            ("from 10,10 to 40", (s, p) => s.PaintPicture(p, 30, 30, 40, 40, 10, 10), [(35, 35), (45, 45), (55, 55), (65, 65)]),
            ("from 10,10, 5 x 5", (s, p) => s.PaintPicture(p, 30, 30, null, null, 10, 10, 5, 5), [(31, 31), (34, 34), (36, 36), (41, 41), (49, 49)]),
            ("q1 to 20", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, 10, 10), [(32, 32), (48, 48), (50, 50)]),
            ("Width2 alone", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, 10), [(32, 32), (48, 32), (32, 48), (48, 48)]),
            ("Height2 alone", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, null, 10), [(32, 32), (32, 48)]),
            ("Y2 alone", (s, p) => s.PaintPicture(p, 30, 30, null, null, null, 10), [(32, 32), (32, 40), (32, 45)]),
            ("Width2 -10 from 10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 10, 0, -10, 20), [(30, 32), (31, 32), (48, 32), (49, 32)]),
            ("Width2 -10 from 15", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 15, 0, -10, 20), [(30, 32), (39, 32), (40, 32), (49, 32)]),
            ("both mirrored", (s, p) => s.PaintPicture(p, 50, 30, -20, 20, 20, 0, -20, 20), [(32, 32), (47, 32)]),
            ("19 wide 10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 19, 0, 10, 10), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("-5 wide 10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, -5, 0, 10, 10), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("-4 wide 5", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, -4, 0, 5, 20), [(30, 32), (45, 32), (46, 32), (49, 32)]),
            ("down from 19", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 19, 10, 10), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("40 x 40", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, 40, 40), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("-10,-10 40 x 40", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, -10, -10, 40, 40), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("past the right edge", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 25, 0, 5, 5), [(30, 30), (32, 32)])));

        Expect.Lines(
        [
            "q2\t0\t3264C8\t3264C8\t808080",
            "from 10,10\t0\tFF8000\tFF8000\t808080\t808080\t808080",
            "from 10,10 to 40\t0\tFF8000\tFF8000\tFF8000\tFF8000",
            "from 10,10, 5 x 5\t0\tFF8000\tFF8000\t808080\t808080\t808080",
            "q1 to 20\t0\t563412\t563412\t808080",
            "Width2 alone\t0\t563412\t563412\tFFFF\tFFFF",
            "Height2 alone\t0\t563412\t563412",
            "Y2 alone\t0\tFFFF\t808080\t808080",
            "Width2 -10 from 10\t0\t3264C8\t3264C8\t563412\t563412",
            "Width2 -10 from 15\t0\t3264C8\t3264C8\t3264C8\t563412",
            "both mirrored\t0\t563412\t3264C8",
            "19 wide 10\t0\t3264C8\t808080\t808080\t808080",
            "-5 wide 10\t0\t808080\t808080\t563412\t563412",
            "-4 wide 5\t0\t808080\t808080\t563412\t563412",
            "down from 19\t0\tFFFF\t808080\t808080\t808080",
            "40 x 40\t0\t563412\t563412\t808080\t808080",
            "-10,-10 40 x 40\t0\t808080\t808080\tFF8000\t808080",
            "past the right edge\t0\t808080\t808080",
        ], rows);
    }

    // VB6 draws these two a pixel or two off: a mirrored source that reaches past the picture's edge.
    [TestMethod]
    [Ignore("A mirrored source past the picture's edge: VB6 cuts the destination on another side than the one the source is cut on, by a rule that is not measured.")]
    public void AMirroredSourcePastThePicturesEdge_IsCutAsVB6CutsIt()
    {
        var rows = UI.Run(() => Rows(new RDFormsDrawingLayer(Box()),
            ("from 20 -20", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 20, 0, -20, 20), [(30, 32), (31, 32), (48, 32), (49, 32)]),
            ("from 25 -10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 25, 0, -10, 10), [(30, 30), (32, 32), (40, 40), (48, 48)]),
            ("from 5 -10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 5, 0, -10, 20), [(30, 32), (39, 32), (40, 32), (49, 32)])));

        Expect.Lines(
        [
            "from 20 -20\t0\t3264C8\t3264C8\t563412\t808080",
            "from 25 -10\t0\t808080\t808080\t3264C8\t808080",
            "from 5 -10\t0\t563412\t563412\t808080\t808080",
        ], rows);
    }

    [TestMethod]
    public void AZeroSize_ASourceBeforeThePictureOrAtItsFarEdge_IsError5_AndAPixelPastALong_6()
    {
        var rows = UI.Run(() => Rows(new RDFormsDrawingLayer(Box()),
            ("Width1 0", (s, p) => s.PaintPicture(p, 30, 30, 0, 20), [(30, 32)]),
            ("Width1 0.4", (s, p) => s.PaintPicture(p, 30, 30, 0.4, 20), [(30, 32)]),
            ("Width1 -0.4", (s, p) => s.PaintPicture(p, 30, 30, -0.4, 20), [(30, 32)]),
            ("Width1 1", (s, p) => s.PaintPicture(p, 30, 30, 1, 20), [(30, 32), (31, 32)]),
            ("Width2 0", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, 0, 20), [(30, 32)]),
            ("Width2 0.4", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 0, 0.4, 20), [(30, 32)]),
            ("at 20", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 20, 0, 5, 5), [(30, 30)]),
            ("before", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, -10, 0, 5, 5), [(30, 30)]),
            ("ending at 0", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, -5, 0, 5, 20), [(30, 32)]),
            ("down at 20", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, 20, 5, 5), [(30, 30)]),
            ("above", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 0, -10, 5, 5), [(30, 30)]),
            ("mirrored from 30", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 30, 0, -10, 10), [(30, 30)]),
            ("X2 alone 20", (s, p) => s.PaintPicture(p, 30, 30, null, null, 20), [(32, 32)]),
            ("X2 alone 25", (s, p) => s.PaintPicture(p, 30, 30, null, null, 25), [(32, 32)]),
            ("Width1 0 past the picture", (s, p) => s.PaintPicture(p, 30, 30, 0, 20, 25, 0, 5, 5), [(30, 32)]),
            ("X1 1E10", (s, p) => s.PaintPicture(p, 1e10, 30), [(30, 32)]),
            ("X1 1E5", (s, p) => s.PaintPicture(p, 1e5, 30), [(30, 32)]),
            ("Width1 40000", (s, p) => s.PaintPicture(p, 30, 30, 40000, 20), [(30, 32)]),
            ("X2 1E10", (s, p) => s.PaintPicture(p, 30, 30, 20, 20, 1e10, 0, 5, 5), [(30, 32)]),
            ("no bytes", (s, p) => s.PaintPicture([], 30, 30), [(30, 32)]),
            ("not a picture", (s, p) => s.PaintPicture([1, 2, 3, 4], 30, 30), [(30, 32)])));

        Expect.Lines(
        [
            "Width1 0\t5\t808080",
            "Width1 0.4\t5\t808080",
            "Width1 -0.4\t5\t808080",
            "Width1 1\t0\t3264C8\t808080",
            "Width2 0\t5\t808080",
            "Width2 0.4\t5\t808080",
            "at 20\t5\t808080",
            "before\t5\t808080",
            "ending at 0\t5\t808080",
            "down at 20\t5\t808080",
            "above\t5\t808080",
            "mirrored from 30\t5\t808080",
            "X2 alone 20\t5\t808080",
            "X2 alone 25\t5\t808080",
            "Width1 0 past the picture\t5\t808080",
            "X1 1E10\t6\t808080",
            "X1 1E5\t0\t808080",
            "Width1 40000\t0\t563412",
            "X2 1E10\t6\t808080",
            "no bytes\t481\t808080",
            "not a picture\t481\t808080",
        ], rows);
    }

    // GDI's ternary raster operation of the opcode's third byte; the low word plays no part.
    [TestMethod]
    [DataRow(0xCC0020, "563412\tFF8000")]
    [DataRow(0x8800C6, "0\t808000")]
    [DataRow(0x660046, "D6B492\t7F0080")]
    [DataRow(0xEE0086, "D6B492\tFF8080")]
    [DataRow(0x440328, "563412\t7F0000")]
    [DataRow(0x330008, "A9CBED\t7FFF")]
    [DataRow(0x1100A6, "294B6D\t7F7F")]
    [DataRow(0xBB0226, "A9CBED\t80FFFF")]
    [DataRow(0x550009, "7F7F7F\t7F7F7F")]
    [DataRow(1, "0\t0")]
    [DataRow(16, "0\t0")]
    [DataRow(0x123, "0\t0")]
    [DataRow(-1, "FFFFFF\tFFFFFF")]
    [DataRow(0xC000CA, "808080\t808080")]
    [DataRow(0xF00021, "808080\t808080")]
    [DataRow(0xFB0A09, "808080\t808080")]
    [DataRow(0x5A0049, "808080\t808080")]
    [DataRow(0xE20746, "808080\t808080")]
    public void TheOpcode_CombinesThePatternTheSourceAndWhatIsThere_AndOneThatReadsThePattern_DrawsNothingWithNoFill(int opcode, string expected)
    {
        var points = UI.Run(() => Rows(new RDFormsDrawingLayer(Box()),
            ("q1 q4 back", (s, p) => s.PaintPicture(p, 30, 30, opcode: opcode), [(32, 32), (45, 45), (50, 50)]))[0]);

        Assert.AreEqual($"q1 q4 back\t0\t{expected}\t808080", points);
    }

    [TestMethod]
    [DataRow(0xC000CA, "3412\t8000")]
    [DataRow(0x5A0049, "807F7F\t807F7F")]
    [DataRow(0xFB0A09, "A9FFFF\t80FFFF")]
    [DataRow(0xB8074A, "CBED\t80FFFF")]
    [DataRow(0xE20746, "80B492\t8080")]
    [DataRow(0xF00021, "FFFF\tFFFF")]
    [DataRow(0xCC0020, "563412\tFF8000")]
    public void ThePattern_IsASolidFillsColour(int opcode, string expected)
    {
        var points = UI.Run(() =>
        {
            var box = Box().Set(P.FillStyle, FillStyle.Solid).Set(P.FillColor, Yellow);
            return Rows(new RDFormsDrawingLayer(box), ("q1 q4", (s, p) => s.PaintPicture(p, 30, 30, opcode: opcode), [(32, 32), (45, 45)]))[0];
        });

        Assert.AreEqual($"q1 q4\t0\t{expected}", points);
    }

    // A hatched fill's lines over the BackColor, whatever was there, on rows 3 and columns 4 from the surface's corner, eight apart.
    [TestMethod]
    public void AHatchedPattern_IsItsLinesOverTheBackColour_InTheSurfacesPhase()
    {
        static (double, double)[] Down(int x, int from) => [.. Enumerable.Range(from, 10).Select(y => ((double)x, (double)y))];
        static (double, double)[] Across(int y, int from) => [.. Enumerable.Range(from, 10).Select(x => ((double)x, (double)y))];
        var rows = UI.Run(() =>
        {
            var box = Box().Set(P.FillColor, Yellow);
            var surface = new RDFormsDrawingLayer(box);
            var rows = new List<string>();
            foreach (var style in new[] { FillStyle.HorizontalLine, FillStyle.VerticalLine, FillStyle.Cross })
            {
                box.Set(P.FillStyle, style);
                rows.AddRange(Rows(surface,
                    ($"{style} down", (s, p) => s.PaintPicture(p, 30, 30, opcode: 0xF00021), Down(32, 30)),
                    ($"{style} across", (s, p) => s.PaintPicture(p, 30, 30, opcode: 0xF00021), Across(32, 30))));
            }
            box.Set(P.FillStyle, FillStyle.HorizontalLine);
            rows.AddRange(Rows(surface,
                ("over a point", (s, p) => { s.PSet(new GraphicsPoint(2, 34), Pen); s.PaintPicture(p, 0, 30, 20, 10, opcode: 0xF00021); },
                    [(2, 33), (2, 34), (2, 35)]),
                ("vbMergeCopy", (s, p) => s.PaintPicture(p, 0, 30, opcode: 0xC000CA), Down(2, 30)),
                ("vbPatInvert", (s, p) => s.PaintPicture(p, 0, 30, opcode: 0x5A0049), Down(2, 30)),
                ("from the corner", (s, p) => s.PaintPicture(p, 0, 0, opcode: 0xF00021), Down(2, 0))));
            box.Set(P.FillStyle, FillStyle.VerticalLine);
            rows.AddRange(Rows(surface, ("vertical from the corner", (s, p) => s.PaintPicture(p, 0, 0, opcode: 0xF00021), Across(2, 0))));
            return rows;
        });

        const string G = "808080", Y = "FFFF";
        static string Line(params string[] values) => string.Join("\t", values);
        Expect.Lines(
        [
            Line("HorizontalLine down", "0", G, G, G, G, G, Y, G, G, G, G),
            Line("HorizontalLine across", "0", G, G, G, G, G, G, G, G, G, G),
            Line("VerticalLine down", "0", G, G, G, G, G, G, G, G, G, G),
            Line("VerticalLine across", "0", G, G, G, G, G, G, Y, G, G, G),
            Line("Cross down", "0", G, G, G, G, G, Y, G, G, G, G),
            Line("Cross across", "0", G, G, G, G, G, G, Y, G, G, G),
            Line("over a point", "0", G, G, Y),
            Line("vbMergeCopy", "0", "0", "0", "0", "0", "0", "3412", "0", "0", "0", "0"),
            Line("vbPatInvert", "0", "0", "0", "0", "0", "0", "807F7F", "0", "0", "0", "0"),
            Line("from the corner", "0", G, G, G, Y, G, G, G, G, G, G),
            Line("vertical from the corner", "0", G, G, G, G, Y, G, G, G, G, G),
        ], rows);
    }

    [TestMethod]
    public void WhatIsThere_IsTheDrawingOrThePicture_TheDrawModePlaysNoPart_AndTheCurrentPointIsKept()
    {
        var rows = UI.Run(() =>
        {
            var box = Box();
            var surface = new RDFormsDrawingLayer(box);
            var rows = Rows(surface,
                ("vbSrcInvert twice", (s, p) => { s.PaintPicture(p, 30, 30, opcode: 0x660046); s.PaintPicture(p, 30, 30, opcode: 0x660046); },
                    [(32, 32), (50, 50)]),
                ("vbSrcAnd over a point", (s, p) => { s.PSet(new GraphicsPoint(32, 32), Pen); s.PaintPicture(p, 30, 30, opcode: 0x8800C6); },
                    [(32, 32)]),
                ("DrawMode 7", (s, p) => { box.Set(P.DrawMode, (short)7); s.PaintPicture(p, 30, 30); box.Set(P.DrawMode, (short)13); },
                    [(32, 32)]),
                ("Cls after", (s, p) => { s.PaintPicture(p, 30, 30); s.Cls(); }, [(32, 32)]));
            surface.CurrentX = 7;
            surface.CurrentY = 9;
            surface.PaintPicture(Quadrants, 30, 30);
            rows.Add($"current point\t{surface.CurrentX}\t{surface.CurrentY}");
            var pictured = new RDFormsDrawingLayer(Box()) { Picture = RDFormsPicture.Decode(Quadrants, out _) };
            rows.AddRange(Rows(pictured, ("over the Picture", (s, p) => s.PaintPicture(p, 5, 5, opcode: 0x660046), [(2, 2), (7, 7), (25, 25)])));
            return rows;
        });

        Expect.Lines(
        [
            "vbSrcInvert twice\t0\t808080\t808080",
            "vbSrcAnd over a point\t0\t502010",
            "DrawMode 7\t0\t563412",
            "Cls after\t0\t808080",
            "current point\t7\t9",
            "over the Picture\t0\t563412\t0\t808080",
        ], rows);
    }

    // Every coordinate and size, the source's too, is in the scale; the picture's own size is its size at 96 pixels an inch.
    [TestMethod]
    public void TheSourceToo_IsInTheSurfacesScale()
    {
        var rows = UI.Run(() =>
        {
            var user = new RDFormsDrawingLayer(Box());
            user.Scale(0, 0, 50, 50);
            var rows = Rows(user,
                ("user at 10,10", (s, p) => s.PaintPicture(p, 10, 10), [(11, 11), (17, 11), (11, 17), (17, 17), (20.5, 20.5)]),
                ("user 20 x 20", (s, p) => s.PaintPicture(p, 10, 10, 20, 20), [(12, 12), (27, 27), (30.5, 30.5)]),
                ("user source 5,5", (s, p) => s.PaintPicture(p, 10, 10, 5, 5, 5, 5, 5, 5), [(11, 11), (14, 14), (15.5, 15.5)]),
                ("user source 10,0", (s, p) => s.PaintPicture(p, 10, 10, 5, 5, 10, 0, 10, 10), [(11, 11)]));
            var twips = new RDFormsDrawingLayer(Box());
            twips.Scale();
            rows.AddRange(Rows(twips,
                ("twips at 300,300", (s, p) => s.PaintPicture(p, 300, 300), [(330, 330), (500, 330), (330, 500), (500, 500), (615, 615)]),
                ("twips source 150,0", (s, p) => s.PaintPicture(p, 300, 300, 150, 150, 150, 0, 150, 150), [(330, 330), (430, 430), (460, 460)]),
                ("twips source 142", (s, p) => s.PaintPicture(p, 0, 0, 15, 15, 142, 0, 15, 15), [(0, 0)]),
                ("twips source 143", (s, p) => s.PaintPicture(p, 0, 0, 15, 15, 143, 0, 15, 15), [(0, 0)]),
                ("twips at 7,7", (s, p) => s.PaintPicture(p, 7, 7), [(0, 0), (15, 15)]),
                ("twips at 8,8", (s, p) => s.PaintPicture(p, 8, 8), [(0, 0), (15, 15)])));
            return rows;
        });

        Expect.Lines(
        [
            "user at 10,10\t0\t563412\t3264C8\tFFFF\tFF8000\t808080",
            "user 20 x 20\t0\t563412\tFF8000\t808080",
            "user source 5,5\t0\tFF8000\tFF8000\t808080",
            "user source 10,0\t5\t808080",
            "twips at 300,300\t0\t563412\t3264C8\tFFFF\tFF8000\t808080",
            "twips source 150,0\t0\t3264C8\t3264C8\t808080",
            "twips source 142\t0\t563412",
            "twips source 143\t0\t3264C8",
            "twips at 7,7\t0\t563412\t563412",
            "twips at 8,8\t0\t808080\t563412",
        ], rows);
    }
}
