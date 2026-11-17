using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class SpinButtonControlTests
{
    // Shown at (10, 5), 120 wide and 30 tall: its arrows are side by side, the down arrow on the left and the up arrow on the right.
    private static readonly Point OnTheUpArrow = new(100, 20);
    private static readonly Point OnTheDownArrow = new(40, 20);

    private static ComponentInstance Spin(int min = 0, int max = 100, int value = 0, int smallChange = 1) =>
        new ComponentInstance(ComponentClasses.SpinButton, "SpinButton1")
            .Set(P.SpinButtonMin, min).Set(P.SpinButtonMax, max).Set(P.SpinButtonValue, value).Set(P.SpinButtonSmallChange, smallChange);

    private static void Click(Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
    }

    [TestMethod]
    public void TheUpArrow_RaisesSpinUp_ThenStepsTheValueTowardMax_AndRaisesChange()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = Spin(value: 10, smallChange: 5);
            var (window, sink) = ControlHost.Show(new RDFormsSpinButton(instance));
            Click(window, OnTheUpArrow);
            window.Close();
            return (instance.Get(P.SpinButtonValue), sink.Raised.Where(e => !e.EndsWith("Focus")).ToList());
        });

        Assert.AreEqual(15, value);
        Expect.Lines(new[] { "SpinButton1.SpinUp", "SpinButton1.Change" }, raised);
    }

    [TestMethod]
    public void TheDownArrow_RaisesSpinDown_ThenStepsTheValueTowardMin()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = Spin(value: 10, smallChange: 3);
            var (window, sink) = ControlHost.Show(new RDFormsSpinButton(instance));
            Click(window, OnTheDownArrow);
            window.Close();
            return (instance.Get(P.SpinButtonValue), sink.Raised.Where(e => !e.EndsWith("Focus")).ToList());
        });

        Assert.AreEqual(7, value);
        Expect.Lines(new[] { "SpinButton1.SpinDown", "SpinButton1.Change" }, raised);
    }

    [TestMethod]
    public void AStepPastAnEnd_StopsAtTheEnd_AndAtTheEnd_ItRaisesSpinUpButNoChange()
    {
        var (values, raised) = UI.Run(() =>
        {
            var instance = Spin(max: 10, value: 8, smallChange: 5);
            var (window, sink) = ControlHost.Show(new RDFormsSpinButton(instance));
            Click(window, OnTheUpArrow);
            var first = instance.Get(P.SpinButtonValue);
            Click(window, OnTheUpArrow);
            window.Close();
            return ((first, instance.Get(P.SpinButtonValue)), sink.Raised.Where(e => !e.EndsWith("Focus")).ToList());
        });

        Assert.AreEqual((10, 10), values);
        Expect.Lines(new[] { "SpinButton1.SpinUp", "SpinButton1.Change", "SpinButton1.SpinUp" }, raised);
    }

    [TestMethod]
    public void AMinGreaterThanItsMax_MovesTheValueTheOtherWay()
    {
        var value = UI.Run(() =>
        {
            var instance = Spin(min: 100, max: 0, value: 50, smallChange: 10);
            var (window, _) = ControlHost.Show(new RDFormsSpinButton(instance));
            Click(window, OnTheUpArrow);
            window.Close();
            return instance.Get(P.SpinButtonValue);
        });

        Assert.AreEqual(40, value);
    }

    [TestMethod]
    public void AValueFromCode_RaisesChange_OnlyWhenItChanges()
    {
        var raised = UI.Run(() =>
        {
            var instance = Spin();
            var (window, sink) = ControlHost.Show(new RDFormsSpinButton(instance));
            instance.Set(P.SpinButtonValue, 30);
            instance.Set(P.SpinButtonValue, 30);
            window.Close();
            return sink.Raised;
        });

        Expect.Lines(new[] { "SpinButton1.Change" }, raised);
    }

    [TestMethod]
    [DataRow(ControlOrientation.Auto, 120d, 30d, true, DisplayName = "Auto, wider than tall: side by side")]
    [DataRow(ControlOrientation.Auto, 20d, 60d, false, DisplayName = "Auto, taller than wide: one above the other")]
    [DataRow(ControlOrientation.Vertical, 120d, 30d, false, DisplayName = "Vertical")]
    [DataRow(ControlOrientation.Horizontal, 20d, 60d, true, DisplayName = "Horizontal")]
    public void ItsArrows_AreSideBySideOrOneAboveTheOther_ByItsOrientation(ControlOrientation orientation, double width, double height, bool sideBySide)
    {
        var (horizontal, upCell, downCell) = UI.Run(() =>
        {
            var spin = new RDFormsSpinButton(Spin().Set(P.Orientation, orientation).Set(P.Width, width).Set(P.Height, height));
            return (spin.IsHorizontal, (Grid.GetRow(spin.UpArrow), Grid.GetColumn(spin.UpArrow)), (Grid.GetRow(spin.DownArrow), Grid.GetColumn(spin.DownArrow)));
        });

        Assert.AreEqual(sideBySide, horizontal);
        Assert.AreEqual(sideBySide ? (0, 1) : (0, 0), upCell);
        Assert.AreEqual(sideBySide ? (0, 0) : (1, 0), downCell);
    }

    [TestMethod]
    public void ItsDelay_IsTheRepeatOfAHeldArrow_TheFirstRepeatFiveTimesLater()
    {
        var (delay, interval) = UI.Run(() =>
        {
            var spin = new RDFormsSpinButton(Spin().Set(P.Delay, 40));
            return (spin.UpArrow.Delay, spin.UpArrow.Interval);
        });

        Assert.AreEqual((200, 40), (delay, interval));
    }

    [TestMethod]
    public void WithTheFocus_TheUpKey_SpinsItUp_AndItsKeyEventsAreRaised()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = Spin(value: 1);
            var spin = new RDFormsSpinButton(instance);
            var (window, sink) = ControlHost.Show(spin);
            spin.Focus();
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Dispatcher.UIThread.RunJobs();
            window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
            Dispatcher.UIThread.RunJobs();
            window.Close();
            return (instance.Get(P.SpinButtonValue), sink.Raised.Where(e => !e.EndsWith("Focus")).ToList());
        });

        Assert.AreEqual(2, value);
        Expect.Lines(new[] { "SpinButton1.KeyDown(Integer 38, Integer 0)", "SpinButton1.SpinUp", "SpinButton1.Change", "SpinButton1.KeyUp(Integer 38, Integer 0)" }, raised);
    }

    [TestMethod]
    public void TheFactory_MakesASpinButtonsView()
    {
        var made = UI.Run(() => RDFormsControls.Create(new ComponentInstance(ComponentClasses.SpinButton, "SpinButton1")));

        Assert.IsInstanceOfType<RDFormsSpinButton>(made);
    }
}
