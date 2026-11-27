using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A font's Underline and Strikethrough, drawn under and through a control's text and the text Print draws. The font is the Inter the
// application carries, so the text is the same on every machine. Each case draws the same text plain, underlined and struck through,
// and compares the dark pixels of each row: the strikethrough adds some above the underline's.
[TestClass]
public sealed class FontDecorationTests
{
    private static readonly FormFont Inter = FormFont.Default with { Name = "fonts:Inter#Inter", Size = 12 };

    private const int FormWidth = 300, FormHeight = 120;

    // The dark pixels of each row of a control drawn on a white form, in the control's bounds.
    private static int[] DarkRows(ComponentClass kind, FormFont font, FormFont? changedTo = null)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, (double)FormWidth).Set(P.Height, (double)FormHeight)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        var control = new ComponentInstance(kind, "Control1").Set(P.Left, 10d).Set(P.Top, 10d).Set(P.Width, 260d).Set(P.Height, 60d)
            .Set(P.Font, font);
        if (kind.Has(P.Caption))
            control.Set(P.Caption, "MMMMMMMM");
        if (kind.Has(P.Text))
            control.Set(P.Text, "MMMMMMMM");
        if (kind.Has(P.BackColor))
            control.Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        control.SetContainer(root);
        var window = new RDFormsFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        if (changedTo is { } changed)
        {
            control.Set(P.Font, changed);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        int[] rows;
        using (var buffer = frame.Lock())
        {
            rows = new int[60];
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 10; x < 270; x++)
                {
                    var value = Marshal.ReadInt32(buffer.Address, (10 + y) * buffer.RowBytes + x * 4);
                    if ((byte)value + (byte)(value >> 8) + (byte)(value >> 16) < 200)
                        rows[y]++;
                }
            }
        }
        window.Close();
        return rows;
    }

    // The rows where a decorated text has more dark pixels than the plain one.
    private static int[] Added(int[] decorated, int[] plain) => [.. Enumerable.Range(0, plain.Length).Where(y => decorated[y] > plain[y])];

    private static ComponentClass Kind(string name) => name switch
    {
        "Label" => ComponentClasses.Label,
        "CheckBox" => ComponentClasses.CheckBox,
        "OptionButton" => ComponentClasses.OptionButton,
        "CommandButton" => ComponentClasses.CommandButton,
        "Frame" => ComponentClasses.Frame,
        "Data" => ComponentClasses.Data,
        "TextBox" => ComponentClasses.TextBox,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [TestMethod]
    [DataRow("Label")]
    [DataRow("CheckBox")]
    [DataRow("OptionButton")]
    [DataRow("CommandButton")]
    [DataRow("Frame")]
    [DataRow("Data")]
    public void AControlsText_IsUnderlinedAndStruckThrough_AsItsFontSays(string name)
    {
        var (plain, underlined, struck) = UI.Run(() => (DarkRows(Kind(name), Inter), DarkRows(Kind(name), Inter with { Underline = true }),
            DarkRows(Kind(name), Inter with { Strikethrough = true })));

        var (under, through) = (Added(underlined, plain), Added(struck, plain));

        Assert.IsTrue(under.Length > 0, "the underline is drawn");
        Assert.IsTrue(through.Length > 0, "the strikethrough is drawn");
        Assert.IsTrue(through.Max() < under.Min(), $"the strikethrough's rows ({string.Join(" ", through)}) are above the underline's ({string.Join(" ", under)})");
    }

    [TestMethod]
    public void ATextBoxsText_IsUnderlinedAndStruckThrough_AsItsFontSays()
    {
        var (plain, underlined, struck) = UI.Run(() => (DarkRows(ComponentClasses.TextBox, Inter), DarkRows(ComponentClasses.TextBox, Inter with { Underline = true }),
            DarkRows(ComponentClasses.TextBox, Inter with { Strikethrough = true })));

        var (under, through) = (Added(underlined, plain), Added(struck, plain));

        Assert.IsTrue(under.Length > 0, "the underline is drawn");
        Assert.IsTrue(through.Length > 0, "the strikethrough is drawn");
        Assert.IsTrue(through.Max() < under.Min(),
            $"the strikethrough's rows ({string.Join(" ", through)}) are above the underline's ({string.Join(" ", under)})");
    }

    // A white TextBox of a text on a white form, drawn: for each row of its bounds, its dark pixels inside its border and the rightmost
    // of them, or -1.
    private static (int Count, int Right)[] Box(FormFont font, string text, bool multiLine = false, Action<RDFormsTextBox>? set = null)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, (double)FormWidth).Set(P.Height, (double)FormHeight)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        var control = new ComponentInstance(ComponentClasses.TextBox, "Text1").Set(P.Left, 10d).Set(P.Top, 10d).Set(P.Width, 260d)
            .Set(P.Height, 90d).Set(P.Font, font).Set(P.MultiLine, multiLine).Set(P.Text, text).Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        control.SetContainer(root);
        var window = new RDFormsFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        set?.Invoke((RDFormsTextBox)window.View.ControlOf(control)!);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        var rows = new (int, int)[90];
        using (var buffer = frame.Lock())
        {
            for (var y = 0; y < rows.Length; y++)
            {
                var (count, right) = (0, -1);
                for (var x = 13; x < 267; x++)
                {
                    var value = Marshal.ReadInt32(buffer.Address, (10 + y) * buffer.RowBytes + x * 4);
                    if ((byte)value + (byte)(value >> 8) + (byte)(value >> 16) < 200)
                        (count, right) = (count + 1, x - 10);
                }
                rows[y] = (count, right);
            }
        }
        window.Close();
        return rows;
    }

    // The rows where a decorated box has more dark pixels than the plain one, in runs of rows next to each other.
    private static List<int[]> AddedRuns((int Count, int Right)[] decorated, (int Count, int Right)[] plain)
    {
        var runs = new List<int[]>();
        foreach (var y in Added([.. decorated.Select(row => row.Count)], [.. plain.Select(row => row.Count)]))
        {
            if (runs.Count > 0 && runs[^1][^1] == y - 1)
                runs[^1] = [.. runs[^1], y];
            else
                runs.Add([y]);
        }
        return runs;
    }

    // A TextBox's underline is under its text and its strikethrough through it, at the lines Print draws: "MM" has no descender.
    [TestMethod]
    public void ATextBoxsUnderline_IsUnderItsText_AndItsStrikethroughThroughIt()
    {
        var (empty, plain, underlined, struck) = UI.Run(() => (Box(Inter, ""), Box(Inter, "MM"), Box(Inter with { Underline = true }, "MM"),
            Box(Inter with { Strikethrough = true }, "MM")));

        var glyphs = AddedRuns(plain, empty).Single();
        var under = AddedRuns(underlined, plain).Single();
        var through = AddedRuns(struck, plain).Single();
        Assert.IsGreaterThan(glyphs.Max(), under.Min(), "the underline is under the text");
        Assert.IsTrue(through.Min() > glyphs.Min() && through.Max() < glyphs.Max(),
            $"the strikethrough's rows ({string.Join(" ", through)}) are within the text's ({glyphs.Min()} to {glyphs.Max()})");
    }

    // VB6 underlines a TextBox's spaces with its text, leading and trailing: the line goes on past the text's last glyph.
    [TestMethod]
    public void ATextBoxsUnderline_TakesInItsTextsSpaces()
    {
        var (text, textPlain, spaced, spacedPlain) = UI.Run(() => (Box(Inter with { Underline = true }, "MM"), Box(Inter, "MM"),
            Box(Inter with { Underline = true }, "MM      "), Box(Inter, "MM      ")));

        var underRight = AddedRuns(text, textPlain).Single().Max(y => text[y].Right);
        var spacedRight = AddedRuns(spaced, spacedPlain).Single().Max(y => spaced[y].Right);
        Assert.IsGreaterThan(underRight + 10, spacedRight, "the underline of the spaced text ends past the text's own");
    }

    // Each line of a multi-line box is underlined, and an empty line is not: two lines of text, with an empty one between them.
    [TestMethod]
    public void AMultiLineTextBox_UnderlinesEachLineOfItsText()
    {
        var (underlined, plain) = UI.Run(() => (Box(Inter with { Underline = true }, "MM\r\n\r\nMM", multiLine: true),
            Box(Inter, "MM\r\n\r\nMM", multiLine: true)));

        Assert.AreEqual(2, AddedRuns(underlined, plain).Count, "an underline under each line of text, none under the empty line");
    }

    // VB6 draws the line under the selection in the selected text's colour. Here that is white, on a light selection, so that the
    // selected part of the line is no dark pixel: 6 of the 8 characters are selected.
    [TestMethod]
    public void ATextBoxsUnderline_UnderTheSelection_IsTheSelectedTextsColour()
    {
        var (selected, hidden, unselected, plain) = UI.Run(() =>
        {
            void Select(RDFormsTextBox box)
            {
                box.SelectionForegroundBrush = Brushes.White;
                box.SelectionBrush = Brushes.LightGray;
                box.Focus();
                box.SelectionStart = 1;
                box.SelectionEnd = 7;
            }
            // A selection the presenter does not show draws its text, and its line, in the text's own colour.
            void Hide(RDFormsTextBox box)
            {
                Select(box);
                Dispatcher.UIThread.RunJobs();
                box.GetVisualDescendants().OfType<TextPresenter>().Single().ShowSelectionHighlight = false;
            }
            return (Box(Inter with { Underline = true }, "MMMMMMMM", set: Select), Box(Inter with { Underline = true }, "MMMMMMMM", set: Hide),
                Box(Inter with { Underline = true }, "MMMMMMMM"), Box(Inter, "MMMMMMMM"));
        });

        var underline = AddedRuns(unselected, plain).Single();
        Assert.IsTrue(underline.All(y => selected[y].Count < unselected[y].Count / 2),
            $"the underline's rows keep {string.Join(" ", underline.Select(y => selected[y].Count))} of "
            + $"{string.Join(" ", underline.Select(y => unselected[y].Count))} dark pixels");
        CollectionAssert.AreEqual(underline.Select(y => unselected[y].Count).ToArray(), underline.Select(y => hidden[y].Count).ToArray(),
            "a selection not shown leaves the underline whole");
    }

    // As a program sets FontStrikethru on a shown TextBox, and then clears it.
    [TestMethod]
    public void AShownTextBoxsFont_IsStruckThrough_AndNoLonger_AsItChanges()
    {
        var (plain, struck, cleared) = UI.Run(() => (DarkRows(ComponentClasses.TextBox, Inter),
            DarkRows(ComponentClasses.TextBox, Inter, Inter with { Strikethrough = true }),
            DarkRows(ComponentClasses.TextBox, Inter with { Strikethrough = true }, Inter)));

        Assert.IsTrue(Added(struck, plain).Length > 0, "the strikethrough is drawn once the font has it");
        CollectionAssert.AreEqual(plain, cleared, "the strikethrough goes once the font has it no longer");
    }

    // As a program sets FontStrikethru on a shown Label, and then clears it.
    [TestMethod]
    public void AShownLabelsFont_IsStruckThrough_AndNoLonger_AsItChanges()
    {
        var (plain, struck, cleared) = UI.Run(() => (DarkRows(ComponentClasses.Label, Inter), DarkRows(ComponentClasses.Label, Inter, Inter with { Strikethrough = true }),
            DarkRows(ComponentClasses.Label, Inter with { Strikethrough = true }, Inter)));

        Assert.IsTrue(Added(struck, plain).Length > 0, "the strikethrough is drawn once the font has it");
        CollectionAssert.AreEqual(plain, cleared, "the strikethrough goes once the font has it no longer");
    }

    // The rows of a white PictureBox, in pixels, where Print's eight spaces leave dark pixels, and whether the first is dark at the
    // left edge.
    private static (int[] Rows, bool FromTheLeft) PrintedSpaces(FormFont font) => UI.Run(() =>
    {
        var box = new ComponentInstance(ComponentClasses.PictureBox, "Picture1").Set(P.BorderStyle, BorderStyle.None).Set(P.Width, 200d).Set(P.Height, 50d)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255)).Set(P.Font, font);
        var surface = new RDFormsDrawingLayer(box);
        surface.Scale(0, 0, 200, 50);
        surface.Print([new PrintItem.Text("        ")]);
        var rows = Enumerable.Range(0, 30).Where(y => Enumerable.Range(0, 40).Any(x => surface.Point(x + 0.5, y + 0.5) != 0xFFFFFF)).ToArray();
        return (rows, rows.Length > 0 && surface.Point(0.5, rows[0] + 0.5) != 0xFFFFFF);
    });

    [TestMethod]
    public void PrintsText_IsUnderlinedAndStruckThrough_ItsSpacesIncluded()
    {
        var plain = PrintedSpaces(Inter);
        var under = PrintedSpaces(Inter with { Underline = true });
        var through = PrintedSpaces(Inter with { Strikethrough = true });

        Assert.AreEqual(0, plain.Rows.Length, "spaces alone draw nothing");
        Assert.IsTrue(under.Rows.Length > 0 && under.FromTheLeft, "the underline is drawn from the text's left");
        Assert.IsTrue(through.Rows.Length > 0 && through.FromTheLeft, "the strikethrough is drawn from the text's left");
        Assert.IsTrue(through.Rows.Max() < under.Rows.Min(), "the strikethrough is above the underline");
    }
}
