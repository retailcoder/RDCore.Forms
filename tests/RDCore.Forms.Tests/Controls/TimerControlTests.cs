using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Time.Testing;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The Timer counts on a clock the test moves, so no test waits for time to pass.
[TestClass]
public sealed class TimerControlTests
{
    private sealed record Shown(Window Window, Canvas Canvas, VBTimer Timer, ComponentInstance Instance, RecordingSink Sink, FakeTimeProvider Clock)
    {
        // Moves the clock on, and raises the ticks it brings.
        public int Advance(int milliseconds)
        {
            var before = Sink.Raised.Count;
            Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
            Dispatcher.UIThread.RunJobs();
            return Sink.Raised.Count - before;
        }
    }

    private static Shown Show(int interval, bool enabled = true)
    {
        var instance = new ComponentInstance(VBClasses.Timer, "Timer1").Set(P.Interval, interval).Set(P.Enabled, enabled);
        var timer = new VBTimer(instance);
        var canvas = new Canvas { Children = { timer } };
        var sink = new RecordingSink();
        var clock = new FakeTimeProvider();
        var window = new Window { Width = 200, Height = 100, Content = canvas };
        VBControl.SetEventSink(window, sink);
        VBControl.SetTimeProvider(window, clock);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return new Shown(window, canvas, timer, instance, sink, clock);
    }

    [TestMethod]
    public void ATimer_RaisesTimer_EachTimeItsIntervalPasses()
    {
        var (raised, ticks) = UI.Run(() =>
        {
            var shown = Show(1000);
            var ticks = new[] { shown.Advance(999), shown.Advance(1), shown.Advance(1000), shown.Advance(500) };
            shown.Window.Close();
            return (shown.Sink.Raised, ticks);
        });

        CollectionAssert.AreEqual(new[] { 0, 1, 1, 0 }, ticks);
        Expect.Lines(new[] { "Timer1.Timer", "Timer1.Timer" }, raised);
    }

    [TestMethod]
    public void TicksThatComeBeforeTheLastIsRaised_AreDropped()
    {
        var ticks = UI.Run(() =>
        {
            var shown = Show(100);
            var ticks = shown.Advance(1000);
            shown.Window.Close();
            return ticks;
        });

        Assert.AreEqual(1, ticks);
    }

    [TestMethod]
    public void ATickThatComesWhileTheTimerEventRuns_IsRaisedOnceItReturns()
    {
        var raised = UI.Run(() =>
        {
            var shown = Show(100);
            var calls = 0;
            shown.Sink.Handler = (_, _, _) =>
            {
                // The first event takes three intervals.
                if (calls++ == 0)
                    shown.Clock.Advance(TimeSpan.FromMilliseconds(300));
            };
            shown.Advance(100);
            shown.Window.Close();
            return shown.Sink.Raised.Count;
        });

        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public void ADisabledTimer_AndOneWhoseIntervalIs0_RaiseNothing()
    {
        var (disabled, zero) = UI.Run(() =>
        {
            var off = Show(100, enabled: false);
            var stopped = Show(0);
            var result = ((off.Advance(1000), off.Timer.IsRunning), (stopped.Advance(1000), stopped.Timer.IsRunning));
            off.Window.Close();
            stopped.Window.Close();
            return result;
        });

        Assert.AreEqual((0, false), disabled);
        Assert.AreEqual((0, false), zero);
    }

    [TestMethod]
    public void StoppingATimer_DropsATickNotYetRaised_AndStartingItAgain_CountsAWholeInterval()
    {
        var (pendingDropped, early, onTime) = UI.Run(() =>
        {
            var shown = Show(1000);
            shown.Clock.Advance(TimeSpan.FromMilliseconds(1000));
            shown.Instance.Set(P.Enabled, false);
            Dispatcher.UIThread.RunJobs();
            var pendingDropped = shown.Sink.Raised.Count;
            shown.Advance(500);
            shown.Instance.Set(P.Enabled, true);
            var early = shown.Advance(999);
            var onTime = shown.Advance(1);
            shown.Window.Close();
            return (pendingDropped, early, onTime);
        });

        Assert.AreEqual(0, pendingDropped);
        Assert.AreEqual(0, early);
        Assert.AreEqual(1, onTime);
    }

    [TestMethod]
    public void ANewInterval_StartsTheIntervalAgain()
    {
        var ticks = UI.Run(() =>
        {
            var shown = Show(1000);
            shown.Advance(600);
            shown.Instance.Set(P.Interval, 800);
            var ticks = new[] { shown.Advance(799), shown.Advance(1) };
            shown.Window.Close();
            return ticks;
        });

        CollectionAssert.AreEqual(new[] { 0, 1 }, ticks);
    }

    [TestMethod]
    public void ATimerTakenOffItsForm_Stops_AndPutBack_RunsAgain()
    {
        var (off, offRunning, back) = UI.Run(() =>
        {
            var shown = Show(100);
            shown.Canvas.Children.Remove(shown.Timer);
            var off = shown.Advance(1000);
            var offRunning = shown.Timer.IsRunning;
            shown.Canvas.Children.Add(shown.Timer);
            var back = shown.Advance(100);
            shown.Window.Close();
            return (off, offRunning, back);
        });

        Assert.AreEqual(0, off);
        Assert.IsFalse(offRunning);
        Assert.AreEqual(1, back);
    }

    [TestMethod]
    public void ATimer_DrawsNothing_AndTakesNoFocus()
    {
        var (visible, focusable, hit) = UI.Run(() =>
        {
            var shown = Show(100);
            var result = (shown.Timer.IsVisible, shown.Timer.Focusable, shown.Timer.IsHitTestVisible);
            shown.Window.Close();
            return result;
        });

        Assert.IsFalse(visible);
        Assert.IsFalse(focusable);
        Assert.IsFalse(hit);
    }
}
