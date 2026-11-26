using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Dropdown Combo's and a Dropdown List's open list, as measured with VB6 (native and p-code agree), on a combo 120 pixels wide: rows
// of a line of its font, 13 pixels at the default font and 20 at MS Sans Serif 12, 8 at most, one where it has no item, in a border of
// a pixel on each side.
[TestClass]
public sealed class ComboDropDownTests
{
    private static (Window Window, RecordingSink Sink, RDFormsComboBox Combo) Show(ComboBoxStyle style, int count = 30)
    {
        var instance = new ComponentInstance(ComponentClasses.ComboBox, "Combo1").Set(P.ComboBoxStyle, style)
            .Set(P.List, Enumerable.Range(0, count).Select(i => $"item{i:00}").ToArray());
        var combo = new RDFormsComboBox(instance);
        var (window, sink) = ControlHost.Show((combo, new Rect(10, 5, 120, 21)));
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return (window, sink, combo);
    }

    private static void Drop(RDFormsComboBox combo, bool open)
    {
        combo.IsDropDownOpen = open;
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // The open list: the popup's border, and the viewer of its rows.
    private static Border List(RDFormsComboBox combo) => (Border)combo.GetVisualDescendants().OfType<Popup>().Single().Child!;

    private static ScrollViewer Viewer(RDFormsComboBox combo) => (ScrollViewer)List(combo).Child!;

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo, 0, 15)]
    [DataRow(ComboBoxStyle.DropdownCombo, 1, 15)]
    [DataRow(ComboBoxStyle.DropdownCombo, 3, 41)]
    [DataRow(ComboBoxStyle.DropdownCombo, 8, 106)]
    [DataRow(ComboBoxStyle.DropdownCombo, 9, 106)]
    [DataRow(ComboBoxStyle.DropdownList, 0, 15)]
    [DataRow(ComboBoxStyle.DropdownList, 3, 41)]
    [DataRow(ComboBoxStyle.DropdownList, 30, 106)]
    public void ADroppedList_ShowsARowOfALineForEachItem_EightAtMost_AndOneWhereItHasNone(ComboBoxStyle style, int count, int height)
    {
        var (list, row, text, scrolls) = UI.Run(() =>
        {
            var (window, _, combo) = Show(style, count);
            Drop(combo, true);
            var viewer = Viewer(combo);
            var item = combo.ContainerFromIndex(0);
            // Where the item's text is drawn in its row, which takes no more than 2 pixels on the left, as a Simple Combo's.
            var text = item?.GetVisualDescendants().OfType<TextBlock>().First().TranslatePoint(default, item);
            var result = (List(combo).Bounds.Size, item?.Bounds.Height, text, viewer.Extent.Height > viewer.Viewport.Height);
            window.Close();
            return result;
        });

        Assert.AreEqual(new Size(120, height), list);
        Assert.AreEqual(count == 0 ? null : 13d, row);
        Assert.AreEqual(count == 0 ? null : new Point(2, 0), text);
        Assert.AreEqual(count > 8, scrolls);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo, 30, 162)]
    [DataRow(ComboBoxStyle.DropdownList, 3, 62)]
    public void ADroppedListAfterAFontFromCode_ShowsRowsOfTheFontsLine(ComboBoxStyle style, int count, int height)
    {
        var (list, row) = UI.Run(() =>
        {
            var (window, _, combo) = Show(style, count);
            // Opened once first, so that its rows are there before the font.
            Drop(combo, true);
            Drop(combo, false);
            RDFormsControl.GetInstance(combo)!.Set(P.Font, new FormFont("MS Sans Serif", 12));
            Dispatcher.UIThread.RunJobs();
            Drop(combo, true);
            var result = (List(combo).Bounds.Height, combo.ContainerFromIndex(0)!.Bounds.Height);
            window.Close();
            return result;
        });

        Assert.AreEqual((height, 20d), ((int)list, row));
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    public void AnItemWiderThanTheCombo_LeavesTheDroppedListAsWideAsTheCombo(ComboBoxStyle style)
    {
        var width = UI.Run(() =>
        {
            var (window, _, combo) = Show(style, 3);
            combo.AddItem(new string('W', 60));
            Drop(combo, true);
            var result = List(combo).Bounds.Width;
            window.Close();
            return result;
        });

        Assert.AreEqual(120d, width);
    }

    // Each step logs TopIndex, the row the open list is scrolled to, and the Scroll raised: VB6 opens and closes the list on its selected
    // item, the last top at most, and on item 0 with none; a Dropdown Combo opened with none selected raises Scroll where its top moves.
    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    public void ADroppedList_OpensAndClosesWithTheSelectedItemInTheTopRow_AndItem0WithNone(ComboBoxStyle style)
    {
        var log = UI.Run(() =>
        {
            var (window, sink, combo) = Show(style);
            var log = new List<string>();
            void Step(string name, Action act)
            {
                sink.Raised.Clear();
                act();
                Dispatcher.UIThread.RunJobs();
                var shown = combo.IsDropDownOpen ? $" row {Viewer(combo).Offset.Y / 13}" : "";
                log.Add($"{name}: {combo.TopIndex}{shown}{(sink.Raised.Contains("Combo1.Scroll") ? " Scroll" : "")}");
            }
            Step("ListIndex 20, TopIndex 3", () => { combo.ListIndex = 20; combo.TopIndex = 3; });
            Step("open", () => Drop(combo, true));
            Step("close", () => Drop(combo, false));
            Step("ListIndex 5", () => combo.ListIndex = 5);
            Step("open", () => Drop(combo, true));
            Step("TopIndex 12", () => combo.TopIndex = 12);
            Step("close", () => Drop(combo, false));
            Step("ListIndex 25", () => combo.ListIndex = 25);
            Step("open", () => Drop(combo, true));
            Step("close", () => Drop(combo, false));
            Step("ListIndex -1, TopIndex 5", () => { combo.ListIndex = -1; combo.TopIndex = 5; });
            Step("open", () => Drop(combo, true));
            Step("close", () => Drop(combo, false));
            window.Close();
            return log;
        });

        var opened = style == ComboBoxStyle.DropdownCombo ? " Scroll" : "";
        Expect.Lines(new[]
        {
            "ListIndex 20, TopIndex 3: 3 Scroll", "open: 20 row 20", "close: 20",
            "ListIndex 5: 5", "open: 5 row 5", "TopIndex 12: 12 row 12 Scroll", "close: 5",
            "ListIndex 25: 22", "open: 22 row 22", "close: 22",
            "ListIndex -1, TopIndex 5: 5 Scroll", $"open: 0 row 0{opened}", "close: 0",
        }, log);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    public void ADroppedListsScrollBar_MovesTheTop_AndRaisesScroll(ComboBoxStyle style)
    {
        var (top, row, raised) = UI.Run(() =>
        {
            var (window, sink, combo) = Show(style);
            Drop(combo, true);
            sink.Raised.Clear();
            var bar = Viewer(combo).GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical);
            bar.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_LineDownButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            var result = (combo.TopIndex, Viewer(combo).Offset.Y / 13, sink.Raised.ToArray());
            window.Close();
            return result;
        });

        Assert.AreEqual((1, 1d), (top, row));
        Expect.Lines(new[] { "Combo1.Scroll" }, raised);
    }

    // Typing opens a Dropdown Combo's list on the items it matches, best first, from its first row: item02, then item20 to item29.
    [TestMethod]
    public void ADropdownCombosListNarrowedByTyping_ShowsItsMatchesFromItsFirstRow_AndOnceClosedOpensOnItsTopAgain()
    {
        var (narrowed, reopened) = UI.Run(() =>
        {
            var (window, _, combo) = Show(ComboBoxStyle.DropdownCombo);
            combo.TopIndex = 10;
            combo.Focus();
            Dispatcher.UIThread.RunJobs();
            foreach (var character in "item2")
            {
                window.KeyTextInput(character.ToString());
                Dispatcher.UIThread.RunJobs();
            }
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var narrowed = (combo.IsDropDownOpen, Viewer(combo).Offset.Y);
            Drop(combo, false);
            combo.ListIndex = 20;
            Drop(combo, true);
            var reopened = (combo.TopIndex, Viewer(combo).Offset.Y / 13);
            window.Close();
            return (narrowed, reopened);
        });

        Assert.IsTrue(narrowed.IsDropDownOpen);
        Assert.AreEqual(0d, narrowed.Y);
        Assert.AreEqual((20, 20d), reopened);
    }
}
