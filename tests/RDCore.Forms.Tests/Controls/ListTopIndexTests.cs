using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

// A ListBox's TopIndex, as measured with VB6 (native and p-code agree): a list of 30 items, 2010 twips high, shows 10 rows of 13 pixels,
// so that its last top is 20.
[TestClass]
public sealed class ListTopIndexTests
{
    private const double TenRows = 134;

    private static ComponentInstance List(int count) =>
        new ComponentInstance(ComponentClasses.ListBox, "List1").Set(P.List, Enumerable.Range(0, count).Select(i => $"item{i:00}").ToArray());

    private static (Window Window, RecordingSink Sink, RDFormsListBox List) Show(int count = 30)
    {
        var list = new RDFormsListBox(List(count));
        var (window, sink) = ControlHost.Show((list, new Rect(10, 5, 120, TenRows)));
        Dispatcher.UIThread.RunJobs();
        return (window, sink, list);
    }

    private static ScrollViewer Viewer(RDFormsListBox list) => list.GetVisualDescendants().OfType<ScrollViewer>().First();

    private static string[] ClicksAndScrolls(RecordingSink sink) =>
        [.. sink.Raised.Where(line => line is "List1.Click" or "List1.Scroll")];

    [TestMethod]
    public void ATopIndexFromCode_ScrollsTheList_TheLastTopAtMost_AndRaisesScrollWhereTheListScrolls()
    {
        var (tops, offset, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            var tops = new List<int>();
            foreach (var top in new[] { 5, 0, 22, 25, 29, 0 })
            {
                list.TopIndex = top;
                tops.Add(list.TopIndex);
            }
            list.TopIndex = 20;
            Dispatcher.UIThread.RunJobs();
            var offset = Viewer(list).Offset.Y;
            window.Close();
            return (tops, offset, sink.Raised);
        });

        CollectionAssert.AreEqual(new[] { 5, 0, 20, 20, 20, 0 }, tops);
        Assert.AreEqual(20 * 13d, offset);
        Expect.Lines(new[] { "List1.Scroll", "List1.Scroll", "List1.Scroll", "List1.Scroll", "List1.Scroll" }, raised);
    }

    [TestMethod]
    [DataRow(30, -1, 380)]
    [DataRow(30, 30, 380)]
    [DataRow(30, 1000, 380)]
    [DataRow(30, 29, 0)]
    [DataRow(3, 2, 0)]
    [DataRow(3, 3, 380)]
    [DataRow(0, 0, 0)]
    [DataRow(0, 1, 380)]
    public void ATopIndexOutsideTheItems_Is380_AndAnEmptyListTakes0(int count, int top, int error)
    {
        var (number, shown) = UI.Run(() =>
        {
            var (window, _, list) = Show(count);
            var number = 0;
            try
            {
                list.TopIndex = top;
            }
            catch (FormErrorException e)
            {
                number = e.Error.Number;
            }
            var shown = list.TopIndex;
            window.Close();
            return (number, shown);
        });

        Assert.AreEqual(error, number);
        Assert.AreEqual(count == 30 && error == 0 ? 20 : 0, shown);
    }

    [TestMethod]
    public void ListIndexAndText_BringTheirItemIntoView_FromAboveAtTheTopRow_AndFromBelowAtTheLast_AndRaiseNoScroll()
    {
        var (tops, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            list.TopIndex = 5;
            sink.Raised.Clear();
            var tops = new List<int>();
            void Read() => tops.Add(list.TopIndex);
            list.ListIndex = 7;
            Read();
            list.ListIndex = 25;
            Read();
            list.ListIndex = 3;
            Read();
            list.ListIndex = -1;
            Read();
            list.SetText("item25");
            Read();
            list.TopIndex = 0;
            sink.Raised.Clear();
            // Text selects its item again, and brings it into view; ListIndex set to the item it is moves nothing.
            list.ListIndex = 25;
            Read();
            list.SetText("item25");
            Read();
            window.Close();
            return (tops, sink.Raised);
        });

        CollectionAssert.AreEqual(new[] { 5, 16, 3, 3, 16, 0, 16 }, tops);
        Assert.IsFalse(raised.Contains("List1.Scroll"), string.Join(", ", raised));
    }

    [TestMethod]
    public void ItemsAddedAndRemoved_LeaveTheTopWhereItIs_TheLastTopAtMost()
    {
        var (tops, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            list.TopIndex = 5;
            var tops = new List<int>();
            list.ListIndex = 10;
            list.AddItem("new", 0);
            tops.Add(list.TopIndex);
            list.RemoveItem(0);
            tops.Add(list.TopIndex);
            list.SetList(0, "x");
            tops.Add(list.TopIndex);
            list.TopIndex = 20;
            sink.Raised.Clear();
            list.RemoveItem(0);
            tops.Add(list.TopIndex);
            list.RemoveItem(0);
            tops.Add(list.TopIndex);
            list.Clear();
            tops.Add(list.TopIndex);
            window.Close();
            return (tops, sink.Raised);
        });

        CollectionAssert.AreEqual(new[] { 5, 5, 5, 19, 18, 0 }, tops);
        Assert.IsFalse(raised.Contains("List1.Scroll"), string.Join(", ", raised));
    }

    [TestMethod]
    public void TheDownKeyFromTheLastRowInView_BringsTheNextItemIntoView_AndRaisesScrollAfterItsClick()
    {
        var (index, top, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            list.TopIndex = 5;
            list.ListIndex = 14;
            list.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();
            var result = (list.ListIndex, list.TopIndex, ClicksAndScrolls(sink));
            window.Close();
            return result;
        });

        Assert.AreEqual(15, index);
        Assert.AreEqual(6, top);
        Expect.Lines(new[] { "List1.Click", "List1.Scroll" }, raised);
    }

    [TestMethod]
    public void TheScrollBarsLineDown_MovesTheTop_AndRaisesScroll_OnceTheListHasScrolled()
    {
        var (top, offset, before, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            list.TopIndex = 5;
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            var bar = list.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical);
            var down = bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_LineDownButton");
            down.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var before = sink.Raised.Count;
            Dispatcher.UIThread.RunJobs();
            var result = (list.TopIndex, Viewer(list).Offset.Y, before, sink.Raised.ToArray());
            window.Close();
            return result;
        });

        Assert.AreEqual(6, top);
        // A line is an item: the list ends its scroll on a whole row.
        Assert.AreEqual(6 * 13d, offset);
        Assert.AreEqual(0, before);
        Expect.Lines(new[] { "List1.Scroll" }, raised);
    }

    [TestMethod]
    public void TheWheel_MovesTheTop_AndRaisesNoScroll()
    {
        var (top, raised) = UI.Run(() =>
        {
            var (window, sink, list) = Show();
            list.TopIndex = 5;
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            var at = list.TranslatePoint(new Point(40, 40), window) ?? default;
            window.MouseWheel(at, new Vector(0, -1));
            Dispatcher.UIThread.RunJobs();
            var result = (list.TopIndex, sink.Raised.ToArray());
            window.Close();
            return result;
        });

        Assert.IsGreaterThan(5, top);
        Assert.IsFalse(raised.Contains("List1.Scroll"), string.Join(", ", raised));
    }
}
