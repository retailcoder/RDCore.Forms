using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ToggleButtonControlTests
{
    private static void Click(global::Avalonia.Controls.Window window)
    {
        window.MouseDown(new Point(20, 10), MouseButton.Left);
        window.MouseUp(new Point(20, 10), MouseButton.Left);
    }

    [TestMethod]
    public void AClick_PutsItDown_AndRaisesChangeThenClick_BeforeMouseUp()
    {
        var (value, shown, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1");
            var button = new RDFormsToggleButton(instance);
            var (window, sink) = ControlHost.Show(button);
            Click(window);
            var shown = button.IsChecked;
            window.Close();
            return (instance.Get(P.ToggleButtonValue), shown, sink.WithoutMoves);
        });

        Assert.IsTrue(value);
        Assert.IsTrue(shown);
        Expect.Lines(new[]
        {
            "ToggleButton1.GotFocus",
            "ToggleButton1.MouseDown(Integer 1, Integer 0, Single 150, Single 75)",
            "ToggleButton1.Change",
            "ToggleButton1.Click",
            "ToggleButton1.MouseUp(Integer 1, Integer 0, Single 150, Single 75)",
        }, raised);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "TripleState False: down, up, down")]
    [DataRow(true, DisplayName = "TripleState True: down, Null, up")]
    public void EachClick_TakesItToItsNextValue(bool tripleState)
    {
        var values = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1").Set(P.TripleState, tripleState);
            var (window, _) = ControlHost.Show(new RDFormsToggleButton(instance));
            var values = new List<bool?>();
            for (var i = 0; i < 3; i++)
            {
                Click(window);
                values.Add(instance.Get(P.ToggleButtonValue));
            }
            window.Close();
            return values;
        });

        CollectionAssert.AreEqual(tripleState ? new bool?[] { true, null, false } : new bool?[] { true, false, true }, values);
    }

    [TestMethod]
    public void AValueFromCode_ShowsInTheButton_AndRaisesItsEvents_OnlyWhenItChanges_ANullOneDrawnUp()
    {
        var (down, nullShown, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1");
            var button = new RDFormsToggleButton(instance);
            var (window, sink) = ControlHost.Show(button);
            instance.Set(P.ToggleButtonValue, true);
            instance.Set(P.ToggleButtonValue, true);
            var down = button.IsChecked;
            instance.Set(P.ToggleButtonValue, null);
            var nullShown = button.IsChecked;
            window.Close();
            return (down, nullShown, sink.Raised);
        });

        Assert.IsTrue(down);
        Assert.IsFalse(nullShown);
        Expect.Lines(new[] { "ToggleButton1.Change", "ToggleButton1.Click", "ToggleButton1.Change", "ToggleButton1.Click" }, raised);
    }

    [TestMethod]
    public void ALockedButton_KeepsItsValueThroughAClick()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1").Set(P.Locked, true);
            var (window, sink) = ControlHost.Show(new RDFormsToggleButton(instance));
            Click(window);
            window.Close();
            return (instance.Get(P.ToggleButtonValue), sink.WithoutMoves);
        });

        Assert.IsFalse(value);
        Assert.IsFalse(raised.Any(e => e.EndsWith(".Change") || e.EndsWith(".Click")), string.Join(", ", raised));
    }

    [TestMethod]
    public void TheFactory_MakesAToggleButtonsView()
    {
        var made = UI.Run(() => RDFormsControls.Create(new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1")));

        Assert.IsInstanceOfType<RDFormsToggleButton>(made);
    }
}
