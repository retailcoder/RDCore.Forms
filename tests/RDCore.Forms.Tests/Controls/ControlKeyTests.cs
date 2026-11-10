using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The key events of the controls that raise their own, in the orders measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class ControlKeyTests
{
    private static ComponentInstance Root() =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double top = 8)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, 8d).Set(P.Top, top).Set(P.Width, 120d).Set(P.Height, 80d);
        component.SetContainer(container);
        return component;
    }

    // The window, with the control focused and what was raised so far forgotten.
    private static VBFormWindow Open(ComponentInstance root, ComponentInstance focused, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.View.ControlOf(focused)!.Focus();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    // A key pressed, the character it types, and the key released. Where typed is false, the window system types no character for it,
    // as Windows does for a key a control handled as it went down.
    private static void Type(VBFormWindow window, Key key, PhysicalKey physical, string? text, bool typed = true,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, text);
        Dispatcher.UIThread.RunJobs();
        if (text is not null && typed)
            window.KeyTextInput(text);
        Dispatcher.UIThread.RunJobs();
        window.KeyRelease(key, modifiers, physical, text);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<string> Raised(RecordingSink sink) => [.. sink.WithoutMoves];

    // Each key event as its name and code, the form's marked F, and the other events by name; the shift argument left out.
    private static List<string> Log(RecordingSink sink, ComponentInstance root)
    {
        var log = new List<string>();
        sink.Handler = (source, definition, arguments) =>
        {
            if (definition.Name is "MouseMove")
                return;
            var key = definition.Name is "KeyDown" or "KeyPress" or "KeyUp";
            log.Add((ReferenceEquals(source, root) ? "F" : "") + definition.Name + (key ? " " + arguments[0].AsInteger() : ""));
        };
        return log;
    }

    [TestMethod]
    public void SpaceOnACommandButton_RaisesItsKeyEvents_ThenClick()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var command1 = Add(root, VBClasses.CommandButton, "Command1");
            var sink = new RecordingSink();
            var window = Open(root, command1, sink);
            Type(window, Key.Space, PhysicalKey.Space, " ");
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command1.KeyDown(Integer 32, Integer 0)", "Command1.KeyPress(Integer 32)", "Command1.KeyUp(Integer 32, Integer 0)",
            "Command1.Click"], raised);
    }

    [TestMethod]
    [DataRow("CommandButton")]
    [DataRow("CheckBox")]
    [DataRow("OptionButton")]
    public void ALetterOnAButton_RaisesItsKeyEvents_AndNoClick(string type)
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var button = Add(root, VBClasses.ByTypeName("VB." + type)!, "Button1");
            if (type == "OptionButton")
                button.Set(P.OptionButtonValue, true);
            var sink = new RecordingSink();
            var window = Open(root, button, sink);
            Type(window, Key.K, PhysicalKey.K, "k");
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Button1.KeyDown(Integer 75, Integer 0)", "Button1.KeyPress(Integer 107)", "Button1.KeyUp(Integer 75, Integer 0)"], raised);
    }

    [TestMethod]
    public void SpaceOnACheckBox_RaisesItsKeyEvents_ThenClick_AndOnTheSelectedOptionButtonNoClick()
    {
        var (check, option) = UI.Run(() =>
        {
            var root = Root();
            var check1 = Add(root, VBClasses.CheckBox, "Check1");
            var option1 = Add(root, VBClasses.OptionButton, "Option1", 100).Set(P.OptionButtonValue, true);
            var sink = new RecordingSink();
            var window = Open(root, check1, sink);
            Type(window, Key.Space, PhysicalKey.Space, " ");
            var first = Raised(sink);
            window.View.ControlOf(option1)!.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            Type(window, Key.Space, PhysicalKey.Space, " ");
            var result = (first, Raised(sink));
            window.Close();
            return result;
        });

        Expect.Lines(["Check1.KeyDown(Integer 32, Integer 0)", "Check1.KeyPress(Integer 32)", "Check1.KeyUp(Integer 32, Integer 0)",
            "Check1.Click"], check);
        Expect.Lines(["Option1.KeyDown(Integer 32, Integer 0)", "Option1.KeyPress(Integer 32)", "Option1.KeyUp(Integer 32, Integer 0)"], option);
    }

    [TestMethod]
    [DataRow("CommandButton", false)]
    [DataRow("CheckBox", false)]
    [DataRow("CommandButton", true)]
    public void AnArrowOnAButton_MovesTheFocus_AndRaisesNoKeyEvent_NotEvenTheNextButtonsKeyUp(string type, bool preview)
    {
        var (raised, focused, value) = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, preview);
            var button1 = Add(root, VBClasses.ByTypeName("VB." + type)!, "Button1");
            var command2 = Add(root, VBClasses.CommandButton, "Command2", 100);
            var sink = new RecordingSink();
            var window = Open(root, button1, sink);
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            var result = (sink.Raised.Where(e => e.Contains(".Key", StringComparison.Ordinal)).ToList(),
                window.View.ControlOf(command2)!.IsFocused, type == "CheckBox" ? button1.Get(P.CheckBoxValue) : default);
            window.Close();
            return result;
        });

        Assert.IsEmpty(raised);
        Assert.IsTrue(focused);
        Assert.AreEqual(default, value);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SpaceOnAButton_RaisesKeyPress32_Once_WhetherOrNotTheWindowSystemTypesTheSpace(bool typed)
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, true);
            var command1 = Add(root, VBClasses.CommandButton, "Command1");
            var sink = new RecordingSink();
            var window = Open(root, command1, sink);
            var log = Log(sink, root);
            Type(window, Key.Space, PhysicalKey.Space, " ", typed);
            var result = log.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["FKeyDown 32", "KeyDown 32", "FKeyPress 32", "KeyPress 32", "FKeyUp 32", "KeyUp 32", "Click"], raised);
    }

    [TestMethod]
    public void SpaceInATextBox_RaisesKeyPress32_AndTypesTheSpace()
    {
        var (raised, text) = UI.Run(() =>
        {
            var root = Root();
            var text1 = Add(root, VBClasses.TextBox, "Text1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, text1, sink);
            var log = Log(sink, root);
            Type(window, Key.Space, PhysicalKey.Space, " ");
            var result = (log.ToList(), text1.Get(P.Text));
            window.Close();
            return result;
        });

        Expect.Lines(["KeyDown 32", "KeyPress 32", "Change", "KeyUp 32"], raised);
        Assert.AreEqual(" ", text);
    }

    [TestMethod]
    public void SpaceInAComboBoxsEditBox_RaisesKeyPress32_Once_AndTypesTheSpace()
    {
        var (raised, text) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var log = Log(sink, root);
            Type(window, Key.Space, PhysicalKey.Space, " ");
            var result = (log.ToList(), combo1.Get(P.Text));
            window.Close();
            return result;
        });

        Expect.Lines(["KeyDown 32", "KeyPress 32", "Change", "KeyUp 32"], raised);
        Assert.AreEqual(" ", text);
    }

    [TestMethod]
    public void DownInAListBox_RaisesKeyDown_ThenClick_ThenKeyUp()
    {
        var (raised, index) = UI.Run(() =>
        {
            var root = Root();
            var list1 = Add(root, VBClasses.ListBox, "List1").Set(P.List, ["alpha", "beta", "gamma"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (VBListBox)window.View.ControlOf(list1)!;
            list.ListIndex = 0;
            sink.Raised.Clear();
            int? atClick = null;
            sink.Handler = (_, definition, _) =>
            {
                if (ReferenceEquals(definition, VBEvents.Click))
                    atClick = list.ListIndex;
            };
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            var result = (Raised(sink), atClick);
            window.Close();
            return result;
        });

        Expect.Lines(["List1.KeyDown(Integer 40, Integer 0)", "List1.Click", "List1.KeyUp(Integer 40, Integer 0)"], raised);
        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public void ALetterInAListBox_SelectsTheNextItemStartingWithIt_BetweenItsKeyPressAndKeyUp()
    {
        var (raised, indexes) = UI.Run(() =>
        {
            var root = Root();
            var list1 = Add(root, VBClasses.ListBox, "List1").Set(P.List, ["Kilo", "alpha", "kappa", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (VBListBox)window.View.ControlOf(list1)!;
            var seen = new List<int>();
            Type(window, Key.K, PhysicalKey.K, "k");
            var first = Raised(sink);
            seen.Add(list.ListIndex);
            Type(window, Key.K, PhysicalKey.K, "k");
            seen.Add(list.ListIndex);
            Type(window, Key.K, PhysicalKey.K, "k");
            seen.Add(list.ListIndex);
            window.Close();
            return (first, seen);
        });

        Expect.Lines(["List1.KeyDown(Integer 75, Integer 0)", "List1.KeyPress(Integer 107)", "List1.Click", "List1.KeyUp(Integer 75, Integer 0)"],
            raised);
        CollectionAssert.AreEqual(new[] { 0, 2, 0 }, indexes);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo, "Combo1.Change|Combo1.DropDown")]
    [DataRow(ComboBoxStyle.DropdownList, "Combo1.Click")]
    public void ALetterInAComboBox_RaisesItsChangeOrClick_BetweenItsKeyPressAndKeyUp(ComboBoxStyle style, string between)
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, style)
                .Set(P.List, ["alpha", "kappa"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            Type(window, Key.K, PhysicalKey.K, "k");
            var result = Raised(sink);
            window.Close();
            return result;
        });

        // A Dropdown Combo's letter opens its list on the item it matches.
        Expect.Lines(["Combo1.KeyDown(Integer 75, Integer 0)", "Combo1.KeyPress(Integer 107)", .. between.Split('|'),
            "Combo1.KeyUp(Integer 75, Integer 0)"], raised);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ALetterThatGrowsTheWordOfADropdownListIntoNoMatch_SelectsTheNextItemItStarts_AsInAListBox_ListClosedOrOpen(bool open)
    {
        var (picked, clicks) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["kiwi", "banana", "Kumquat", "apple"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            if (open)
            {
                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();
            }
            sink.Raised.Clear();
            var indexes = new List<int>();
            foreach (var (key, physical, text) in new[] { (Key.K, PhysicalKey.K, "k"), (Key.K, PhysicalKey.K, "k"), (Key.K, PhysicalKey.K, "k"),
                         (Key.B, PhysicalKey.B, "b"), (Key.B, PhysicalKey.B, "b"), (Key.Z, PhysicalKey.Z, "z") })
            {
                Type(window, key, physical, text);
                indexes.Add(combo.ListIndex);
            }
            var result = (indexes, Raised(sink).Count(e => e == "Combo1.Click"));
            window.Close();
            return result;
        });

        // No item matches kk, bb or bz: from none selected, kiwi, then Kumquat, then kiwi again from the top; banana, and banana again
        // raises no Click; no item matches z.
        CollectionAssert.AreEqual(new[] { 0, 2, 0, 1, 1, 1 }, picked);
        Assert.AreEqual(4, clicks);
    }

    [TestMethod]
    public void TheLettersTypedInADropdownList_AddUpIntoAWord_ThatSelectsAnItemItStarts_BeforeOneWhoseHumpsItMatches()
    {
        var (picked, clicks) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["MultiPlayer", "banana", "Mango", "MPeg", "kiwi"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            var indexes = new List<int>();
            foreach (var (key, physical, text) in new[] { (Key.M, PhysicalKey.M, "m"), (Key.P, PhysicalKey.P, "p"), (Key.L, PhysicalKey.L, "l"),
                         (Key.X, PhysicalKey.X, "x"), (Key.B, PhysicalKey.B, "b") })
            {
                Type(window, key, physical, text);
                indexes.Add(combo.ListIndex);
            }
            var result = (indexes, Raised(sink).Count(e => e == "Combo1.Click"));
            window.Close();
            return result;
        });

        // m: MultiPlayer, the first item it starts; mp: MPeg, which it starts, before MultiPlayer, whose humps it matches; mpl:
        // MultiPlayer's humps alone. No item matches mplx, or x: the word is forgotten, and b starts a new one.
        CollectionAssert.AreEqual(new[] { 0, 3, 0, 0, 1 }, picked);
        Assert.AreEqual(4, clicks);
    }

    // The items a combo's open list shows, from the top.
    private static List<int> Shown(VBComboBox combo)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        return [.. Enumerable.Range(0, combo.ItemCount).Select(combo.ContainerFromIndex).OfType<Control>().Where(item => item.IsVisible)
            .OrderBy(item => item.Bounds.Y).Select(combo.IndexFromContainer)];
    }

    [TestMethod]
    public void TypingInADropdownCombo_OpensItsListOnTheItemsItMatches_BestFirst_AndEnterChoosesTheHighlightedOne()
    {
        var (log, shown, typed, chosen) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d)
                .Set(P.List, ["apple", "MultiPlayer", "banana", "MPeg", "Mango"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            var log = Log(sink, root);
            Type(window, Key.M, PhysicalKey.M, "m");
            var afterM = Shown(combo);
            Type(window, Key.P, PhysicalKey.P, "p");
            var afterMP = Shown(combo);
            var typed = (combo.Text, combo.ListIndex);
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            Type(window, Key.Enter, PhysicalKey.Enter, null);
            var result = (log.Where(e => !e.StartsWith("Key", StringComparison.Ordinal)).ToList(), (afterM, afterMP), typed,
                (combo.Text, combo.ListIndex, combo.IsDropDownOpen));
            window.Close();
            return result;
        });

        // Down highlights MultiPlayer, whose humps mp matches, after MPeg, which it starts; the text stays mp until Enter.
        Expect.Lines(["Change", "DropDown", "Change", "Click"], log);
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, shown.afterM);
        CollectionAssert.AreEqual(new[] { 3, 1 }, shown.afterMP);
        Assert.AreEqual(("mp", -1), typed);
        Assert.AreEqual(("MultiPlayer", 1, false), chosen);
    }

    [TestMethod]
    public void ADropdownCombosList_ClosesWhereItsTextMatchesNoItem_AndTheTextGoesOnAsTyped()
    {
        var (log, text, open) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["kiwi", "Kumquat"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            var log = Log(sink, root);
            Type(window, Key.K, PhysicalKey.K, "k");
            Type(window, Key.X, PhysicalKey.X, "x");
            Type(window, Key.Y, PhysicalKey.Y, "y");
            var result = (log.Where(e => !e.StartsWith("Key", StringComparison.Ordinal)).ToList(), combo.Text, combo.IsDropDownOpen);
            window.Close();
            return result;
        });

        Expect.Lines(["Change", "DropDown", "Change", "Change"], log);
        Assert.AreEqual("kxy", text);
        Assert.IsFalse(open);
    }

    [TestMethod]
    public void TypingOverAnItemChosenInADropdownCombo_RaisesChange()
    {
        var (log, text, index) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            var log = Log(sink, root);
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            Type(window, Key.B, PhysicalKey.B, "b");
            var result = (log.Where(e => !e.StartsWith("Key", StringComparison.Ordinal)).ToList(), combo.Text, combo.ListIndex);
            window.Close();
            return result;
        });

        // The chosen item's text is selected in the box, and the letter replaces it.
        Expect.Lines(["Click", "Change", "DropDown"], log);
        Assert.AreEqual("b", text);
        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DownInAComboBox_ListClosedOrOpen_RaisesKeyDown_ThenClick_ThenKeyUp(bool open)
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            // An item selected, so that the open list puts the focus on it.
            combo.ListIndex = 0;
            if (open)
            {
                combo.IsDropDownOpen = true;
                Dispatcher.UIThread.RunJobs();
            }
            sink.Raised.Clear();
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Combo1.KeyDown(Integer 40, Integer 0)", "Combo1.Click", "Combo1.KeyUp(Integer 40, Integer 0)"], raised);
    }

    [TestMethod]
    public void AltDownOnAComboBox_RaisesBothKeysDowns_ThenDropDown_ThenBothKeyUps()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var log = Log(sink, root);
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            window.KeyPress(Key.Down, RawInputModifiers.Alt, PhysicalKey.ArrowDown, null);
            window.KeyRelease(Key.Down, RawInputModifiers.Alt, PhysicalKey.ArrowDown, null);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            var result = log.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["KeyDown 18", "KeyDown 40", "DropDown", "KeyUp 40", "KeyUp 18"], raised);
    }

    [TestMethod]
    public void F4OnAComboBox_RaisesKeyDown_ThenDropDown_ThenKeyUp()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var log = Log(sink, root);
            Type(window, Key.F4, PhysicalKey.F4, null);
            var result = log.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["KeyDown 115", "DropDown", "KeyUp 115"], raised);
    }

    [TestMethod]
    public void AKeyAsciiChanged_InAComboBoxsKeyPress_TypesTheNewCharacter()
    {
        var text = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, VBEvents.KeyPress))
                    arguments[0] = FormValue.FromInteger(75);
            };
            Type(window, Key.K, PhysicalKey.K, "k");
            window.Close();
            return combo1.Get(P.Text);
        });

        Assert.AreEqual("K", text);
    }

    [TestMethod]
    public void ALetterThenAnArrowInAListBox_MovesOnFromTheItemTheLetterSelected()
    {
        var index = UI.Run(() =>
        {
            var root = Root();
            var list1 = Add(root, VBClasses.ListBox, "List1").Set(P.List, ["alpha", "beta", "kappa", "omega"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (VBListBox)window.View.ControlOf(list1)!;
            list.ListIndex = 0;
            Type(window, Key.K, PhysicalKey.K, "k");
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            var result = list.ListIndex;
            window.Close();
            return result;
        });

        Assert.AreEqual(3, index);
    }

    [TestMethod]
    public void AKeyAsciiOf0_InAComboBoxsKeyPress_TypesNothing()
    {
        var (raised, text) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, VBEvents.KeyPress))
                    arguments[0] = FormValue.FromInteger(0);
            };
            Type(window, Key.K, PhysicalKey.K, "k");
            var result = (Raised(sink), combo1.Get(P.Text));
            window.Close();
            return result;
        });

        Expect.Lines(["Combo1.KeyDown(Integer 75, Integer 0)", "Combo1.KeyPress(Integer 107)", "Combo1.KeyUp(Integer 75, Integer 0)"], raised);
        Assert.AreEqual("", text);
    }

    // The order measured with KeyPreview: the form's event, then the control's, one event at a time, and the control's action before
    // the form's KeyUp.
    [TestMethod]
    public void WithKeyPreview_TheFormRaisesEachKeyEventBeforeTheControl()
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, true);
            var list1 = Add(root, VBClasses.ListBox, "List1").Set(P.List, ["alpha", "kappa"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            Type(window, Key.K, PhysicalKey.K, "k");
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.KeyDown(Integer 75, Integer 0)", "List1.KeyDown(Integer 75, Integer 0)", "Form1.KeyPress(Integer 107)",
            "List1.KeyPress(Integer 107)", "List1.Click", "Form1.KeyUp(Integer 75, Integer 0)", "List1.KeyUp(Integer 75, Integer 0)"], raised);
    }

    [TestMethod]
    public void AKeyCodeOf0_InTheFormsKeyDown_RaisesNoKeyDownOnTheControl_AndTheKeyStillMovesTheSelection()
    {
        var (raised, index) = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, true);
            var list1 = Add(root, VBClasses.ListBox, "List1").Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (VBListBox)window.View.ControlOf(list1)!;
            list.ListIndex = 0;
            sink.Raised.Clear();
            sink.Handler = (source, definition, arguments) =>
            {
                if (ReferenceEquals(source, root) && ReferenceEquals(definition, VBEvents.KeyDown))
                    arguments[0] = FormValue.FromInteger(0);
            };
            Type(window, Key.Down, PhysicalKey.ArrowDown, null);
            var result = (Raised(sink), list.ListIndex);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.KeyDown(Integer 40, Integer 0)", "List1.Click", "Form1.KeyUp(Integer 40, Integer 0)", "List1.KeyUp(Integer 40, Integer 0)"],
            raised);
        Assert.AreEqual(1, index);
    }
}
