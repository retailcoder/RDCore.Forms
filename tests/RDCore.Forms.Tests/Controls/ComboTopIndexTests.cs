using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A ComboBox's TopIndex, as measured with VB6 (native and p-code agree), with 30 items: a Dropdown Combo's and a Dropdown List's list
// shows 8 rows, so that its last top is 22, and a Simple Combo 1800 twips high shows 7, its last top 23.
[TestClass]
public sealed class ComboTopIndexTests
{
    private static ComponentInstance Combo(ComboBoxStyle style, int count) =>
        new ComponentInstance(ComponentClasses.ComboBox, "Combo1").Set(P.ComboBoxStyle, style)
            .Set(P.List, Enumerable.Range(0, count).Select(i => $"item{i:00}").ToArray());

    private static (Window Window, RecordingSink Sink, RDFormsComboBox Combo) Show(ComboBoxStyle style, int count = 30)
    {
        var combo = new RDFormsComboBox(Combo(style, count));
        var (window, sink) = ControlHost.Show((combo, new Rect(10, 5, 120, style == ComboBoxStyle.SimpleCombo ? 120 : 21)));
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return (window, sink, combo);
    }

    private static int LastTop(ComboBoxStyle style) => style == ComboBoxStyle.SimpleCombo ? 23 : 22;

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    [DataRow(ComboBoxStyle.SimpleCombo)]
    public void ATopIndexFromCode_IsTheLastTopAtMost_AndRaisesScrollWhereTheListScrolls(ComboBoxStyle style)
    {
        var (tops, raised) = UI.Run(() =>
        {
            var (window, sink, combo) = Show(style);
            var tops = new List<int>();
            foreach (var top in new[] { 5, 0, 22, 25, 29 })
            {
                combo.TopIndex = top;
                tops.Add(combo.TopIndex);
            }
            window.Close();
            return (tops, sink.Raised.Count(line => line == "Combo1.Scroll"));
        });

        var last = LastTop(style);
        CollectionAssert.AreEqual(new[] { 5, 0, 22, last, last }, tops);
        Assert.AreEqual(style == ComboBoxStyle.SimpleCombo ? 4 : 3, raised);
    }

    [TestMethod]
    [DataRow(30, -1, 380)]
    [DataRow(30, 30, 380)]
    [DataRow(30, 29, 0)]
    [DataRow(0, 0, 380)]
    public void ATopIndexOutsideTheItems_Is380_AndAnEmptyCombosEvery380(int count, int top, int error)
    {
        var number = UI.Run(() =>
        {
            var (window, _, combo) = Show(ComboBoxStyle.DropdownList, count);
            var number = 0;
            try
            {
                combo.TopIndex = top;
            }
            catch (FormErrorException e)
            {
                number = e.Error.Number;
            }
            window.Close();
            return number;
        });

        Assert.AreEqual(error, number);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    [DataRow(ComboBoxStyle.SimpleCombo)]
    public void ASelectionSet_PutsTheSelectedItemAtTheTopRow_AndRaisesNoScroll(ComboBoxStyle style)
    {
        var (tops, raised) = UI.Run(() =>
        {
            var (window, sink, combo) = Show(style);
            var tops = new List<int>();
            var scrolled = new List<int>();
            void From(int top, int selected)
            {
                combo.ListIndex = selected;
                combo.TopIndex = top;
                sink.Raised.Clear();
            }
            void Read()
            {
                tops.Add(combo.TopIndex);
                if (sink.Raised.Contains("Combo1.Scroll"))
                    scrolled.Add(tops.Count);
                sink.Raised.Clear();
            }
            From(5, -1);
            combo.ListIndex = 7;
            Read();
            combo.ListIndex = 25;
            Read();
            combo.ListIndex = 3;
            Read();
            combo.ListIndex = -1;
            Read();
            // An item added before the selected one, and at it, moves it down: it is set again. One added after it moves nothing.
            From(5, 10);
            combo.AddItem("new", 0);
            Read();
            combo.RemoveItem(0);
            From(5, 2);
            combo.AddItem("new", 2);
            Read();
            combo.RemoveItem(2);
            From(5, 10);
            combo.AddItem("new", 20);
            Read();
            combo.RemoveItem(20);
            // Any item's List sets the selection again; with none, the top is item 0.
            From(5, 0);
            combo.SetList(10, "x");
            Read();
            From(5, -1);
            combo.SetList(0, "x");
            Read();
            // An item removed before the selected one leaves the top.
            From(5, 3);
            combo.RemoveItem(0);
            Read();
            window.Close();
            return (tops, scrolled);
        });

        var last = LastTop(style);
        CollectionAssert.AreEqual(new[] { 7, last, 3, 3, 11, 3, 5, 0, 0, 5 }, tops);
        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void ADropdownListsText_PutsItsItemAtTheTopRow_EvenWhereItIsSelected()
    {
        var tops = UI.Run(() =>
        {
            var (window, _, combo) = Show(ComboBoxStyle.DropdownList);
            combo.TopIndex = 5;
            combo.SetText("item25");
            var first = combo.TopIndex;
            combo.TopIndex = 0;
            combo.SetText("item25");
            var again = combo.TopIndex;
            window.Close();
            return (first, again);
        });

        Assert.AreEqual((22, 22), tops);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.SimpleCombo)]
    public void AnEditBoxsText_SelectsNothing_AndLeavesTheTop(ComboBoxStyle style)
    {
        var top = UI.Run(() =>
        {
            var (window, _, combo) = Show(style);
            combo.TopIndex = 5;
            combo.SetText("item25");
            var top = combo.TopIndex;
            window.Close();
            return top;
        });

        Assert.AreEqual(5, top);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo, 6)]
    [DataRow(ComboBoxStyle.DropdownList, 6)]
    [DataRow(ComboBoxStyle.SimpleCombo, 7)]
    public void TheDownKey_BringsTheItemItSelectsIntoView_AtTheLastRow_AndRaisesScrollAfterItsClick(ComboBoxStyle style, int expected)
    {
        var (index, top, raised) = UI.Run(() =>
        {
            var (window, sink, combo) = Show(style);
            combo.ListIndex = 12;
            combo.TopIndex = 5;
            combo.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();
            var result = (combo.ListIndex, combo.TopIndex, sink.Raised.Where(line => line is "Combo1.Click" or "Combo1.Scroll").ToArray());
            window.Close();
            return result;
        });

        Assert.AreEqual(13, index);
        Assert.AreEqual(expected, top);
        Expect.Lines(new[] { "Combo1.Click", "Combo1.Scroll" }, raised);
    }

    [TestMethod]
    public void ASimpleCombosList_IsScrolledToTheTop_AndItsScrollBarRaisesScroll()
    {
        var (placed, top, raised) = UI.Run(() =>
        {
            var (window, sink, combo) = Show(ComboBoxStyle.SimpleCombo);
            combo.TopIndex = 5;
            Dispatcher.UIThread.RunJobs();
            var viewer = combo.GetVisualDescendants().OfType<ScrollViewer>().First(v => v.Name != "PART_ContentHost"
                && v.FindAncestorOfType<TextBox>() is null);
            var placed = viewer.Offset.Y;
            sink.Raised.Clear();
            var bar = viewer.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.ScrollBar>()
                .Single(b => b.Orientation == Orientation.Vertical);
            bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_LineDownButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            var result = (placed, combo.TopIndex, sink.Raised.ToArray());
            window.Close();
            return result;
        });

        Assert.AreEqual(5 * 13d, placed);
        Assert.AreEqual(6, top);
        Expect.Lines(new[] { "Combo1.Scroll" }, raised);
    }
}
