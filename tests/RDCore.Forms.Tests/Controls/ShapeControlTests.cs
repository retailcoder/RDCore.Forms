using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ShapeControlTests
{
    private static readonly Color White = Color.FromRgb(255, 255, 255), Red = Color.FromRgb(255, 0, 0), Blue = Color.FromRgb(0, 0, 255);

    // A white form of 300 by 200 pixels, in its scale.
    private static ComponentInstance Root(ScaleMode scale = ScaleMode.Twip) =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 4500d).Set(P.Height, 3000d).Set(P.ScaleMode, scale)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255));

    private static ComponentInstance Shape(ComponentInstance container, ShapeType figure, double left, double top, double width, double height)
    {
        var shape = new ComponentInstance(VBClasses.Shape, "Shape1").Set(P.Shape, figure)
            .Set(P.Left, left * 15).Set(P.Top, top * 15).Set(P.Width, width * 15).Set(P.Height, height * 15)
            .Set(P.BorderColor, FormColor.FromRgb(0, 0, 255)).Set(P.FillColor, FormColor.FromRgb(255, 0, 0));
        shape.SetContainer(container);
        return shape;
    }

    private static ComponentInstance Line(ComponentInstance container, double x1, double y1, double x2, double y2)
    {
        var line = new ComponentInstance(VBClasses.Line, "Line1").Set(P.X1, x1).Set(P.Y1, y1).Set(P.X2, x2).Set(P.Y2, y2)
            .Set(P.BorderColor, FormColor.FromRgb(0, 0, 255));
        line.SetContainer(container);
        return line;
    }

    // The colours of the form's pixels.
    private static Color[] Render(ComponentInstance root, params PixelPoint[] points)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var colours = PixelsAt(window, points);
        window.Close();
        return colours;
    }

    private static PixelPoint[] Row(int y, int from, int to) => [.. Enumerable.Range(from, to - from + 1).Select(x => new PixelPoint(x, y))];

    [TestMethod]
    public void ASolidRectangle_IsFilled_AndBorderedInsideItsBounds()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            Shape(root, ShapeType.Rectangle, 10, 10, 50, 30).Set(P.FillStyle, FillStyle.Solid);
            return Render(root, new(9, 20), new(10, 20), new(11, 20), new(35, 25), new(59, 20), new(60, 20));
        });

        CollectionAssert.AreEqual(new[] { White, Blue, Red, Red, Blue, White }, colours);
    }

    [TestMethod]
    public void AnOval_LeavesItsCornersUndrawn_AndACircle_IsAsWideAsTheShorterSide()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            Shape(root, ShapeType.Oval, 10, 10, 100, 40).Set(P.FillStyle, FillStyle.Solid);
            Shape(root, ShapeType.Circle, 150, 10, 100, 40).Set(P.FillStyle, FillStyle.Solid);
            // The oval's corner and centre; the circle's centre, and a point inside its bounds but left of its 40-pixel circle.
            return Render(root, new(11, 11), new(60, 30), new(200, 30), new(160, 30));
        });

        CollectionAssert.AreEqual(new[] { White, Red, Red, White }, colours);
    }

    [TestMethod]
    public void AnOpaqueBackStyle_DrawsTheBackColour_UnderATransparentFill()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            Shape(root, ShapeType.Rectangle, 10, 10, 50, 30).Set(P.BackStyle, BackStyle.Opaque).Set(P.BackColor, FormColor.FromRgb(0, 255, 0));
            Shape(root, ShapeType.Rectangle, 100, 10, 50, 30);
            return Render(root, new(35, 25), new(125, 25));
        });

        CollectionAssert.AreEqual(new[] { Color.FromRgb(0, 255, 0), White }, colours);
    }

    [TestMethod]
    public void AHorizontalLineFill_DrawsEveryEighthRow_FromTheShapesCorner()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            Shape(root, ShapeType.Rectangle, 10, 10, 50, 40).Set(P.FillStyle, FillStyle.HorizontalLine)
                .Set(P.ShapeBorderStyle, ShapeBorderStyle.Transparent);
            return Render(root, new(30, 18), new(30, 19), new(30, 26), new(30, 22));
        });

        CollectionAssert.AreEqual(new[] { Red, White, Red, White }, colours);
    }

    [TestMethod]
    public void AShape_TakesNoInput()
    {
        var hit = UI.Run(() => new VBShape(new ComponentInstance(VBClasses.Shape, "Shape1")).IsHitTestVisible);

        Assert.IsFalse(hit);
    }

    [TestMethod]
    public void AOnePixelLine_CoversItsFirstEnd_UpToItsSecond_LeavingTheSecondOut()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            // From pixel (10, 20) to pixel (40, 20), in twips.
            Line(root, 150, 300, 600, 300);
            return Render(root, [new(9, 20), new(10, 20), new(39, 20), new(40, 20), new(25, 19), new(25, 21)]);
        });

        CollectionAssert.AreEqual(new[] { White, Blue, Blue, White, White, White }, colours);
    }

    [TestMethod]
    public void ADashedLine_RunsEighteenPixelsOn_AndSixOff_FromItsFirstEnd()
    {
        var colours = UI.Run(() =>
        {
            var root = Root();
            Line(root, 0, 300, 1500, 300).Set(P.ShapeBorderStyle, ShapeBorderStyle.Dash);
            return Render(root, Row(20, 0, 30));
        });

        var on = colours.Select(c => c == Blue).ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, 31).Select(x => x % 24 < 18).ToArray(), on);
    }

    [TestMethod]
    public void AWiderLine_IsSolid_CoversBothEnds_AndItsExtraRowIsAbove()
    {
        var (two, three) = UI.Run(() =>
        {
            var root = Root();
            Line(root, 150, 300, 600, 300).Set(P.BorderWidth, (short)2).Set(P.ShapeBorderStyle, ShapeBorderStyle.Dot);
            Line(root, 150, 900, 600, 900).Set(P.BorderWidth, (short)3);
            var colours = Render(root, new(25, 18), new(25, 19), new(25, 20), new(25, 21), new(40, 20),
                new(25, 58), new(25, 59), new(25, 60), new(25, 61), new(25, 62));
            return (colours[..5], colours[5..]);
        });

        CollectionAssert.AreEqual(new[] { White, Blue, Blue, White, Blue }, two);
        CollectionAssert.AreEqual(new[] { White, Blue, Blue, Blue, White }, three);
    }

    [TestMethod]
    public void ALinesEnds_AreInItsContainersScale_AndFollowIt()
    {
        var (pixel, user, frame, changed) = UI.Run(() =>
        {
            var pixelRoot = Root(ScaleMode.Pixel);
            var inPixels = Line(pixelRoot, 10, 20, 30, 40);
            // 4500 twips across over 100 units is 45 twips a unit, and 3000 down over -50 is -60, from a top of 50.
            var userRoot = Root(ScaleMode.User).Set(P.ScaleWidth, 100d).Set(P.ScaleHeight, -50d).Set(P.ScaleTop, 50d);
            var inUser = Line(userRoot, 10, 50, 20, 0);
            var frameInPixels = new ComponentInstance(VBClasses.Frame, "Frame1").Set(P.Width, 1500d).Set(P.Height, 1500d);
            frameInPixels.SetContainer(pixelRoot);
            var inFrame = Line(frameInPixels, 10, 20, 30, 40);
            var window = new VBFormWindow(new FormDefinition(pixelRoot));
            var userWindow = new VBFormWindow(new FormDefinition(userRoot));
            window.Show();
            userWindow.Show();
            Dispatcher.UIThread.RunJobs();
            var control = (VBLine)window.View.ControlOf(inPixels)!;
            var userControl = (VBLine)userWindow.View.ControlOf(inUser)!;
            var frameControl = (VBLine)window.View.ControlOf(inFrame)!;
            var before = (control.Start, control.End);
            var user = (userControl.Start, userControl.End);
            var frame = (frameControl.Start, frameControl.End);
            pixelRoot.Set(P.ScaleMode, ScaleMode.Point);
            var changed = (control.Start, control.End);
            window.Close();
            userWindow.Close();
            return (before, user, frame, changed);
        });

        Assert.AreEqual((new Point(150, 300), new Point(450, 600)), pixel);
        Assert.AreEqual((new Point(450, 0), new Point(900, 3000)), user);
        Assert.AreEqual((new Point(10, 20), new Point(30, 40)), frame);
        Assert.AreEqual((new Point(200, 400), new Point(600, 800)), changed);
    }

    [TestMethod]
    public void ALine_TakesNoInput()
    {
        var hit = UI.Run(() => new VBLine(new ComponentInstance(VBClasses.Line, "Line1")).IsHitTestVisible);

        Assert.IsFalse(hit);
    }
}
