using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The DrawModes and Point over a picture, as measured with VB6, native and p-code agreeing, on a grey (&H808080) PictureBox in pixels
// whose picture is 20 by 20 pixels in four quadrants: RGB(18, 52, 86) at the top left, RGB(200, 100, 50) at the top right,
// RGB(255, 255, 0) at the bottom left and RGB(0, 128, 255) at the bottom right. The pen is &H00F0A050 and the FillColor &H0000FFFF.
// Values are VB colours, red in the low byte, as hex.
[TestClass]
public sealed class DrawModeTests
{
    private static readonly FormColor Grey = FormColor.FromRgb(0x80, 0x80, 0x80), Pen = FormColor.FromRgb(0x50, 0xA0, 0xF0),
        Yellow = FormColor.FromRgb(0xFF, 0xFF, 0x00);

    private static byte[] Quadrants() =>
        Png(20, 20, [.. Enumerable.Range(0, 400).Select(i => (i / 20 < 10, i % 20 < 10) switch
        {
            (true, true) => 0xFF123456u,
            (true, false) => 0xFFC86432u,
            (false, true) => 0xFFFFFF00u,
            _ => 0xFF0080FFu,
        })]);

    private static ComponentInstance Box(byte[]? picture = null) =>
        new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Width, 100d).Set(P.Height, 100d).Set(P.BorderStyle, BorderStyle.None)
            .Set(P.ScaleMode, ScaleMode.Pixel).Set(P.BackColor, Grey).Set(P.Picture, picture);

    private static VBDrawingLayer Surface(ComponentInstance box) => new(box) { Picture = VBPicture.Decode(Quadrants(), out _) };

    private static string Hex(IDrawingSurface surface, params (double X, double Y)[] points) =>
        string.Join("\t", points.Select(p => surface.Point(p.X, p.Y).ToString("X")));

    // For each DrawMode: PSet over the BackColor and over the top right quadrant; Line BF over the BackColor and over the bottom right
    // quadrant; a box filled with the FillColor, at its edge and inside.
    private static readonly string[] Modes =
    [
        "DrawMode=1\t0\t0\t0\t0\t0\t0",
        "DrawMode=2\tF5F2F\tD1B27\tF5F2F\t5FAF\tF5F2F\t7F0000",
        "DrawMode=3\t80\t24488\t80\tF0000\t80\t800000",
        "DrawMode=4\tF5FAF\tF5FAF\tF5FAF\tF5FAF\tF5FAF\tFF0000",
        "DrawMode=5\t702050\tC08010\t702050\t2050\t702050\t7F7F",
        "DrawMode=6\t7F7F7F\tCD9B37\t7F7F7F\t7FFF\t7F7F7F\t7F7F7F",
        "DrawMode=7\t7020D0\tC2C498\t7020D0\tF2050\t7020D0\t807F7F",
        "DrawMode=8\t7F7FFF\tCFDFBF\t7F7FFF\tF7FFF\t7F7FFF\tFF7F7F",
        "DrawMode=9\t808000\t302040\t808000\tF08000\t808000\t8080",
        "DrawMode=10\t8FDF2F\t3D3B67\t8FDF2F\tF0DFAF\t8FDF2F\t7F8080",
        "DrawMode=11\t808080\t3264C8\t808080\tFF8000\t808080\t808080",
        "DrawMode=12\t8FDFAF\t3F7FEF\t8FDFAF\tFFDFAF\t8FDFAF\tFF8080",
        "DrawMode=13\tF0A050\tF0A050\tF0A050\tF0A050\tF0A050\tFFFF",
        "DrawMode=14\tFFFF7F\tFDBB77\tFFFF7F\tF0FFFF\tFFFF7F\t7FFFFF",
        "DrawMode=15\tF0A0D0\tF2E4D8\tF0A0D0\tFFA050\tF0A0D0\t80FFFF",
        "DrawMode=16\tFFFFFF\tFFFFFF\tFFFFFF\tFFFFFF\tFFFFFF\tFFFFFF",
    ];

    [TestMethod]
    public void EachDrawMode_CombinesThePenAndTheFill_WithTheBackColourOrThePictureUnder()
    {
        var rows = UI.Run(() =>
        {
            var box = Box();
            var surface = Surface(box);
            var rows = new List<string>();
            for (short mode = 1; mode <= 16; mode++)
            {
                surface.Cls();
                box.Set(P.DrawMode, mode);
                surface.PSet(new GraphicsPoint(30, 30), Pen);
                surface.PSet(new GraphicsPoint(15, 2), Pen);
                var points = Hex(surface, (30, 30), (15, 2));
                surface.Cls();
                surface.Line(new GraphicsPoint(40, 40), new GraphicsPoint(45, 45), Pen, LineBox.FilledBox);
                surface.Line(new GraphicsPoint(12, 12), new GraphicsPoint(14, 14), Pen, LineBox.FilledBox);
                var filled = Hex(surface, (42, 42), (13, 13));
                surface.Cls();
                box.Set(P.FillStyle, FillStyle.Solid).Set(P.FillColor, Yellow);
                surface.Line(new GraphicsPoint(50, 50), new GraphicsPoint(55, 55), Pen, LineBox.Box);
                box.Set(P.FillStyle, FillStyle.Transparent);
                rows.Add($"DrawMode={mode}\t{points}\t{filled}\t{Hex(surface, (50, 50), (52, 52))}");
            }
            return rows;
        });

        Expect.Lines(Modes, rows);
    }

    // XOR Pen over a box and a circle filled with the FillColor: the fill stops where the edge starts, so the edge's pixels combine
    // with the BackColor and the fill's with it too, at a DrawWidth of 1 and of 3.
    [TestMethod]
    [DataRow(1, "808080\t808080\t808080\t7020D0\t807F7F\t807F7F\t807F7F\t807F7F")]
    [DataRow(3, "808080\t808080\t7020D0\t7020D0\t7020D0\t807F7F\t807F7F\t807F7F")]
    public void TheFill_StopsUnderTheEdge(int width, string expected)
    {
        var (box, circle) = UI.Run(() =>
        {
            var component = Box().Set(P.DrawMode, (short)7).Set(P.DrawWidth, (short)width).Set(P.FillStyle, FillStyle.Solid).Set(P.FillColor, Yellow);
            var surface = Surface(component);
            surface.Line(new GraphicsPoint(60, 10), new GraphicsPoint(75, 25), Pen, LineBox.Box);
            surface.Circle(new GraphicsPoint(70, 70), 10, Pen);
            return (Hex(surface, [.. Enumerable.Range(57, 8).Select(x => ((double)x, 17d))]),
                Hex(surface, [.. Enumerable.Range(76, 8).Reverse().Select(x => ((double)x, 70d))]));
        });

        Assert.AreEqual(expected, box);
        Assert.AreEqual(expected, circle);
    }

    [TestMethod]
    public void Point_ReadsThePictureUnderTheDrawing_TheBackColourPastIt_AndClsKeepsIt()
    {
        var (before, after) = UI.Run(() =>
        {
            var surface = Surface(Box());
            var before = Hex(surface, (2, 2), (15, 2), (2, 15), (15, 15), (30, 30), (-1, 5));
            surface.PSet(new GraphicsPoint(2, 2), Pen);
            surface.Cls();
            return (before, Hex(surface, (2, 2), (15, 15)));
        });

        Assert.AreEqual("563412\t3264C8\tFFFF\tFF8000\t808080\tFFFFFFFF", before);
        Assert.AreEqual("563412\tFF8000", after);
    }

    // RDForms' own: a transparent pixel of the picture reads as the BackColor that shows through it.
    [TestMethod]
    public void Point_ReadsANewPicture_AndTheBackColourThroughItsTransparentPixels()
    {
        var (before, after) = UI.Run(() =>
        {
            var surface = Surface(Box());
            var before = Hex(surface, (0, 0), (1, 0));
            surface.Picture = VBPicture.Decode(Png(4, 4, 0xFFFF0000u, 0u), out _);
            return (before, Hex(surface, (0, 0), (1, 0), (10, 10)));
        });

        Assert.AreEqual("563412\t563412", before);
        Assert.AreEqual("FF\t808080\t808080", after);
    }

    [TestMethod]
    public void XorPen_DrawnTwice_GivesBackWhatWasThere()
    {
        var points = UI.Run(() =>
        {
            var box = Box().Set(P.DrawMode, (short)7);
            var surface = Surface(box);
            for (var i = 0; i < 2; i++)
            {
                surface.PSet(new GraphicsPoint(30, 30), Pen);
                surface.PSet(new GraphicsPoint(2, 2), Pen);
            }
            return Hex(surface, (30, 30), (2, 2));
        });

        Assert.AreEqual("808080\t563412", points);
    }

    [TestMethod]
    [DataRow((short)0)]
    [DataRow((short)17)]
    [DataRow((short)-1)]
    public void ADrawModeOutside1To16_IsRefused_AndTheDrawModeKept(short mode)
    {
        var box = Box();

        Assert.IsFalse(box.TrySet(P.DrawMode, mode));
        Assert.AreEqual((short)13, box.Get(P.DrawMode));
        Assert.IsTrue(box.TrySet(P.DrawMode, (short)1) && box.TrySet(P.DrawMode, (short)16));
    }

    // On a shown form: a Picture set erases a form's and a PictureBox's drawing; a BackColor set erases a PictureBox's whatever the
    // colour, and a form's for a new colour only. Each keeps the current point.
    [TestMethod]
    public void APictureOrABackColourSet_ErasesTheDrawing_AsVB6Does_AndKeepsTheCurrentPoint()
    {
        var rows = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 333d).Set(P.Height, 200d).Set(P.ScaleMode, ScaleMode.Pixel)
                .Set(P.BackColor, Grey).Set(P.Picture, Quadrants());
            var box = Box(Quadrants()).Set(P.Left, 200d).Set(P.Top, 10d);
            box.SetContainer(root);
            var window = new VBFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var rows = new List<string>();
            void Step(string label, ComponentInstance target, double backX, Action set)
            {
                var surface = DrawingSurfaces.Of(target)!;
                surface.Cls();
                surface.PSet(new GraphicsPoint(backX, 30), Pen);
                surface.PSet(new GraphicsPoint(2, 2), Pen);
                surface.CurrentX = 7;
                surface.CurrentY = 9;
                set();
                Dispatcher.UIThread.RunJobs();
                rows.Add($"{label}\t{Hex(surface, (backX, 30), (2, 2))}\t{surface.CurrentX},{surface.CurrentY}");
            }
            var bytes = box.Get(P.Picture);
            Step("box, the same BackColor", box, 30, () => box.Set(P.BackColor, Grey));
            Step("box, a new BackColor", box, 30, () => box.Set(P.BackColor, FormColor.FromRgb(0xC0, 0xC0, 0xC0)));
            box.Set(P.BackColor, Grey);
            Step("box, the same Picture", box, 30, () => box.Set(P.Picture, bytes));
            Step("box, no Picture", box, 30, () => box.Set(P.Picture, null));
            Step("form, the same BackColor", root, 100, () => root.Set(P.BackColor, Grey));
            Step("form, a new BackColor", root, 100, () => root.Set(P.BackColor, FormColor.FromRgb(0xC0, 0xC0, 0xC0)));
            root.Set(P.BackColor, Grey);
            Step("form, a Picture", root, 100, () => root.Set(P.Picture, Quadrants()));
            window.Close();
            return rows;
        });

        Expect.Lines(new[]
        {
            "box, the same BackColor\t808080\t563412\t7,9",
            "box, a new BackColor\tC0C0C0\t563412\t7,9",
            "box, the same Picture\t808080\t563412\t7,9",
            "box, no Picture\t808080\t808080\t7,9",
            "form, the same BackColor\tF0A050\tF0A050\t7,9",
            "form, a new BackColor\tC0C0C0\t563412\t7,9",
            "form, a Picture\t808080\t563412\t7,9",
        }, rows);
    }
}
