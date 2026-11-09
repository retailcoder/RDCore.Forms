using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
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

[TestClass]
public sealed class DrawingSurfaceTests
{
    private const int White = 0xFFFFFF, Red = 0x0000FF, Blue = 0xFF0000, Green = 0x00FF00;

    // A white form of 300 by 200 pixels in a pixel scale, drawing in blue and filling in red.
    private static ComponentInstance Root(bool autoRedraw = false) =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 4500d).Set(P.Height, 3000d).Set(P.ScaleMode, ScaleMode.Pixel)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255)).Set(P.ForeColor, FormColor.FromRgb(0, 0, 255))
            .Set(P.FillColor, FormColor.FromRgb(255, 0, 0)).Set(P.AutoRedraw, autoRedraw);

    private static VBDrawingLayer Surface(ComponentInstance root) => new(root);

    private static int[] Points(IDrawingSurface surface, params (double X, double Y)[] points) =>
        [.. points.Select(p => surface.Point(p.X, p.Y))];

    [TestMethod]
    public void ALine_CoversItsFirstPoint_UpToItsSecond_AndMovesTheCurrentPointThere()
    {
        var (colours, current) = UI.Run(() =>
        {
            var surface = Surface(Root());
            surface.Line(new GraphicsPoint(10, 20), new GraphicsPoint(40, 20));
            return (Points(surface, (9, 20), (10, 20), (39, 20), (40, 20), (25, 21)), (surface.CurrentX, surface.CurrentY));
        });

        CollectionAssert.AreEqual(new[] { White, Blue, Blue, White, White }, colours);
        Assert.AreEqual((40d, 20d), current);
    }

    [TestMethod]
    public void AStepPoint_IsMeasuredFromTheCurrentPoint_AndALinesSecond_FromItsFirst()
    {
        var (colours, current) = UI.Run(() =>
        {
            var surface = Surface(Root());
            surface.CurrentX = 50;
            surface.CurrentY = 50;
            // From (60, 50) to (70, 50); then on from there to (70, 60).
            surface.Line(new GraphicsPoint(10, 0, IsRelative: true), new GraphicsPoint(10, 0, IsRelative: true));
            surface.Line(null, new GraphicsPoint(0, 10, IsRelative: true), FormColor.FromRgb(0, 255, 0));
            return (Points(surface, (59, 50), (60, 50), (69, 50), (70, 55)), (surface.CurrentX, surface.CurrentY));
        });

        CollectionAssert.AreEqual(new[] { White, Blue, Blue, Green }, colours);
        Assert.AreEqual((70d, 60d), current);
    }

    [TestMethod]
    public void ABox_IsBorderedInTheLinesColour_AndFilledAsFillStyleSays_AndBF_FillsItWithTheLinesColour()
    {
        var (box, filled) = UI.Run(() =>
        {
            var surface = Surface(Root().Set(P.FillStyle, FillStyle.Solid));
            surface.Line(new GraphicsPoint(10, 10), new GraphicsPoint(30, 20), box: LineBox.Box);
            surface.Line(new GraphicsPoint(50, 10), new GraphicsPoint(70, 20), FormColor.FromRgb(0, 255, 0), LineBox.FilledBox);
            return (Points(surface, (10, 10), (30, 20), (20, 15), (31, 15)), Points(surface, (50, 10), (60, 15), (70, 20), (71, 20)));
        });

        CollectionAssert.AreEqual(new[] { Blue, Blue, Red, White }, box);
        CollectionAssert.AreEqual(new[] { Green, Green, Green, White }, filled);
    }

    [TestMethod]
    public void ACircle_IsCentredOnItsPoint_FilledAsFillStyleSays_AndLeavesTheCurrentPointThere()
    {
        var (colours, current) = UI.Run(() =>
        {
            var surface = Surface(Root().Set(P.FillStyle, FillStyle.Solid));
            surface.Circle(new GraphicsPoint(100, 100), 20);
            return (Points(surface, (100, 100), (100, 80), (100, 76), (84, 84)), (surface.CurrentX, surface.CurrentY));
        });

        CollectionAssert.AreEqual(new[] { Red, Blue, White, White }, colours);
        Assert.AreEqual((100d, 100d), current);
    }

    [TestMethod]
    public void AnAspectBelow1_FlattensTheCircle_AndAbove1_NarrowsIt()
    {
        var colours = UI.Run(() =>
        {
            var surface = Surface(Root().Set(P.FillStyle, FillStyle.Solid));
            surface.Circle(new GraphicsPoint(60, 60), 40, aspect: 0.25);
            surface.Circle(new GraphicsPoint(200, 100), 40, aspect: 4);
            // The first is 80 across and 20 high; the second 20 across and 80 high.
            return Points(surface, (25, 60), (60, 75), (200, 65), (225, 100));
        });

        CollectionAssert.AreEqual(new[] { Red, White, Red, White }, colours);
    }

    [TestMethod]
    public void AnArcsAngleOutsideTwoPi_IsError5()
    {
        var number = UI.Run(() => Assert.ThrowsExactly<FormErrorException>(() =>
            Surface(Root()).Circle(new GraphicsPoint(10, 10), 5, start: 7)).Error.Number);

        Assert.AreEqual(5, number);
    }

    [TestMethod]
    public void PSet_DrawsAPoint_AndPoint_ReadsIt_TheBackColourWhereNothingIs_AndMinus1Outside()
    {
        var colours = UI.Run(() =>
        {
            var surface = Surface(Root());
            surface.PSet(new GraphicsPoint(5, 5), FormColor.FromRgb(255, 0, 0));
            return Points(surface, (5, 5), (6, 5), (-1, 5), (300, 5), (5, 200));
        });

        CollectionAssert.AreEqual(new[] { Red, White, -1, -1, -1 }, colours);
    }

    [TestMethod]
    public void Cls_ClearsTheDrawing_AndTheCurrentPoint()
    {
        var (colour, current) = UI.Run(() =>
        {
            var surface = Surface(Root());
            surface.PSet(new GraphicsPoint(5, 5));
            surface.Cls();
            return (surface.Point(5, 5), (surface.CurrentX, surface.CurrentY));
        });

        Assert.AreEqual(White, colour);
        Assert.AreEqual((0d, 0d), current);
    }

    [TestMethod]
    public void Scale_SetsAUserScaleOverTheInside_AndScaleWithNoPoints_SetsTwipsBack()
    {
        var (user, colour, twips) = UI.Run(() =>
        {
            var root = Root();
            var surface = Surface(root);
            // 300 by 200 pixels as 0 to 30 across and 0 to 20 down: a unit is 10 pixels.
            surface.Scale(0, 0, 30, 20);
            var user = (root.Get(P.ScaleMode), root.Get(P.ScaleWidth), root.Get(P.ScaleHeight));
            surface.PSet(new GraphicsPoint(3, 2));
            var colour = surface.Point(3, 2) == Blue && surface.Point(3.1, 2) == White;
            surface.Scale();
            return (user, colour, (root.Get(P.ScaleMode), root.Get(P.ScaleWidth), root.Get(P.ScaleHeight), surface.Point(450, 300)));
        });

        Assert.AreEqual((ScaleMode.User, 30d, 20d), user);
        Assert.IsTrue(colour);
        Assert.AreEqual((ScaleMode.Twip, 4500d, 3000d, Blue), twips);
    }

    [TestMethod]
    public void TheDrawing_IsShownUnderTheControls_AndKeptAsTheFormGrows()
    {
        var (before, after) = UI.Run(() =>
        {
            var root = Root();
            var window = new VBFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.View.Drawing.Line(new GraphicsPoint(10, 10), new GraphicsPoint(20, 20), box: LineBox.FilledBox);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var before = PixelsAt(window, new PixelPoint(15, 15), new PixelPoint(25, 25));
            root.Set(P.Width, 6000d);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var after = PixelsAt(window, new PixelPoint(15, 15));
            window.Close();
            return (before, after);
        });

        CollectionAssert.AreEqual(new[] { Color.FromRgb(0, 0, 255), Color.FromRgb(255, 255, 255) }, before);
        CollectionAssert.AreEqual(new[] { Color.FromRgb(0, 0, 255) }, after);
    }

    [TestMethod]
    public void WithAutoRedrawFalse_TheFormRaisesPaint_OnceShown_AndAsItGrows_ButNotAsItShrinks()
    {
        var (shown, grown, shrunk) = UI.Run(() =>
        {
            var root = Root();
            var window = new VBFormWindow(new FormDefinition(root));
            var sink = new RecordingSink();
            VBControl.SetEventSink(window, sink);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var shown = sink.Raised.Count(e => e == "Form1.Paint");
            root.Set(P.Width, 6000d);
            Dispatcher.UIThread.RunJobs();
            var grown = sink.Raised.Count(e => e == "Form1.Paint");
            root.Set(P.Width, 3000d);
            Dispatcher.UIThread.RunJobs();
            var shrunk = sink.Raised.Count(e => e == "Form1.Paint");
            VBControl.SetEventSink(window, null);
            window.Close();
            return (shown, grown, shrunk);
        });

        Assert.AreEqual((1, 2, 2), (shown, grown, shrunk));
    }

    [TestMethod]
    public void WithAutoRedrawTrue_NoPaintIsRaised()
    {
        var raised = UI.Run(() =>
        {
            var root = Root(autoRedraw: true);
            var window = new VBFormWindow(new FormDefinition(root));
            var sink = new RecordingSink();
            VBControl.SetEventSink(window, sink);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            root.Set(P.Width, 6000d);
            Dispatcher.UIThread.RunJobs();
            VBControl.SetEventSink(window, null);
            window.Close();
            return sink.Raised;
        });

        Assert.IsFalse(raised.Contains("Form1.Paint"));
    }

    [TestMethod]
    public void AShownFormsSurfaces_AreRegistered_ForItsRootAndItsPictureBoxes_UntilItCloses()
    {
        var (rootSurface, pictureSurface, closed) = UI.Run(() =>
        {
            var root = Root();
            var picture = new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Width, 1500d).Set(P.Height, 1500d);
            picture.SetContainer(root);
            var window = new VBFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var rootSurface = ReferenceEquals(DrawingSurfaces.Of(root), window.View.Drawing);
            var pictureSurface = ReferenceEquals(DrawingSurfaces.Of(picture), ((VBPictureBox)window.View.ControlOf(picture)!).Drawing);
            window.Close();
            return (rootSurface, pictureSurface, DrawingSurfaces.Of(root));
        });

        Assert.IsTrue(rootSurface);
        Assert.IsTrue(pictureSurface);
        Assert.IsNull(closed);
    }

    [TestMethod]
    public void APictureBoxsSurface_IsItsInside_WithinItsBorder()
    {
        var (inside, colour) = UI.Run(() =>
        {
            var root = Root();
            // 100 by 100 pixels, with a 3-D border of two pixels.
            var picture = new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Left, 150d).Set(P.Top, 150d)
                .Set(P.Width, 1500d).Set(P.Height, 1500d).Set(P.ScaleMode, ScaleMode.Pixel).Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
            picture.SetContainer(root);
            var window = new VBFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var surface = ((VBPictureBox)window.View.ControlOf(picture)!).Drawing;
            surface.PSet(new GraphicsPoint(0, 0), FormColor.FromRgb(255, 0, 0));
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var colour = PixelsAt(window, new PixelPoint(12, 12));
            window.Close();
            return (surface.Inside, colour[0]);
        });

        Assert.AreEqual(new PixelSize(96, 96), inside);
        Assert.AreEqual(Color.FromRgb(255, 0, 0), colour);
    }
}
