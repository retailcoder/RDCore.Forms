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

    [TestMethod]
    public void DraggingTheThumb_RaisesScrollAtEachValue_ThenOneChange_AsItIsReleased()
    {
        var (raised, value) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.HScrollBar);
            var bar = new VBScrollBar(instance);
            var (window, sink) = ControlHost.Show((bar, new Rect(10, 10, 200, 17)));
            var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
            var at = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            foreach (var step in new[] { 10, 20, 30, 40 })
            {
                window.MouseMove(at + new Point(step, 0), RawInputModifiers.LeftMouseButton);
                Dispatcher.UIThread.RunJobs();
            }
            window.MouseUp(at + new Point(40, 0), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            var result = (sink.WithoutMoves.Where(e => !e.EndsWith("Focus", StringComparison.Ordinal)).ToList(), instance.Get(P.ScrollBarValue));
            window.Close();
            return result;
        });

        Assert.IsTrue(value > 0);
        Assert.AreEqual(4, raised.Count(e => e == "HScroll1.Scroll"), string.Join(", ", raised));
        Assert.AreEqual("HScroll1.Change", raised[^1]);
        Assert.AreEqual(1, raised.Count(e => e == "HScroll1.Change"));
    }

    [TestMethod]
    public void TheThumbPressedAndReleased_WithoutMoving_RaisesNeitherScrollNorChange()
    {
        var raised = UI.Run(() =>
        {
            var bar = new VBScrollBar(Bar(VBClasses.HScrollBar, value: 50));
            var (window, sink) = ControlHost.Show((bar, new Rect(10, 10, 200, 17)));
            var thumb = bar.GetVisualDescendants().OfType<Thumb>().Single();
            Click(window, thumb);
            var result = sink.WithoutMoves.Where(e => !e.EndsWith("Focus", StringComparison.Ordinal)).ToList();
            window.Close();
            return result;
        });

        Expect.Lines([], raised);
    }

    [TestMethod]
    public void WithTheFocus_TheArrowsMoveItBySmallChange_ThePageKeysByLargeChange_AndHomeAndEndToItsEnds()
    {
        var (values, changes) = UI.Run(() =>
        {
            var instance = Bar(VBClasses.HScrollBar).Set(P.SmallChange, (short)3).Set(P.LargeChange, (short)20);
            var bar = new VBScrollBar(instance);
            var (window, sink) = ControlHost.Show((bar, new Rect(10, 10, 200, 17)));
            bar.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            List<short> result = [];
            foreach (var (key, physical) in new[]
                     {
                         (Key.Right, PhysicalKey.ArrowRight), (Key.PageDown, PhysicalKey.PageDown), (Key.Left, PhysicalKey.ArrowLeft),
                         (Key.Down, PhysicalKey.ArrowDown), (Key.Up, PhysicalKey.ArrowUp), (Key.PageUp, PhysicalKey.PageUp),
                         (Key.End, PhysicalKey.End), (Key.Home, PhysicalKey.Home),
                     })
            {
                window.KeyPress(key, RawInputModifiers.None, physical, null);
                window.KeyRelease(key, RawInputModifiers.None, physical, null);
                Dispatcher.UIThread.RunJobs();
                result.Add(instance.Get(P.ScrollBarValue));
            }
            var after = (result, sink.Raised.Count(e => e == "HScroll1.Change"));
            window.Close();
            return after;
        });

        CollectionAssert.AreEqual(new short[] { 3, 23, 20, 23, 20, 0, 100, 0 }, values);
        Assert.AreEqual(8, changes);
    }

    private static void Click(Window window, Control control)
    {
        var centre = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(centre, MouseButton.Left);
        window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
