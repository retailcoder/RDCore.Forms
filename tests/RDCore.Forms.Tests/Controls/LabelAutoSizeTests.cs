using Avalonia;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Label's AutoSize, as measured with VB6, native and p-code agreeing: its size is its caption's TextWidth and TextHeight in its font,
// inside its border. Sizes are the component's, in pixels, and the expected ones are a PictureBox's TextWidth and TextHeight in pixels,
// in the same font: the Inter the application carries, so the values are the same on every machine.
[TestClass]
public sealed class LabelAutoSizeTests
{
    private static readonly FormFont Inter = FormFont.Default with { Name = "fonts:Inter#Inter" };

    // A surface that measures text as a form does, in pixels.
    private static RDFormsDrawingLayer Measuring(FormFont font) =>
        new(new ComponentInstance(ComponentClasses.PictureBox, "Picture1").Set(P.ScaleMode, ScaleMode.Pixel).Set(P.Font, font));

    private static ComponentInstance Label(string caption, double left = 10, double top = 5) =>
        new ComponentInstance(ComponentClasses.Label, "Label1").Set(P.Font, Inter).Set(P.Caption, caption).Set(P.AutoSize, true)
            .Set(P.Left, left).Set(P.Top, top).Set(P.Width, 120d).Set(P.Height, 30d);

    private static string Size(ComponentInstance label) => $"{label.Get(P.Width)} {label.Get(P.Height)}";

    [TestMethod]
    public void ALabelThatSizesItself_FitsItsCaption_AsAProgramSetsIt_OnAFormNotShown_AndNotAsItLoads()
    {
        var (sizes, expected) = UI.Run(() =>
        {
            var text = Measuring(Inter);
            var big = Measuring(Inter with { Size = 12 });
            string Fits(RDFormsDrawingLayer measure, string caption, int border = 0) =>
                $"{measure.TextWidth(caption) + border} {measure.TextHeight(caption) + border}";
            var expected = new[]
            {
                "loaded: 120 30",
                $"another caption: {Fits(text, "Hello world")}",
                "Width and Height: 200 40",
                "the same caption: 200 40",
                $"two lines: {Fits(text, "two\r\nlines")}",
                $"empty: {text.TextWidth(" ")} {text.TextHeight("")}",
                $"&File: {Fits(text, "File")}",
                $"UseMnemonic False: {Fits(text, "File")}",
                $"AutoSize True again: {Fits(text, "&File")}",
                $"Fixed Single: {Fits(text, "&File", 4)}",
                $"Font 12: {Fits(big, "&File", 4)}",
                $"AutoSize False, another caption: {Fits(big, "&File", 4)}",
            };

            var root = new ComponentInstance(ComponentClasses.Form, "Form1");
            var label = Label("Hello");
            label.SetContainer(root);
            _ = new RDFormsFormWindow(new FormDefinition(root));
            var sizes = new List<string> { $"loaded: {Size(label)}" };
            void Step(string what, Action<ComponentInstance> change)
            {
                change(label);
                sizes.Add($"{what}: {Size(label)}");
            }
            Step("another caption", l => l.Set(P.Caption, "Hello world"));
            Step("Width and Height", l => l.Set(P.Width, 200d).Set(P.Height, 40d));
            Step("the same caption", l => l.Set(P.Caption, "Hello world"));
            Step("two lines", l => l.Set(P.Caption, "two\r\nlines"));
            Step("empty", l => l.Set(P.Caption, ""));
            Step("&File", l => l.Set(P.Caption, "&File"));
            Step("UseMnemonic False", l => l.Set(P.UseMnemonic, false));
            Step("AutoSize True again", l => l.Set(P.AutoSize, true));
            Step("Fixed Single", l => l.Set(P.BorderStyle, BorderStyle.FixedSingle));
            Step("Font 12", l => l.Set(P.Font, Inter with { Size = 12 }));
            Step("AutoSize False, another caption", l => l.Set(P.AutoSize, false).Set(P.Caption, "Hi"));
            return (sizes, expected);
        });

        Expect.Lines(expected, sizes);
    }

    // A word stays on a line where the line through it, without the space after it, fits; a line's width counts that space.
    [TestMethod]
    public void ALabelThatWraps_KeepsItsWidth_AndGrowsDown_AcrossOnlyToALineWiderThanIt()
    {
        var (sizes, expected, drawn) = UI.Run(() =>
        {
            var text = Measuring(Inter);
            var (two, three, four, line) = (text.TextWidth("aaaa aaaa"), text.TextWidth("aaaa aaaa aaaa "),
                text.TextWidth("aaaa aaaa aaaa aaaa"), text.TextHeight("a"));
            // The premises: three words and a space fit in 100 pixels and four words do not.
            Assert.IsTrue(three <= 100 && four > 100, $"{three} {four}");
            var expected = new[]
            {
                $"Width, two words wide: {text.TextWidth("aaaa aaaa ")} {2 * line}",
                $"Width, a pixel less: {two - 1} {3 * line}",
                $"Width 100.4: 100.4 {line}",
                $"Height: 100.4 50",
                $"four words: 100 {2 * line}",
                $"a word wider: {text.TextWidth("aaaaaaaaaaaaaaaaaaaaaaaaa")} {line}",
            };

            var label = Label("aaaa aaaa aaaa").Set(P.WordWrap, true);
            var control = new RDFormsLabel(label);
            var (window, _) = ControlHost.Show((control, new Rect(10, 5, 120, 30)));
            var sizes = new List<string>();
            void Step(string what, Action<ComponentInstance> change)
            {
                change(label);
                sizes.Add($"{what}: {Size(label)}");
            }
            Step("Width, two words wide", l => l.Set(P.Width, (double)two));
            Step("Width, a pixel less", l => l.Set(P.Width, two - 1d));
            Step("Width 100.4", l => l.Set(P.Width, 100.4));
            Step("Height", l => l.Set(P.Height, 50d));
            Step("four words", l => l.Set(P.Caption, "aaaa aaaa aaaa aaaa"));
            var drawn = control.DrawnText;
            Step("a word wider", l => l.Set(P.Caption, "aaaaaaaaaaaaaaaaaaaaaaaaa"));
            window.Close();
            return (sizes, expected, drawn);
        });

        Expect.Lines(expected, sizes);
        Assert.AreEqual("aaaa aaaa aaaa \naaaa", drawn);
    }

    [TestMethod]
    [DataRow(TextAlignment.LeftJustify, DisplayName = "Left: its left edge")]
    [DataRow(TextAlignment.RightJustify, DisplayName = "Right: its right edge")]
    [DataRow(TextAlignment.Center, DisplayName = "Center: its centre")]
    public void ALabelThatSizesItself_KeepsItsAlignmentsEdge_InWholePixels(TextAlignment alignment)
    {
        var (place, expected) = UI.Run(() =>
        {
            var width = Measuring(Inter).TextWidth("Hello");
            var left = alignment switch
            {
                TextAlignment.RightJustify => Math.Round(10.4 + 120) - width,
                TextAlignment.Center => Math.Round(10.4 + (120 - width) / 2, MidpointRounding.AwayFromZero),
                _ => 10,
            };
            var root = new ComponentInstance(ComponentClasses.Form, "Form1");
            var label = Label("Hi", 10.4, 5.6).Set(P.Alignment, alignment);
            label.SetContainer(root);
            _ = new RDFormsFormWindow(new FormDefinition(root));
            label.Set(P.Caption, "Hello");
            return ($"{label.Get(P.Left)} {label.Get(P.Top)} {label.Get(P.Width)}", $"{left} 6 {width}");
        });

        Assert.AreEqual(expected, place);
    }
}
