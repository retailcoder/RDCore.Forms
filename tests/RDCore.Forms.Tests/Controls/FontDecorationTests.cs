using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
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
    [Ignore("A TextBox's text is drawn by Avalonia's text presenter, which draws no underline or strikethrough.")]
    public void ATextBoxsText_IsUnderlinedAndStruckThrough_AsItsFontSays()
    {
        var (plain, underlined, struck) = UI.Run(() => (DarkRows(ComponentClasses.TextBox, Inter), DarkRows(ComponentClasses.TextBox, Inter with { Underline = true }),
            DarkRows(ComponentClasses.TextBox, Inter with { Strikethrough = true })));

        Assert.IsTrue(Added(underlined, plain).Length > 0, "the underline is drawn");
        Assert.IsTrue(Added(struck, plain).Length > 0, "the strikethrough is drawn");
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
