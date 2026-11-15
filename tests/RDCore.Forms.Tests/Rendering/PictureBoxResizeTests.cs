using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using M = RDCore.Forms.Model.VB.VBMethods;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// When a PictureBox raises Resize, as VB6 does it: the steps and the log are VB6's, with the sizes in twips that each Resize handler
// reads. Picture1 is a plain PictureBox, Picture2 takes a picture from the form's file with AutoSize, and Picture4 is hidden.
[TestClass]
public sealed class PictureBoxResizeTests
{
    private const double TwipsPerPixel = 15;

    private static ComponentInstance Box(ComponentInstance root, string name, double left, double top)
    {
        var box = new ComponentInstance(VBClasses.PictureBox, name).Set(P.Left, left / TwipsPerPixel).Set(P.Top, top / TwipsPerPixel)
            .Set(P.Width, 1500 / TwipsPerPixel).Set(P.Height, 900 / TwipsPerPixel);
        box.SetContainer(root);
        return box;
    }

    private static string Twips(double pixels) => Math.Round(pixels * TwipsPerPixel).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static FormValue S(double value) => FormValue.FromSingle((float)value);

    [TestMethod]
    public void APictureBox_RaisesResize_AsItsSizeInWholePixelsOrItsBorderChanges_OncePerStatement_AndNotAsItIsMade()
    {
        var log = UI.Run(() =>
        {
            byte[] red = PictureControlTests.Png(24, 20), blue = PictureControlTests.Png(40, 20);
            var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 400d).Set(P.Height, 300d).Set(P.ScaleMode, ScaleMode.Twip);
            var one = Box(root, "Picture1", 1000, 700);
            var two = Box(root, "Picture2", 2700, 700).Set(P.AutoSize, true).Set(P.Picture, red);
            var four = Box(root, "Picture4", 1000, 2000).Set(P.Visible, false);

            var log = new List<string>();
            var sink = new RecordingSink
            {
                Handler = (source, raised, _) =>
                {
                    if (source.Class == VBClasses.PictureBox && raised.Name == "Resize")
                        log.Add($"  {source.Name}.Resize {Twips(source.Get(P.Width))}x{Twips(source.Get(P.Height))}");
                },
            };
            log.Add("> Load Form1");
            var window = new VBFormWindow(new FormDefinition(root));
            VBControl.SetEventSink(window, sink);
            void Step(string statement, Action change)
            {
                log.Add($"> {statement}");
                change();
                Dispatcher.UIThread.RunJobs();
            }

            Step("Picture1.Width = 2000", () => one.Set(P.Width, 2000 / TwipsPerPixel));
            Step("Picture1.Width = 2000", () => one.Set(P.Width, 2000 / TwipsPerPixel));
            Step("Picture1.Height = 1000", () => one.Set(P.Height, 1000 / TwipsPerPixel));
            Step("Picture1.Move 200, 200", () => window.Invoke(one, M.Move, [S(200), S(200)], CodeUnit.VB6));
            Step("Picture1.Move 200, 200, 2500, 1100", () => window.Invoke(one, M.Move, [S(200), S(200), S(2500), S(1100)], CodeUnit.VB6));
            Step("Picture1.Move 200, 200, 2500", () => window.Invoke(one, M.Move, [S(200), S(200), S(2500)], CodeUnit.VB6));
            Step("Picture1.Width = 2501", () => one.Set(P.Width, 2501 / TwipsPerPixel));
            Step("Picture1.Width = 2510", () => one.Set(P.Width, 2510 / TwipsPerPixel));
            Step("Picture1.Left = 300", () => one.Set(P.Left, 300 / TwipsPerPixel));
            Step("Picture4.Width = 2000", () => four.Set(P.Width, 2000 / TwipsPerPixel));
            Step("Form1.Show", window.Show);
            Step("Picture1.Width = 2200", () => one.Set(P.Width, 2200 / TwipsPerPixel));
            Step("Picture1.Width = 2200", () => one.Set(P.Width, 2200 / TwipsPerPixel));
            Step("Picture1.Move 1000, 700, 2400, 1200", () => window.Invoke(one, M.Move, [S(1000), S(700), S(2400), S(1200)], CodeUnit.VB6));
            Step("Picture1.BorderStyle = 0", () => one.Set(P.BorderStyle, BorderStyle.None));
            Step("Picture1.BorderStyle = 1", () => one.Set(P.BorderStyle, BorderStyle.FixedSingle));
            Step("Picture1.Visible = False", () => one.Set(P.Visible, false));
            Step("Picture1.Width = 2300", () => one.Set(P.Width, 2300 / TwipsPerPixel));
            Step("Picture1.Visible = True", () => one.Set(P.Visible, true));
            Step("Picture1.ScaleMode = 3", () => one.Set(P.ScaleMode, ScaleMode.Pixel));
            Step("Picture1.ScaleWidth = 100", () => one.Set(P.ScaleWidth, 100d));
            Step("Picture4.Height = 1200", () => four.Set(P.Height, 1200 / TwipsPerPixel));
            Step("Picture1.AutoSize = True", () => one.Set(P.AutoSize, true));
            Step("Set Picture1.Picture = LoadPicture(Red)", () => one.Set(P.Picture, (byte[])red.Clone()));
            Step("Set Picture1.Picture = LoadPicture(Red)", () => one.Set(P.Picture, (byte[])red.Clone()));
            Step("Set Picture1.Picture = LoadPicture(Blue)", () => one.Set(P.Picture, blue));
            Step("Picture1.AutoSize = False", () => one.Set(P.AutoSize, false));
            Step("Set Picture1.Picture = LoadPicture(Red)", () => one.Set(P.Picture, (byte[])red.Clone()));
            Step("Picture2.AutoSize = False", () => two.Set(P.AutoSize, false));
            Step("Picture2.AutoSize = True", () => two.Set(P.AutoSize, true));
            Step("Form1.Hide", window.Hide);
            Step("Picture1.Width = 2600", () => one.Set(P.Width, 2600 / TwipsPerPixel));
            window.Close();
            return log;
        });

        Expect.Lines(
        [
            "> Load Form1",
            "> Picture1.Width = 2000",
            "  Picture1.Resize 2000x900",
            "> Picture1.Width = 2000",
            "> Picture1.Height = 1000",
            "  Picture1.Resize 2000x1000",
            "> Picture1.Move 200, 200",
            "> Picture1.Move 200, 200, 2500, 1100",
            "  Picture1.Resize 2500x1100",
            "> Picture1.Move 200, 200, 2500",
            "> Picture1.Width = 2501",
            "> Picture1.Width = 2510",
            "> Picture1.Left = 300",
            "> Picture4.Width = 2000",
            "  Picture4.Resize 2000x900",
            "> Form1.Show",
            "> Picture1.Width = 2200",
            "  Picture1.Resize 2200x1100",
            "> Picture1.Width = 2200",
            "> Picture1.Move 1000, 700, 2400, 1200",
            "  Picture1.Resize 2400x1200",
            "> Picture1.BorderStyle = 0",
            "  Picture1.Resize 2400x1200",
            "> Picture1.BorderStyle = 1",
            "  Picture1.Resize 2400x1200",
            "> Picture1.Visible = False",
            "> Picture1.Width = 2300",
            "  Picture1.Resize 2300x1200",
            "> Picture1.Visible = True",
            "> Picture1.ScaleMode = 3",
            "> Picture1.ScaleWidth = 100",
            "> Picture4.Height = 1200",
            "  Picture4.Resize 2000x1200",
            "> Picture1.AutoSize = True",
            "> Set Picture1.Picture = LoadPicture(Red)",
            "  Picture1.Resize 420x360",
            "> Set Picture1.Picture = LoadPicture(Red)",
            "> Set Picture1.Picture = LoadPicture(Blue)",
            "  Picture1.Resize 660x360",
            "> Picture1.AutoSize = False",
            "> Set Picture1.Picture = LoadPicture(Red)",
            "> Picture2.AutoSize = False",
            "> Picture2.AutoSize = True",
            "> Form1.Hide",
            "> Picture1.Width = 2600",
            "  Picture1.Resize 2600x360",
        ], log);
    }

    // Picture3 is aligned to the top and Picture5 to the left, on the same form as VB6's. The form's own Resize is logged with its size.
    [TestMethod]
    public void AnAlignedBox_RaisesResize_AfterTheFormsAndAfterTheBoxesItMoves()
    {
        var log = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 400d).Set(P.Height, 300d).Set(P.ScaleMode, ScaleMode.Twip);
            var three = Box(root, "Picture3", 0, 0).Set(P.Align, Align.Top).Set(P.Height, 600 / TwipsPerPixel);
            var five = Box(root, "Picture5", 0, 600).Set(P.Align, Align.Left).Set(P.Width, 900 / TwipsPerPixel);

            var log = new List<string>();
            var sink = new RecordingSink
            {
                Handler = (source, raised, _) =>
                {
                    if (raised.Name == "Resize")
                        log.Add($"  {source.Name}.Resize {Twips(source.Get(P.Width))}x{Twips(source.Get(P.Height))}");
                },
            };
            log.Add("> Load Form1");
            var window = new VBFormWindow(new FormDefinition(root));
            VBControl.SetEventSink(window, sink);
            void Step(string statement, Action change)
            {
                log.Add($"> {statement}");
                try
                {
                    change();
                }
                catch (FormErrorException e)
                {
                    log.Add($"  err={e.Error.Number}");
                }
                Dispatcher.UIThread.RunJobs();
            }

            Step("Form1.Show", window.Show);
            Step("Picture3.Height = 900", () => VBRunTime.Assign(three, P.Height, 900 / TwipsPerPixel));
            Step("Picture3.Width = 1000", () => VBRunTime.Assign(three, P.Width, 1000 / TwipsPerPixel));
            Step("Picture3.Move 0, 0, 1000, 700", () => window.Invoke(three, M.Move, [S(0), S(0), S(1000), S(700)], CodeUnit.VB6));
            Step("Picture5.Width = 1200", () => VBRunTime.Assign(five, P.Width, 1200 / TwipsPerPixel));
            Step("Form1.Width = Form1.Width + 1500", () => root.Set(P.Width, root.Get(P.Width) + 100));
            Step("Form1.Height = Form1.Height + 1500", () => root.Set(P.Height, root.Get(P.Height) + 100));
            Step("Form1.Move Form1.Left, Form1.Top, Form1.Width - 1500, Form1.Height - 1500", () => window.Invoke(root, M.Move,
                [S(root.Get(P.Left) * TwipsPerPixel), S(root.Get(P.Top) * TwipsPerPixel), S(6000), S(4500)], CodeUnit.VB6));
            Step("Picture3.Align = 0", () => VBRunTime.Assign(three, P.Align, Align.None));
            Step("Picture3.Align = 2", () => VBRunTime.Assign(three, P.Align, Align.Bottom));
            window.Close();
            return log;
        });

        Expect.Lines(
        [
            "> Load Form1",
            "> Form1.Show",
            "  Form1.Resize 6000x4500",
            "  Picture3.Resize 6000x600",
            "  Picture5.Resize 900x3900",
            "> Picture3.Height = 900",
            "  Picture5.Resize 900x3600",
            "  Picture3.Resize 6000x900",
            "> Picture3.Width = 1000",
            "  err=387",
            "> Picture3.Move 0, 0, 1000, 700",
            "  Picture5.Resize 900x3795",
            "  Picture3.Resize 6000x700",
            "> Picture5.Width = 1200",
            "  Picture5.Resize 1200x3795",
            "> Form1.Width = Form1.Width + 1500",
            "  Form1.Resize 7500x4500",
            "  Picture3.Resize 7500x700",
            "> Form1.Height = Form1.Height + 1500",
            "  Form1.Resize 7500x6000",
            "  Picture5.Resize 1200x5295",
            "> Form1.Move Form1.Left, Form1.Top, Form1.Width - 1500, Form1.Height - 1500",
            "  Form1.Resize 6000x4500",
            "  Picture3.Resize 6000x700",
            "  Picture5.Resize 1200x3795",
            "> Picture3.Align = 0",
            "  Picture5.Resize 1200x4500",
            "> Picture3.Align = 2",
            "  Picture5.Resize 1200x3795",
        ], log);
    }

    [TestMethod]
    public void APictureBox_DeclaresResize() =>
        Assert.IsNotNull(VBClasses.PictureBox.Event("Resize"));
}
