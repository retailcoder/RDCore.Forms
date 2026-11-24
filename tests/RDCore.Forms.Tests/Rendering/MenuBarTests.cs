using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

[TestClass]
public sealed class MenuBarTests
{
    private static ComponentInstance Menu(ComponentInstance container, string name, string caption, string? shortcut = null)
    {
        var menu = new ComponentInstance(ComponentClasses.Menu, name).Set(P.Caption, caption);
        if (shortcut is not null && MenuShortcut.TryParse(shortcut, out var key))
            menu.Set(P.Shortcut, key);
        menu.SetContainer(container);
        return menu;
    }

    // The form VB6 was measured with: Top holds Alpha (Ctrl+Q), Sub, which holds Leaf (Ctrl+W), and Dis (Ctrl+E), disabled. Solo
    // is a top-level menu with no items.
    private static ComponentInstance Probe()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d);
        var top = Menu(root, "mnuTop", "&Top");
        Menu(top, "mnuA", "&Alpha", "^Q");
        var sub = Menu(top, "mnuSub", "&Sub");
        Menu(sub, "mnuLeaf", "&Leaf", "^W");
        Menu(top, "mnuDis", "&Dis", "^E").Set(P.Enabled, false);
        Menu(root, "mnuSolo", "S&olo");
        return root;
    }

    private static RDFormsFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new RDFormsFormWindow(new FormDefinition(root));
        RDFormsControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    private static MenuItem Item(RDFormsFormWindow window, string name) =>
        window.MenuBar.GetLogicalDescendants().OfType<MenuItem>().Single(m => RDFormsControl.GetInstance(m)?.Name == name);

    private static FormMenuItem Model(RDFormsFormWindow window, string name) =>
        window.MenuBar.Menu.AllItems.Single(item => item.Component.Name == name);

    private static List<string> Clicks(RecordingSink sink) => [.. sink.Raised.Where(e => e.EndsWith(".Click", StringComparison.Ordinal))];

    private static void Press(RDFormsFormWindow window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.Control, PhysicalKey.A + (key - Key.A), null);
        window.KeyRelease(key, RawInputModifiers.Control, PhysicalKey.A + (key - Key.A), null);
        Dispatcher.UIThread.RunJobs();
    }

    [TestMethod]
    public void TheBar_HoldsTheTopLevelMenus_WithTheirItemsNested_AndAHyphenIsASeparator()
    {
        var (bar, sub, separators, header) = UI.Run(() =>
        {
            var root = Probe();
            Menu(FormMenuItem.Of(root.Contents[0]).Component, "mnuSep", "-");
            var window = Open(root, new RecordingSink());
            var result = (
                window.MenuBar.Items.OfType<MenuItem>().Select(m => RDFormsControl.GetInstance(m)!.Name).ToList(),
                Item(window, "mnuSub").Items.OfType<MenuItem>().Select(m => RDFormsControl.GetInstance(m)!.Name).ToList(),
                Item(window, "mnuTop").Items.OfType<Separator>().Count(),
                (string?)Item(window, "mnuTop").Header);
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "mnuTop", "mnuSolo" }, bar);
        CollectionAssert.AreEqual(new[] { "mnuLeaf" }, sub);
        Assert.AreEqual(1, separators);
        Assert.AreEqual("_Top", header);
    }

    [TestMethod]
    [DataRow("&File", "_File")]
    [DataRow("Save && Exit", "Save & Exit")]
    [DataRow("a_b", "a__b")]
    [DataRow("&One &Two", "_One Two")]
    public void ACaption_IsShownWithItsAccessKey(string caption, string header)
    {
        var shown = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            Model(window, "mnuA").Caption = caption;
            var result = (string?)Item(window, "mnuA").Header;
            window.Close();
            return result;
        });

        Assert.AreEqual(header, shown);
    }

    [TestMethod]
    public void AFormWithNoMenus_OrWithEveryMenuHidden_HasNoBar()
    {
        var (none, hidden, shownAgain) = UI.Run(() =>
        {
            var plain = Open(new ComponentInstance(ComponentClasses.Form, "Form1"), new RecordingSink());
            var root = new ComponentInstance(ComponentClasses.Form, "Form2");
            var popup = Menu(root, "mnuPopup", "Popup");
            Menu(popup, "mnuCopy", "&Copy");
            popup.Set(P.Visible, false);
            var popupOnly = Open(root, new RecordingSink());
            var before = popupOnly.MenuBar.IsVisible;
            FormMenuItem.Of(popup).Visible = true;
            var result = (plain.MenuBar.IsVisible, before, popupOnly.MenuBar.IsVisible);
            plain.Close();
            popupOnly.Close();
            return result;
        });

        Assert.IsFalse(none);
        Assert.IsFalse(hidden);
        Assert.IsTrue(shownAgain);
    }

    [TestMethod]
    public void AMenuThatHoldsItems_RaisesItsClickAsItOpens_AndALeafAsItIsChosen()
    {
        var (opened, nested, closed, chosen) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            Item(window, "mnuTop").IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
            var first = Clicks(sink);
            sink.Raised.Clear();
            Item(window, "mnuSub").IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
            var second = Clicks(sink);
            sink.Raised.Clear();
            Item(window, "mnuSub").IsSubMenuOpen = false;
            Item(window, "mnuTop").IsSubMenuOpen = false;
            Dispatcher.UIThread.RunJobs();
            var third = Clicks(sink);
            Item(window, "mnuLeaf").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            var fourth = Clicks(sink);
            window.Close();
            return (first, second, third, fourth);
        });

        Expect.Lines(["mnuTop.Click"], opened);
        Expect.Lines(["mnuSub.Click"], nested);
        Assert.IsEmpty(closed);
        Expect.Lines(["mnuLeaf.Click"], chosen);
    }

    private static AutomationPeer PeerOf(RDFormsFormWindow window, string name) => ControlAutomationPeer.CreatePeerForElement(Item(window, name));

    // The patterns a Win32 menu's items have: an item that holds items expands and collapses, on the bar or in a menu; one that holds
    // none, on the bar or in a menu, is invoked and toggled.
    [TestMethod]
    [DataRow("mnuTop", true)]
    [DataRow("mnuSub", true)]
    [DataRow("mnuSolo", false)]
    [DataRow("mnuLeaf", false)]
    public void AMenuItem_HasTheUIAutomationPatternsOfAWin32MenuItem(string name, bool holdsItems)
    {
        var patterns = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            var peer = PeerOf(window, name);
            var result = (peer.GetProvider<IExpandCollapseProvider>() is not null, peer.GetProvider<IInvokeProvider>() is not null,
                peer.GetProvider<IToggleProvider>() is not null, peer.GetAutomationControlType());
            window.Close();
            return result;
        });

        Assert.AreEqual((holdsItems, !holdsItems, !holdsItems, AutomationControlType.MenuItem), patterns);
    }

    // As with the pointer: expanding a menu raises its Click as it opens, invoking a leaf raises its own and closes the menus, and
    // collapsing the bar's menu closes it with no Click.
    [TestMethod]
    public void UIAutomation_ExpandsAMenu_InvokesALeaf_AndCollapses_RaisingTheClicksAPointerDoes()
    {
        var (opened, nested, states, chosen, closedAfterChoosing, collapsed) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            var top = PeerOf(window, "mnuTop").GetProvider<IExpandCollapseProvider>()!;
            top.Expand();
            Dispatcher.UIThread.RunJobs();
            var first = Clicks(sink);
            sink.Raised.Clear();
            var sub = PeerOf(window, "mnuSub").GetProvider<IExpandCollapseProvider>()!;
            sub.Expand();
            Dispatcher.UIThread.RunJobs();
            var second = Clicks(sink);
            sink.Raised.Clear();
            var expanded = (top.ExpandCollapseState, sub.ExpandCollapseState);
            PeerOf(window, "mnuLeaf").GetProvider<IInvokeProvider>()!.Invoke();
            Dispatcher.UIThread.RunJobs();
            var third = Clicks(sink);
            var closed = (Item(window, "mnuTop").IsSubMenuOpen, window.MenuBar.IsOpen);
            top.Expand();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            top.Collapse();
            Dispatcher.UIThread.RunJobs();
            var fourth = (Clicks(sink), top.ExpandCollapseState, window.MenuBar.IsOpen);
            window.Close();
            return (first, second, expanded, third, closed, fourth);
        });

        Expect.Lines(["mnuTop.Click"], opened);
        Expect.Lines(["mnuSub.Click"], nested);
        Assert.AreEqual((ExpandCollapseState.Expanded, ExpandCollapseState.Expanded), states);
        Expect.Lines(["mnuLeaf.Click"], chosen);
        Assert.AreEqual((false, false), closedAfterChoosing);
        Assert.IsEmpty(collapsed.Item1);
        Assert.AreEqual((ExpandCollapseState.Collapsed, false), (collapsed.Item2, collapsed.Item3));
    }

    // Toggling an item chooses it, as a click does: the program's Checked has the last word, and CheckOnClick ticks it before its Click.
    // A disabled item refuses every pattern, as a Win32 one does.
    [TestMethod]
    public void UIAutomation_TogglesAnItemByChoosingIt_AndADisabledItemRefuses()
    {
        var (states, raised, refused) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            var alpha = PeerOf(window, "mnuA").GetProvider<IToggleProvider>()!;
            alpha.Toggle();
            Dispatcher.UIThread.RunJobs();
            var untouched = alpha.ToggleState;
            Model(window, "mnuA").CheckOnClick = true;
            alpha.Toggle();
            Dispatcher.UIThread.RunJobs();
            var ticked = alpha.ToggleState;
            var clicks = Clicks(sink);
            var dis = PeerOf(window, "mnuDis");
            var refusals = new List<string>();
            foreach (var (pattern, use) in new (string, Action)[]
                     {
                         ("Invoke", () => dis.GetProvider<IInvokeProvider>()!.Invoke()),
                         ("Toggle", () => dis.GetProvider<IToggleProvider>()!.Toggle()),
                     })
            {
                try
                {
                    use();
                }
                catch (ElementNotEnabledException)
                {
                    refusals.Add(pattern);
                }
            }
            var result = ((untouched, ticked), clicks, refusals);
            window.Close();
            return result;
        });

        Assert.AreEqual((ToggleState.Off, ToggleState.On), states);
        Expect.Lines(["mnuA.Click", "mnuA.Click"], raised);
        Expect.Lines(["Invoke", "Toggle"], refused);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public void ChoosingAnItem_LeavesItsChecked_UnlessItsCheckOnClickIsTrue(bool checkOnClick, bool ticked)
    {
        var (checkedAfter, tickShown, inClick) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Probe();
            var window = Open(root, sink);
            var alpha = Model(window, "mnuA");
            alpha.CheckOnClick = checkOnClick;
            bool? seen = null;
            sink.Handler = (source, definition, _) =>
            {
                if (source == alpha.Component && ReferenceEquals(definition, Events.Click))
                    seen = alpha.Checked;
            };
            Item(window, "mnuA").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            var result = (alpha.Checked, Item(window, "mnuA").IsChecked, seen);
            window.Close();
            return result;
        });

        Assert.AreEqual(ticked, checkedAfter);
        Assert.AreEqual(ticked, tickShown);
        Assert.AreEqual(ticked, inClick);
    }

    [TestMethod]
    public void AShortcut_RaisesEachMenusClick_FromTheTopDownToItsItems()
    {
        var (alpha, leaf, disabled) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            Press(window, Key.Q);
            var first = Clicks(sink);
            sink.Raised.Clear();
            Press(window, Key.W);
            var second = Clicks(sink);
            sink.Raised.Clear();
            Press(window, Key.E);
            var third = Clicks(sink);
            window.Close();
            return (first, second, third);
        });

        Expect.Lines(["mnuTop.Click", "mnuA.Click"], alpha);
        Expect.Lines(["mnuTop.Click", "mnuSub.Click", "mnuLeaf.Click"], leaf);
        Expect.Lines(["mnuTop.Click"], disabled);
    }

    [TestMethod]
    public void AParentsClickThatEnablesTheItem_LetsItsShortcutThrough()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            sink.Handler = (source, definition, _) =>
            {
                if (source.Name == "mnuTop" && ReferenceEquals(definition, Events.Click))
                    Model(window, "mnuDis").Enabled = true;
            };
            Press(window, Key.E);
            var result = Clicks(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["mnuTop.Click", "mnuDis.Click"], raised);
    }

    [TestMethod]
    [DataRow("mnuTop", "Enabled", Key.Q, "")]
    [DataRow("mnuTop", "Visible", Key.Q, "")]
    [DataRow("mnuA", "Visible", Key.Q, "mnuTop.Click")]
    [DataRow("mnuSub", "Enabled", Key.W, "mnuTop.Click")]
    [DataRow("mnuSub", "Visible", Key.W, "mnuTop.Click")]
    [DataRow("mnuLeaf", "Enabled", Key.W, "mnuTop.Click,mnuSub.Click")]
    public void AShortcut_StopsAtTheFirstMenuThatIsDisabledOrHidden(string name, string property, Key key, string expected)
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            Model(window, name).Component.Set(property == "Enabled" ? P.Enabled : P.Visible, false);
            Press(window, key);
            var result = Clicks(sink);
            window.Close();
            return result;
        });

        Assert.AreEqual(expected, string.Join(",", raised));
    }

    [TestMethod]
    public void AShortcut_ChoosesItsItemWhileATextBoxHasTheFocus_AndTheBoxSeesNoKey()
    {
        var raised = UI.Run(() =>
        {
            var root = Probe();
            var text = new ComponentInstance(ComponentClasses.TextBox, "Text1").Set(P.Width, 120d).Set(P.Height, 21d);
            text.SetContainer(root);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            window.View.ControlOf(text)!.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            Press(window, Key.Q);
            // The key's release is not the shortcut's: what the box raises for it is not what this test looks at.
            var result = sink.Raised.Where(e => !e.Contains("Focus", StringComparison.Ordinal) && !e.Contains(".KeyUp", StringComparison.Ordinal)).ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["mnuTop.Click", "mnuA.Click"], raised);
    }

    [TestMethod]
    public void AnItemsShortcut_IsShownBesideIt_AndFollowsItsComponent()
    {
        var (before, after) = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            var shown = Item(window, "mnuA").InputGesture;
            Model(window, "mnuA").Shortcut = new MenuShortcut(113, ShortcutModifiers.Shift);
            var result = (shown, Item(window, "mnuA").InputGesture);
            window.Close();
            return result;
        });

        Assert.AreEqual(new KeyGesture(Key.Q, KeyModifiers.Control), before);
        Assert.AreEqual(new KeyGesture(Key.F2, KeyModifiers.Shift), after);
    }

    [TestMethod]
    public void AnyKeyWithAnyModifiers_CanBeAShortcut()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Probe();
            var window = Open(root, sink);
            Model(window, "mnuA").Shortcut = new MenuShortcut(36, ShortcutModifiers.Ctrl | ShortcutModifiers.Shift | ShortcutModifiers.Alt);
            window.KeyPress(Key.Home, RawInputModifiers.Control | RawInputModifiers.Shift | RawInputModifiers.Alt, PhysicalKey.Home, null);
            Dispatcher.UIThread.RunJobs();
            var result = Clicks(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["mnuTop.Click", "mnuA.Click"], raised);
    }

    // mnuTop holds Alpha, Sub and Dis, then mnuArr(1) and mnuArr(3); the bar ends with mnuBar(0).
    [TestMethod]
    public void ALoadedElement_IsShownInItsPlace_ChosenAsItself_AndAnUnloadedOne_IsTakenAway()
    {
        var (loaded, bar, clicked, unloaded) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Probe();
            var top = root.Contents[0];
            Menu(top, "mnuArr", "A1").Index = 1;
            Menu(top, "mnuArr", "A3").Index = 3;
            Menu(root, "mnuBar", "B0").Index = 0;
            var window = Open(root, sink);
            int? source = null;
            sink.Handler = (component, _, _) => source = component.Index;
            var menu = window.MenuBar.Menu;
            menu.ArrayOf("mnuArr")!.Load(2).Caption = "A2";
            menu.ArrayOf("mnuBar")!.Load(1).Caption = "B1";
            Dispatcher.UIThread.RunJobs();
            var inTop = Headers(Item(window, "mnuTop"));
            var onBar = Headers(window.MenuBar);
            window.MenuBar.GetLogicalDescendants().OfType<MenuItem>().Single(m => RDFormsControl.GetInstance(m) is { Index: 2 })
                .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.ArrayOf("mnuArr")!.Unload(2);
            menu.ArrayOf("mnuBar")!.Unload(1);
            Dispatcher.UIThread.RunJobs();
            var result = (inTop, onBar, source, Headers(Item(window, "mnuTop")).Concat(Headers(window.MenuBar)).ToArray());
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "_Alpha", "_Sub", "_Dis", "A1", "A2", "A3" }, loaded);
        CollectionAssert.AreEqual(new[] { "_Top", "S_olo", "B0", "B1" }, bar);
        Assert.AreEqual(2, clicked);
        CollectionAssert.AreEqual(new[] { "_Alpha", "_Sub", "_Dis", "A1", "A3", "_Top", "S_olo", "B0" }, unloaded);

        static string[] Headers(ItemsControl items) => [.. items.Items.OfType<MenuItem>().Select(m => (string)m.Header!)];
    }

    [TestMethod]
    public void UnloadingTheLastVisibleMenuOfTheBar_TakesTheBarAway()
    {
        var (before, after) = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d);
            Menu(root, "mnuBar", "B0").Index = 0;
            var window = Open(root, new RecordingSink());
            var bar = window.MenuBar.Menu.ArrayOf("mnuBar")!;
            bar.Load(1);
            bar[0].Visible = false;
            Dispatcher.UIThread.RunJobs();
            var shown = window.MenuBar.IsVisible;
            bar.Unload(1);
            Dispatcher.UIThread.RunJobs();
            var result = (shown, window.MenuBar.IsVisible);
            window.Close();
            return result;
        });

        Assert.IsTrue(before);
        Assert.IsFalse(after);
    }
}
