using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;
using Item = RDCore.Forms.Drawing.PrintItem;

namespace RDCore.Forms.Tests.Controls;

// The Print method's rules as measured with VB6, native and p-code agreeing, on a borderless white PictureBox of 6000 by 1500 twips.
// The font is the Inter the application carries, at 8.25 points, so the values are the same on every machine: a column of 7 pixels
// (105 twips), a zone of 98 (1470 twips) and a line of 14 (210 twips); "a" and "c" are 6 pixels wide and "b" 7. Each row clears the
// box, runs its statements, and gives CurrentX and CurrentY.
[TestClass]
public sealed class PrintTests
{
    private static readonly FormFont Inter = FormFont.Default with { Name = "fonts:Inter#Inter" };

    private static ComponentInstance Box() =>
        new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.BorderStyle, BorderStyle.None).Set(P.Width, 400d).Set(P.Height, 100d)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255)).Set(P.Font, Inter);

    private static Item Text(string value) => new Item.Text(value);

    private static Item Spc(int count) => new Item.Spc(count);

    private static Item Tab(int? column = null) => new Item.Tab(column);

    private static List<string> Rows(Action<IDrawingSurface>? scale, params (string Label, Action<IDrawingSurface> Run)[] rows) => UI.Run(() =>
    {
        var surface = new VBDrawingLayer(Box());
        surface.Scale();
        var lines = new List<string>();
        foreach (var (label, run) in rows)
        {
            surface.Cls();
            scale?.Invoke(surface);
            run(surface);
            lines.Add($"{label}\t{surface.CurrentX:0.###}\t{surface.CurrentY:0.###}");
            surface.Scale();
        }
        return lines;
    });

    private static Action<IDrawingSurface> At(double x, double y, params (Item[] Items, bool EndsLine)[] prints) => surface =>
    {
        surface.CurrentX = x;
        surface.CurrentY = y;
        foreach (var (items, endsLine) in prints)
            surface.Print(items, endsLine);
    };

    private static Action<IDrawingSurface> Printing(params (Item[] Items, bool EndsLine)[] prints) => At(0, 0, prints);

    [TestMethod]
    public void Print_MovesTheCurrentPointByColumnsZonesAndLines_AsVB6Does()
    {
        var alphabet = "abcdefghijklmnopqrstuvwxyz";
        var rows = Rows(null,
            ("Print abc", Printing(([Text("abc")], true))),
            ("Print abc;", Printing(([Text("abc")], false))),
            ("Print", Printing(([], true))),
            ("Print a,", Printing(([Text("a"), Tab()], false))),
            ("Print a, b,", Printing(([Text("a"), Tab(), Text("b"), Tab()], false))),
            ("Print ,", Printing(([Tab()], false))),
            ("Print a long text,", Printing(([Text(alphabet), Tab()], false))),
            ("Print Spc(5);", Printing(([Spc(5)], false))),
            ("CurrentX=106 Print Spc(1);", At(106, 0, ([Spc(1)], false))),
            ("CurrentX=100 Print Spc(0);", At(100, 0, ([Spc(0)], false))),
            ("Print Spc(-2);", Printing(([Spc(-2)], false))),
            ("CurrentX=500 Print Tab(0);", At(500, 0, ([Tab(0)], false))),
            ("Print Tab(10);", Printing(([Tab(10)], false))),
            ("Print abcdef; Tab(2);", Printing(([Text("abcdef"), Tab(2)], false))),
            ("Print a; Tab;", Printing(([Text("a"), Tab()], false))),
            ("Print ab CRLF c;", Printing(([Text("ab\r\nc")], false))),
            ("Print ab LF c;", Printing(([Text("ab\nc")], false))),
            ("Print ab CR c;", Printing(([Text("ab\rc")], false))),
            ("CurrentX=500 Print a", At(500, 0, ([Text("a")], true))),
            ("CurrentX=500 Print a;", At(500, 0, ([Text("a")], false))),
            ("CurrentX=500 Print a,", At(500, 0, ([Text("a"), Tab()], false))),
            ("CurrentX=500 Print Tab(3);", At(500, 0, ([Tab(3)], false))),
            ("CurrentY=200 Print a", At(0, 200, ([Text("a")], true))),
            ("Print a then Print b;", Printing(([Text("a")], true), ([Text("b")], false))),
            ("Print a;b", Printing(([Text("a"), Text("b")], true))),
            ("at zone 1 Print ,", Printing(([Text("a"), Tab()], false), ([Tab()], false))),
            ("a twip before zone 1 Print ,", s =>
            {
                s.Print([Text("a"), Tab()], false);
                s.CurrentX -= 1;
                s.Print([Tab()], false);
            }),
            ("at column 10 Print Tab(10);", Printing(([Tab(10)], false), ([Tab(10)], false))),
            ("inside column 10 Print Tab(10);", s =>
            {
                s.Print([Tab(10)], false);
                s.CurrentX += 15;
                s.Print([Tab(10)], false);
            }),
            ("just before column 10 Print Tab(10);", s =>
            {
                s.Print([Tab(10)], false);
                s.CurrentX -= 15;
                s.Print([Tab(10)], false);
            }));

        Expect.Lines(new[]
        {
            "Print abc\t0\t210",
            "Print abc;\t285\t0",
            "Print\t0\t210",
            "Print a,\t1470\t0",
            "Print a, b,\t2940\t0",
            "Print ,\t1470\t0",
            // 155 pixels, past the first zone.
            "Print a long text,\t2940\t0",
            "Print Spc(5);\t525\t0",
            // From the pixel 106 twips is on, 105 twips: a column on is 210, where 106 twips and a column would round up to 315.
            "CurrentX=106 Print Spc(1);\t210\t0",
            "CurrentX=100 Print Spc(0);\t105\t0",
            "Print Spc(-2);\t0\t0",
            "CurrentX=500 Print Tab(0);\t0\t210",
            "Print Tab(10);\t945\t0",
            "Print abcdef; Tab(2);\t105\t210",
            "Print a; Tab;\t1470\t0",
            "Print ab CRLF c;\t90\t210",
            "Print ab LF c;\t90\t210",
            "Print ab CR c;\t90\t210",
            "CurrentX=500 Print a\t0\t210",
            // 500 twips is on pixel 33, 495 twips.
            "CurrentX=500 Print a;\t585\t0",
            "CurrentX=500 Print a,\t1470\t0",
            "CurrentX=500 Print Tab(3);\t210\t210",
            // 200 twips is on pixel 13, 195 twips.
            "CurrentY=200 Print a\t0\t405",
            "Print a then Print b;\t105\t210",
            "Print a;b\t0\t210",
            "at zone 1 Print ,\t2940\t0",
            "a twip before zone 1 Print ,\t2940\t0",
            "at column 10 Print Tab(10);\t945\t0",
            "inside column 10 Print Tab(10);\t945\t210",
            "just before column 10 Print Tab(10);\t945\t0",
        }, rows);
    }

    // Scale (100, 100)-(3100, 850): a unit is two twips, and the scale's 0 is 200 twips left of and above the inside's corner. Columns
    // and zones count from the scale's 0; a text moves CurrentY to the pixel it is drawn on, and Spc, Tab and a comma keep it.
    [TestMethod]
    public void InAUserScale_ColumnsAndZonesCountFromTheScalesZero_AndEachPositionGoesToAPixel()
    {
        var rows = Rows(s => s.Scale(100, 100, 3100, 850),
            ("Print ,", Printing(([Tab()], false))),
            ("CurrentX=500 Print a,", At(500, 0, ([Text("a"), Tab()], false))),
            ("CurrentX=500 Print a;", At(500, 0, ([Text("a")], false))),
            ("CurrentX=500 Print Spc(2);", At(500, 0, ([Spc(2)], false))),
            ("CurrentX=500 Print Tab(30);", At(500, 0, ([Tab(30)], false))),
            ("CurrentX=500 Print Tab(3);", At(500, 0, ([Tab(3)], false))),
            ("CurrentX=500 Print a", At(500, 0, ([Text("a")], true))),
            ("CurrentX=500 Print a, b;", At(500, 0, ([Text("a"), Tab(), Text("b")], false))),
            ("the scale set after a Cls", _ => { }),
            ("Cls in the scale", s => s.Cls()));

        Expect.Lines(new[]
        {
            "Print ,\t737.5\t0",
            "CurrentX=500 Print a,\t737.5\t2.5",
            "CurrentX=500 Print a;\t542.5\t2.5",
            "CurrentX=500 Print Spc(2);\t632.5\t0",
            "CurrentX=500 Print Tab(30);\t1525\t0",
            "CurrentX=500 Print Tab(3);\t107.5\t107.5",
            "CurrentX=500 Print a\t2.5\t107.5",
            "CurrentX=500 Print a, b;\t790\t2.5",
            "the scale set after a Cls\t100\t100",
            "Cls in the scale\t0\t0",
        }, rows);
    }

    [TestMethod]
    public void TextWidth_IsTheLongestLine_AndTextHeight_TheLinesTimesALine_InTheScalesUnits()
    {
        var (twips, half) = UI.Run(() =>
        {
            var surface = new VBDrawingLayer(Box());
            surface.Scale();
            string Measure() => string.Join("\t", surface.TextWidth("abc"), surface.TextWidth("ab\r\nabcd"), surface.TextWidth("abcd\rab"),
                surface.TextWidth(""), surface.TextHeight("a"), surface.TextHeight("a\r\nb"), surface.TextHeight("a\nb\rc"), surface.TextHeight(""));
            var twips = Measure();
            surface.Scale(100, 100, 3100, 850);
            return (twips, Measure());
        });

        Assert.AreEqual("285\t390\t390\t0\t210\t420\t630\t210", twips);
        Assert.AreEqual("142.5\t195\t195\t0\t105\t210\t315\t105", half);
    }

    // In pixels, over a red box: five spaces are a cell 15 pixels wide and 14 high.
    [TestMethod]
    [DataRow(false, (short)13, "FFFFFF\tFFFFFF\tFFFFFF\tFF\tFF")]
    [DataRow(true, (short)13, "FF\tFF\tFF\tFF\tFF")]
    [DataRow(false, (short)6, "FFFFFF\tFFFFFF\tFFFFFF\tFF\tFF")]
    public void FontTransparentFalse_FillsTheTextsCellWithTheBackColour_AndDrawModeDoesNotApply(bool transparent, short mode, string expected)
    {
        var points = UI.Run(() =>
        {
            var box = Box().Set(P.ScaleMode, ScaleMode.Pixel);
            var surface = new VBDrawingLayer(box);
            surface.Line(new GraphicsPoint(0, 0), new GraphicsPoint(200, 60), FormColor.FromRgb(255, 0, 0), LineBox.FilledBox);
            box.Set(P.FontTransparent, transparent).Set(P.DrawMode, mode);
            surface.CurrentX = 0;
            surface.CurrentY = 0;
            surface.Print([Text("     ")], false);
            return string.Join("\t", new[] { (1, 6), (1, 13), (14, 6), (1, 14), (15, 6) }.Select(p => surface.Point(p.Item1, p.Item2).ToString("X")));
        });

        Assert.AreEqual(expected, points);
    }

    // The text is drawn in the ForeColor with no smoothing, within its cell; DrawMode does not change it.
    [TestMethod]
    [DataRow((short)13)]
    [DataRow((short)6)]
    public void TheText_IsDrawnInTheForeColour_WithinItsCell(short mode)
    {
        var (inside, outside, other) = UI.Run(() =>
        {
            var box = Box().Set(P.ScaleMode, ScaleMode.Pixel).Set(P.ForeColor, FormColor.FromRgb(0, 0, 255)).Set(P.DrawMode, mode);
            var surface = new VBDrawingLayer(box);
            surface.CurrentX = 20;
            surface.CurrentY = 10;
            surface.Print([Text("W")], false);
            var colours = (from x in Enumerable.Range(0, 60) from y in Enumerable.Range(0, 40) select (x, y, c: surface.Point(x, y))).ToList();
            bool Within((int x, int y, int c) p) => p.x >= 20 && p.x < 20 + surface.TextWidth("W") && p.y >= 10 && p.y < 10 + surface.TextHeight("W");
            return (colours.Count(p => Within(p) && p.c == 0xFF0000), colours.Count(p => !Within(p) && p.c != 0xFFFFFF),
                colours.Count(p => p.c is not (0xFF0000 or 0xFFFFFF)));
        });

        Assert.IsTrue(inside > 10, $"{inside} pixels of the text");
        Assert.AreEqual(0, outside);
        Assert.AreEqual(0, other);
    }

    [TestMethod]
    public void FontTransparent_IsAFormsAndAPictureBoxsProperty_TrueByDefault()
    {
        Assert.IsTrue(new ComponentInstance(VBClasses.Form, "Form1").Get(P.FontTransparent));
        Assert.IsTrue(new ComponentInstance(VBClasses.PictureBox, "Picture1").Get(P.FontTransparent));
        Assert.IsFalse(VBClasses.Label.Has(P.FontTransparent));
    }
}
