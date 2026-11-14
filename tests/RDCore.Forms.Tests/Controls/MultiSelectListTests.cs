using System.IO.Abstractions.TestingHelpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A list whose MultiSelect is Simple or Extended, pressed and keyed as in the VB6 measurements. Each step logs the Click and DblClick it
// raised, the items selected and ListIndex. VB6's Ctrl and Shift presses cannot be measured by sending messages, so those steps follow
// the ListBox's own rules.
[TestClass]
public sealed class MultiSelectListTests
{
    // A list of five items, and what a program reads of it.
    private sealed record Multi(ListBox Control, Func<int> ListIndex, Func<int, bool> Selected);

    private static Multi Make(string type, ListBoxMultiSelect mode)
    {
        switch (type)
        {
            case "ListBox":
                var list = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List1")
                    .Set(P.List, ["a", "b", "c", "d", "e"]).Set(P.MultiSelect, mode));
                return new(list, () => list.ListIndex, list.GetSelected);
            case "FileListBox":
                var root = OperatingSystem.IsWindows() ? @"C:\five" : "/five";
                var files = new MockFileSystem(new[] { "a.txt", "b.txt", "c.txt", "d.txt", "e.bas" }
                    .ToDictionary(name => Path.Combine(root, name), _ => new MockFileData("")), root);
                var fileList = new VBFileListBox(new ComponentInstance(VBClasses.FileListBox, "File1").Set(P.MultiSelect, mode),
                    OperatingSystem.IsWindows() ? files : new RootDrives(files));
                return new(fileList, () => fileList.ListIndex, fileList.GetSelected);
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static Pointer Mouse() => new(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);

    // A left press and release on an item, a double click's second one where clickCount is 2.
    private static void Press(Window window, ListBox list, int index, KeyModifiers modifiers = KeyModifiers.None, int clickCount = 1)
    {
        var item = list.ContainerFromIndex(index)!;
        var pointer = Mouse();
        var point = item.TranslatePoint(new Point(4, 4), window)!.Value;
        item.RaiseEvent(new PointerPressedEventArgs(item, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), modifiers, clickCount));
        item.RaiseEvent(new PointerReleasedEventArgs(item, pointer, window, point, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();
    }

    // A key pressed, the character it types, and the key released.
    private static void Stroke(Window window, Key key, PhysicalKey physical, string? text)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, text);
        Dispatcher.UIThread.RunJobs();
        if (text is not null)
            window.KeyTextInput(text);
        window.KeyRelease(key, RawInputModifiers.None, physical, text);
        Dispatcher.UIThread.RunJobs();
    }

    // Runs the steps on a new list and logs each as "step: events [selected] ListIndex".
    private static List<string> Steps(string type, ListBoxMultiSelect mode, params (string Name, Action<Window, ListBox> Act)[] steps) =>
        UI.Run(() =>
        {
            var list = Make(type, mode);
            var (window, sink) = ControlHost.Show((list.Control, new Rect(10, 5, 160, 140)));
            var log = new List<string>();
            foreach (var (name, act) in steps)
            {
                sink.Raised.Clear();
                act(window, list.Control);
                var events = sink.Raised.Where(e => e.EndsWith(".Click", StringComparison.Ordinal) || e.EndsWith(".DblClick", StringComparison.Ordinal))
                    .Select(e => e[(e.IndexOf('.') + 1)..]);
                var selected = Enumerable.Range(0, list.Control.ItemCount).Where(list.Selected);
                log.Add($"{name}: {string.Join(" ", events)} [{string.Join(",", selected)}] {list.ListIndex()}");
            }
            window.Close();
            return log;
        });

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void ASimpleListsPress_TogglesTheItem_WhateverTheModifiers_AndRaisesClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Simple,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("Ctrl+press 1", (w, l) => Press(w, l, 1, KeyModifiers.Control)),
            ("Shift+press 4", (w, l) => Press(w, l, 4, KeyModifiers.Shift)));

        Expect.Lines(["press 1: Click [1] 1", "press 3: Click [1,3] 3", "press 3: Click [1] 3", "Ctrl+press 1: Click [] 1",
            "Shift+press 4: Click [4] 4"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void AnExtendedListsPress_SelectsTheItemAlone_TogglesItWithCtrl_AndSelectsFromTheAnchorWithShift(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Extended,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("press 3", (w, l) => Press(w, l, 3)),
            ("Ctrl+press 1", (w, l) => Press(w, l, 1, KeyModifiers.Control)),
            ("Shift+press 4", (w, l) => Press(w, l, 4, KeyModifiers.Shift)),
            ("press 2", (w, l) => Press(w, l, 2)));

        Expect.Lines(["press 1: Click [1] 1", "press 3: Click [3] 3", "Ctrl+press 1: Click [1,3] 1", "Shift+press 4: Click [1,2,3,4] 4",
            "press 2: Click [2] 2"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void ASimpleListsDown_MovesOnlyTheFocus_AndSpaceTogglesTheFocusedItem_EachWithClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Simple,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("Down", (w, _) => Stroke(w, Key.Down, PhysicalKey.ArrowDown, null)),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Down: Click [1] 2", "Space: Click [1,2] 2", "Space: Click [1] 2"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void AnExtendedListsDown_SelectsTheNextItemAlone_AndSpaceChangesNothing_EachWithClick(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Extended,
            ("press 1", (w, l) => Press(w, l, 1)),
            ("Down", (w, _) => Stroke(w, Key.Down, PhysicalKey.ArrowDown, null)),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Down: Click [2] 2", "Space: Click [2] 2"], log);
    }

    private static (string, Action<Window, ListBox>) Keyed(string name, Key key, PhysicalKey physical) =>
        (name, (w, _) => Stroke(w, key, physical, null));

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void ASimpleListsLeftRightHomeAndEnd_MoveOnlyTheFocus_EachWithClick_AtTheEndsToo(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Simple,
            ("press 1", (w, l) => Press(w, l, 1)),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft),
            Keyed("Home", Key.Home, PhysicalKey.Home),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft),
            Keyed("End", Key.End, PhysicalKey.End),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Right: Click [1] 2", "Right: Click [1] 3", "Left: Click [1] 2", "Home: Click [1] 0",
            "Left: Click [1] 0", "End: Click [1] 4", "Right: Click [1] 4", "Space: Click [1,4] 4"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void AnExtendedListsLeftRightHomeAndEnd_SelectTheItemReachedAlone_EachWithClick_AtTheEndsToo(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.Extended,
            ("press 1", (w, l) => Press(w, l, 1)),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft),
            Keyed("End", Key.End, PhysicalKey.End),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Home", Key.Home, PhysicalKey.Home),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft));

        Expect.Lines(["press 1: Click [1] 1", "Right: Click [2] 2", "Left: Click [1] 1", "End: Click [4] 4", "Right: Click [4] 4",
            "Home: Click [0] 0", "Left: Click [0] 0"], log);
    }

    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("FileListBox")]
    public void ASingleSelectListsLeftRightHomeAndEnd_MoveTheSelection_WithClickOnlyWhereItMoves(string type)
    {
        var log = Steps(type, ListBoxMultiSelect.None,
            ("press 1", (w, l) => Press(w, l, 1)),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft),
            Keyed("Home", Key.Home, PhysicalKey.Home),
            Keyed("Left", Key.Left, PhysicalKey.ArrowLeft),
            Keyed("End", Key.End, PhysicalKey.End),
            Keyed("Right", Key.Right, PhysicalKey.ArrowRight),
            ("Space", (w, _) => Stroke(w, Key.Space, PhysicalKey.Space, " ")));

        Expect.Lines(["press 1: Click [1] 1", "Right: Click [2] 2", "Right: Click [3] 3", "Left: Click [2] 2", "Home: Click [0] 0",
            "Left:  [0] 0", "End: Click [4] 4", "Right:  [4] 4", "Space:  [4] 4"], log);
    }

    [TestMethod]
    [DataRow(ListBoxMultiSelect.None)]
    [DataRow(ListBoxMultiSelect.Simple)]
    [DataRow(ListBoxMultiSelect.Extended)]
    public void PageDownAndPageUp_MoveAsManyItemsAsTheListShowsWhole_LessOne(ListBoxMultiSelect mode)
    {
        var (reached, wholeAtTheBottom, clicks) = UI.Run(() =>
        {
            var list = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List1")
                .Set(P.List, [.. Enumerable.Range(0, 30).Select(i => $"item{i}")]).Set(P.MultiSelect, mode));
            var (window, sink) = ControlHost.Show((list, new Rect(10, 5, 160, 140)));
            Press(window, list, 0);
            sink.Raised.Clear();
            var reached = new List<int>();
            bool? wholeAtTheBottom = null;
            foreach (var (key, physical) in new[] { (Key.PageDown, PhysicalKey.PageDown), (Key.PageDown, PhysicalKey.PageDown),
                         (Key.PageUp, PhysicalKey.PageUp), (Key.End, PhysicalKey.End), (Key.PageUp, PhysicalKey.PageUp) })
            {
                Stroke(window, key, physical, null);
                reached.Add(list.ListIndex);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                // After the first PageDown, the item reached is the last one shown whole, and the list has not scrolled.
                wholeAtTheBottom ??= ShownWhole(list, 0) && ShownWhole(list, list.ListIndex) && !ShownWhole(list, list.ListIndex + 1);
            }
            var clicks = sink.Raised.Count(e => e.EndsWith(".Click", StringComparison.Ordinal));
            window.Close();
            return (reached, wholeAtTheBottom, clicks);
        });

        var page = reached[0];
        Assert.IsTrue(page > 1, $"a page of {page}");
        Assert.IsTrue(wholeAtTheBottom);
        CollectionAssert.AreEqual(new[] { page, 2 * page, page, 29, 29 - page }, reached);
        Assert.AreEqual(5, clicks);
    }

    // Whether the list shows the item at index whole, from its top to its bottom.
    private static bool ShownWhole(ListBox list, int index)
    {
        var viewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
        if (list.ContainerFromIndex(index) is not { } item || item.TranslatePoint(default, viewer) is not { } top)
            return false;
        return top.Y >= -0.5 && top.Y + item.Bounds.Height <= viewer.Viewport.Height + 0.5;
    }

    [TestMethod]
    [DataRow("ListBox", ListBoxMultiSelect.Simple)]
    [DataRow("ListBox", ListBoxMultiSelect.Extended)]
    [DataRow("FileListBox", ListBoxMultiSelect.Simple)]
    [DataRow("FileListBox", ListBoxMultiSelect.Extended)]
    public void ADoubleClick_RaisesClickThenDblClick_AndTogglesTheItemOnce(string type, ListBoxMultiSelect mode)
    {
        var log = Steps(type, mode,
            ("double click 0", (w, l) =>
            {
                Press(w, l, 0);
                Press(w, l, 0, clickCount: 2);
            }));

        Expect.Lines(["double click 0: Click DblClick [0] 0"], log);
    }
}
