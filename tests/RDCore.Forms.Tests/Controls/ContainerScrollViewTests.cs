using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Frame's and a form's inside, scrolled by ScrollLeft and ScrollTop, and the Scroll each raises.
[TestClass]
public sealed class ContainerScrollViewTests
{
    private static ComponentInstance Frame(ScrollBars bars = ScrollBars.Vertical) =>
        new ComponentInstance(ComponentClasses.Frame, "Frame1").Set(P.Caption, "Frame1")
            .Set(P.ContainerScrollBars, bars).Set(P.ScrollHeight, 400d).Set(P.ScrollWidth, 300d);

    private static RDFormsFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new RDFormsFormWindow(new FormDefinition(root));
        RDFormsControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    [TestMethod]
    public void SettingScrollTop_ScrollsTheFrame_AndRaisesScroll_AsAPropertyChange()
    {
        var (offset, shownAt, raised) = UI.Run(() =>
        {
            var instance = Frame();
            var frame = new RDFormsFrame(instance);
            // A control the Frame holds, 120 pixels down.
            var held = new Border { Width = 10, Height = 10 };
            Canvas.SetLeft(held, 20);
            Canvas.SetTop(held, 120);
            frame.ChildHost.Children.Add(held);
            var (window, sink) = ControlHost.Show((frame, new Rect(10, 5, 120, 100)));
            instance.Set(P.ScrollTop, 50d);
            frame.UpdateLayout();
            var offset = frame.Scroller.Offset;
            var shownAt = held.TranslatePoint(default, frame);
            window.Close();
            return (offset, shownAt, sink.Raised);
        });

        Assert.AreEqual(new Vector(0, 50), offset);
        Assert.AreEqual(new Point(20, 70), shownAt);
        Expect.Lines(new[] { "Frame1.Scroll(Long 0, Long 8, Single 0, Single 50, Single 0, Single 50)" }, raised);
    }

    [TestMethod]
    public void AScrollTopPastTheEnd_ScrollsToTheEnd()
    {
        var (offset, raised) = UI.Run(() =>
        {
            var instance = Frame();
            var frame = new RDFormsFrame(instance);
            var (window, sink) = ControlHost.Show((frame, new Rect(10, 5, 120, 100)));
            instance.Set(P.ScrollTop, 1000d);
            var offset = frame.Scroller.Offset;
            window.Close();
            return (offset, sink.Raised);
        });

        // 400 to scroll, 100 shown.
        Assert.AreEqual(new Vector(0, 300), offset);
        Expect.Lines(new[] { "Frame1.Scroll(Long 0, Long 8, Single 0, Single 1000, Single 0, Single 300)" }, raised);
    }

    [TestMethod]
    public void SettingScrollLeft_ScrollsAcross()
    {
        var (offset, raised) = UI.Run(() =>
        {
            var instance = Frame(ScrollBars.Both);
            var frame = new RDFormsFrame(instance);
            var (window, sink) = ControlHost.Show((frame, new Rect(10, 5, 120, 100)));
            instance.Set(P.ScrollLeft, 40d);
            var offset = frame.Scroller.Offset;
            window.Close();
            return (offset, sink.Raised);
        });

        Assert.AreEqual(new Vector(40, 0), offset);
        Expect.Lines(new[] { "Frame1.Scroll(Long 8, Long 0, Single 40, Single 0, Single 40, Single 0)" }, raised);
    }

    [TestMethod]
    public void TheUsersScroll_SetsScrollTop_AndRaisesScroll_OnceItIsDone()
    {
        var (top, before, raised) = UI.Run(() =>
        {
            var instance = Frame();
            var frame = new RDFormsFrame(instance);
            var (window, sink) = ControlHost.Show((frame, new Rect(10, 5, 120, 100)));
            instance.Set(P.ScrollTop, 50d);
            sink.Raised.Clear();
            // The mouse wheel sets the viewer's offset, a line at a time.
            frame.Scroller.Offset = new Vector(0, 30);
            var before = sink.Raised.Count;
            Dispatcher.UIThread.RunJobs();
            window.Close();
            return (instance.Get(P.ScrollTop), before, sink.Raised);
        });

        Assert.AreEqual(30d, top);
        Assert.AreEqual(0, before);
        Expect.Lines(new[] { "Frame1.Scroll(Long 0, Long 1, Single 0, Single -20, Single 0, Single -20)" }, raised);
    }

    [TestMethod]
    [DataRow(ScrollBars.None, ScrollBars.Both, ScrollBarVisibility.Hidden, ScrollBarVisibility.Hidden)]
    [DataRow(ScrollBars.Vertical, ScrollBars.Both, ScrollBarVisibility.Hidden, ScrollBarVisibility.Visible)]
    [DataRow(ScrollBars.Both, ScrollBars.Both, ScrollBarVisibility.Visible, ScrollBarVisibility.Visible)]
    [DataRow(ScrollBars.Both, ScrollBars.None, ScrollBarVisibility.Auto, ScrollBarVisibility.Auto)]
    [DataRow(ScrollBars.Both, ScrollBars.Horizontal, ScrollBarVisibility.Visible, ScrollBarVisibility.Auto)]
    public void TheBars_ShowAsScrollBarsAndKeepScrollBarsVisibleSay(ScrollBars bars, ScrollBars kept, ScrollBarVisibility across, ScrollBarVisibility down)
    {
        var shown = UI.Run(() =>
        {
            var instance = Frame(bars).Set(P.KeepScrollBarsVisible, kept);
            var frame = new RDFormsFrame(instance);
            return (frame.Scroller.HorizontalScrollBarVisibility, frame.Scroller.VerticalScrollBarVisibility);
        });

        Assert.AreEqual((across, down), shown);
    }

    [TestMethod]
    public void SettingScrollTop_ScrollsTheFormsClientArea_WhichKeepsTheFormsSize()
    {
        var (offset, size, raised) = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d)
                .Set(P.ContainerScrollBars, ScrollBars.Both).Set(P.ScrollHeight, 600d);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            root.Set(P.ScrollTop, 100d);
            Dispatcher.UIThread.RunJobs();
            var offset = window.Scroller.Offset;
            var size = window.Scroller.Bounds.Size;
            var raised = sink.Raised.Where(e => e.Contains(".Scroll(")).ToList();
            window.Close();
            return (offset, size, raised);
        });

        Assert.AreEqual(new Vector(0, 100), offset);
        Assert.AreEqual(new Size(320, 240), size);
        Expect.Lines(new[] { "Form1.Scroll(Long 0, Long 8, Single 0, Single 100, Single 0, Single 100)" }, raised);
    }

    [TestMethod]
    public void AFormWithNothingToScroll_ScrollsNot()
    {
        var (offset, raised) = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            root.Set(P.ScrollTop, 100d);
            var offset = window.Scroller.Offset;
            var raised = sink.Raised.Where(e => e.Contains(".Scroll(")).ToList();
            window.Close();
            return (offset, raised);
        });

        Assert.AreEqual(default, offset);
        Assert.IsEmpty(raised);
    }
}
