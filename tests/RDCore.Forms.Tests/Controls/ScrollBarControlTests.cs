using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ScrollBarControlTests
{
    private static ComponentInstance Bar(ComponentClass type, short min = 0, short max = 100, short value = 0) =>
        new ComponentInstance(type, ReferenceEquals(type, VBClasses.HScrollBar) ? "HScroll1" : "VScroll1")
            .Set(P.Min, min).Set(P.Max, max).Set(P.ScrollBarValue, value);

    [TestMethod]
    public void EachClass_RunsItsWay()
    {
        var orientations = UI.Run(() => (new VBScrollBar(Bar(VBClasses.HScrollBar)).Orientation, new VBScrollBar(Bar(VBClasses.VScrollBar)).Orientation));

        Assert.AreEqual((Orientation.Horizontal, Orientation.Vertical), orientations);
    }

    [TestMethod]
    public void MovingTheThumb_SetsTheValue_ToAWholeNumber_AndRaisesChange()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.HScrollBar);
            var bar = new VBScrollBar(instance);
            var (window, sink) = ControlHost.Show(bar);
            // The thumb's track sets the bar's value as it is dragged.
            bar.Value = 41.6;
            var value = instance.Get(P.ScrollBarValue);
            window.Close();
            return (value, sink.Raised);
        });

        Assert.AreEqual((short)42, value);
        Expect.Lines(new[] { "HScroll1.Change" }, raised);
    }

    [TestMethod]
    public void AValueSetFromCode_MovesTheThumb_AndRaisesChange_ButTheSameValueDoesNot()
    {
        var (position, raised) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.VScrollBar);
            var bar = new VBScrollBar(instance);
            var (window, sink) = ControlHost.Show(bar);
            instance.Set(P.ScrollBarValue, (short)30);
            instance.Set(P.ScrollBarValue, (short)30);
            var position = bar.Value;
            window.Close();
            return (position, sink.Raised);
        });

        Assert.AreEqual(30d, position);
        Expect.Lines(new[] { "VScroll1.Change" }, raised);
    }

    [TestMethod]
    public void AMinGreaterThanItsMax_RunsTheBarTheOtherWay()
    {
        var (atMin, moved) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.HScrollBar, min: 100, max: 0, value: 100);
            var bar = new VBScrollBar(instance);
            var atMin = (bar.Minimum, bar.Maximum, bar.Value);
            bar.Value = 30;
            return (atMin, instance.Get(P.ScrollBarValue));
        });

        Assert.AreEqual((0d, 100d, 0d), atMin);
        Assert.AreEqual((short)70, moved);
    }

    [TestMethod]
    public void ItsArrow_MovesItBySmallChange_AndItsTrack_ByLargeChange()
    {
        var (afterArrow, afterTrack, raised) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.HScrollBar).Set(P.SmallChange, (short)3).Set(P.LargeChange, (short)20);
            var bar = new VBScrollBar(instance);
            var (window, sink) = ControlHost.Show((bar, new Rect(10, 10, 200, 17)));
            var buttons = bar.GetVisualDescendants().OfType<RepeatButton>().ToList();
            var arrow = buttons.Single(b => b.Name == "PART_LineDownButton");
            var track = buttons.Single(b => b.Name == "PART_PageDownButton");
            Click(window, arrow);
            var afterArrow = instance.Get(P.ScrollBarValue);
            Click(window, track);
            var afterTrack = instance.Get(P.ScrollBarValue);
            window.Close();
            return (afterArrow, afterTrack, sink.WithoutMoves);
        });

        Assert.AreEqual((short)3, afterArrow);
        Assert.AreEqual((short)23, afterTrack);
        Expect.Lines(new[] { "HScroll1.GotFocus", "HScroll1.Change", "HScroll1.Change" }, raised);
    }

    private static void Click(Window window, Control control)
    {
        var centre = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(centre, MouseButton.Left);
        window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
