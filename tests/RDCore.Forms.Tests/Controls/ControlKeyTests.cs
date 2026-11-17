using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The key events of the controls that raise their own, in the orders measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class ControlKeyTests
{
    private static ComponentInstance Root() =>
        new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double top = 8)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, 8d).Set(P.Top, top).Set(P.Width, 120d).Set(P.Height, 80d);
        component.SetContainer(container);
        return component;
    }

    // The window, with the control focused and what was raised so far forgotten.
    private static RDFormsFormWindow Open(ComponentInstance root, ComponentInstance focused, RecordingSink sink)
    {
        var window = new RDFormsFormWindow(new FormDefinition(root));
        RDFormsControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.View.ControlOf(focused)!.Focus();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    // A key pressed, the character it types, and the key released. Where typed is false, the window system types no character for it,
    // as Windows does for a key a control handled as it went down.
    private static void Type(RDFormsFormWindow window, Key key, PhysicalKey physical, string? text, bool typed = true,
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
            var command1 = Add(root, ComponentClasses.CommandButton, "Command1");
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
            var button = Add(root, ComponentClasses.ByTypeName("RDForms." + type)!, "Button1");
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

    // RDForms' own: unmeasured with VB6.
    [TestMethod]
    public void APictureBox_RaisesItsKeyEvents_AnArrowsIncluded_WhichKeepsTheFocus()
    {
        var (raised, focused) = UI.Run(() =>
        {
            var root = Root();
            var picture = Add(root, ComponentClasses.PictureBox, "Picture1");
            Add(root, ComponentClasses.TextBox, "Text1", top: 100);
            var sink = new RecordingSink();
            var window = Open(root, picture, sink);
            Type(window, Key.K, PhysicalKey.K, "k");
            Type(window, Key.Right, PhysicalKey.ArrowRight, null);
            var result = (Raised(sink), window.ActiveControl?.Name);
            window.Close();
            return result;
        });

        Expect.Lines(["Picture1.KeyDown(Integer 75, Integer 0)", "Picture1.KeyPress(Integer 107)", "Picture1.KeyUp(Integer 75, Integer 0)",
            "Picture1.KeyDown(Integer 39, Integer 0)", "Picture1.KeyUp(Integer 39, Integer 0)"], raised);
        Assert.AreEqual("Picture1", focused);
    }

    [TestMethod]
    public void AKeyInAControlAPictureBoxHolds_IsThatControlsAlone()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var picture = Add(root, ComponentClasses.PictureBox, "Picture1");
            var list = Add(picture, ComponentClasses.ListBox, "List1");
            var sink = new RecordingSink();
            var window = Open(root, list, sink);
            Type(window, Key.K, PhysicalKey.K, "k");
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["List1.KeyDown(Integer 75, Integer 0)", "List1.KeyPress(Integer 107)", "List1.KeyUp(Integer 75, Integer 0)"], raised);
    }

    [TestMethod]
    public void SpaceOnACheckBox_RaisesItsKeyEvents_ThenClick_AndOnTheSelectedOptionButtonNoClick()
    {
        var (check, option) = UI.Run(() =>
        {
            var root = Root();
            var check1 = Add(root, ComponentClasses.CheckBox, "Check1");
            var option1 = Add(root, ComponentClasses.OptionButton, "Option1", 100).Set(P.OptionButtonValue, true);
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
            var button1 = Add(root, ComponentClasses.ByTypeName("RDForms." + type)!, "Button1");
            var command2 = Add(root, ComponentClasses.CommandButton, "Command2", 100);
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
            var command1 = Add(root, ComponentClasses.CommandButton, "Command1");
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
            var text1 = Add(root, ComponentClasses.TextBox, "Text1").Set(P.Height, 21d);
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d);
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
            var list1 = Add(root, ComponentClasses.ListBox, "List1").Set(P.List, ["alpha", "beta", "gamma"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (RDFormsListBox)window.View.ControlOf(list1)!;
            list.ListIndex = 0;
            sink.Raised.Clear();
            int? atClick = null;
            sink.Handler = (_, definition, _) =>
            {
                if (ReferenceEquals(definition, Events.Click))
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
            var list1 = Add(root, ComponentClasses.ListBox, "List1").Set(P.List, ["Kilo", "alpha", "kappa", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (RDFormsListBox)window.View.ControlOf(list1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, style)
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["kiwi", "banana", "Kumquat", "apple"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["MultiPlayer", "banana", "Mango", "MPeg", "kiwi"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
    private static List<int> Shown(RDFormsComboBox combo)
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d)
                .Set(P.List, ["apple", "MultiPlayer", "banana", "MPeg", "Mango"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["kiwi", "Kumquat"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (RDFormsComboBox)window.View.ControlOf(combo1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d).Set(P.List, ["alpha", "beta"]);
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, Events.KeyPress))
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
            var list1 = Add(root, ComponentClasses.ListBox, "List1").Set(P.List, ["alpha", "beta", "kappa", "omega"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (RDFormsListBox)window.View.ControlOf(list1)!;
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
            var combo1 = Add(root, ComponentClasses.ComboBox, "Combo1").Set(P.Height, 21d);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, Events.KeyPress))
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
            var list1 = Add(root, ComponentClasses.ListBox, "List1").Set(P.List, ["alpha", "kappa"]);
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
            var list1 = Add(root, ComponentClasses.ListBox, "List1").Set(P.List, ["alpha", "beta"]);
            var sink = new RecordingSink();
            var window = Open(root, list1, sink);
            var list = (RDFormsListBox)window.View.ControlOf(list1)!;
            list.ListIndex = 0;
            sink.Raised.Clear();
            sink.Handler = (source, definition, arguments) =>
            {
                if (ReferenceEquals(source, root) && ReferenceEquals(definition, Events.KeyDown))
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

    // The keys the scroll bar grid was measured with: the key, its physical key, and the text it types.
    private static readonly (string Name, Key Key, PhysicalKey Physical, string? Text)[] ScrollBarKeys =
    [
        ("Left", Key.Left, PhysicalKey.ArrowLeft, null), ("Right", Key.Right, PhysicalKey.ArrowRight, null),
        ("Up", Key.Up, PhysicalKey.ArrowUp, null), ("Down", Key.Down, PhysicalKey.ArrowDown, null),
        ("PageUp", Key.PageUp, PhysicalKey.PageUp, null), ("PageDown", Key.PageDown, PhysicalKey.PageDown, null),
        ("Home", Key.Home, PhysicalKey.Home, null), ("End", Key.End, PhysicalKey.End, null), ("A", Key.A, PhysicalKey.A, "a"),
        ("Space", Key.Space, PhysicalKey.Space, " "), ("Enter", Key.Enter, PhysicalKey.Enter, null),
        ("Escape", Key.Escape, PhysicalKey.Escape, null), ("F2", Key.F2, PhysicalKey.F2, null), ("Delete", Key.Delete, PhysicalKey.Delete, null),
    ];

    // A scroll bar of the form, Value 50, SmallChange 1 and LargeChange 10, as the grid has it.
    private static ComponentInstance ScrollBar(ComponentInstance root, short min, short max) =>
        Add(root, ComponentClasses.HScrollBar, "HScroll1").Set(P.Height, 17d).Set(P.Min, min).Set(P.Max, max).Set(P.ScrollBarValue, (short)50)
            .Set(P.SmallChange, (short)1).Set(P.LargeChange, (short)10);

    // The bar's events as the grid logs them: a key event with its code, the form's marked F, and the bar's with its Value.
    private static List<string> Log(RecordingSink sink, ComponentInstance root, ComponentInstance bar)
    {
        var log = new List<string>();
        sink.Handler = (source, definition, arguments) =>
        {
            if (definition.Name is "MouseMove" or "GotFocus" or "LostFocus")
                return;
            var key = definition.Name is "KeyDown" or "KeyPress" or "KeyUp";
            log.Add((ReferenceEquals(source, root) ? "F" : "") + definition.Name + (key ? " " + arguments[0].AsInteger() : "")
                + (ReferenceEquals(source, root) ? "" : " v=" + bar.Get(P.ScrollBarValue)));
        };
        return log;
    }

    // Each key on the bar, its Value set back to a start before each, as "> key" and the events the key raised.
    private static List<string> TypeEach(RDFormsFormWindow window, ComponentInstance bar, List<string> log, short from = 50,
        params (string Name, Key Key, PhysicalKey Physical, string? Text)[] keys)
    {
        var lines = new List<string>();
        foreach (var (name, key, physical, text) in keys)
        {
            bar.Set(P.ScrollBarValue, from);
            Dispatcher.UIThread.RunJobs();
            log.Clear();
            Type(window, key, physical, text);
            lines.Add("> " + name);
            lines.AddRange(log);
        }
        return lines;
    }

    // The grid measured with VB6 on a bar from 0 to 100 and on one from 100 to 0: KeyDown before the key moves the bar, a Change where
    // it moved, and KeyUp after; a KeyPress for a key that types a character, Enter and Esc. Home takes it to the lower of Min and Max.
    [TestMethod]
    [DataRow((short)0, (short)100, new short[] { 49, 51, 49, 51, 40, 60, 0, 100 }, DisplayName = "Min 0, Max 100")]
    [DataRow((short)100, (short)0, new short[] { 51, 49, 51, 49, 60, 40, 0, 100 }, DisplayName = "Min 100, Max 0")]
    public void AScrollBar_RaisesItsKeyEvents_KeyDownBeforeTheKeyMovesIt_AndKeyUpAfter(short min, short max, short[] moved)
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var bar = ScrollBar(root, min, max);
            var sink = new RecordingSink();
            var window = Open(root, bar, sink);
            var result = TypeEach(window, bar, Log(sink, root, bar), keys: ScrollBarKeys);
            window.Close();
            return result;
        });

        var codes = new[] { 37, 39, 38, 40, 33, 34, 36, 35 };
        var expected = new List<string>();
        for (var i = 0; i < codes.Length; i++)
            expected.AddRange([$"> {ScrollBarKeys[i].Name}", $"KeyDown {codes[i]} v=50", $"Change v={moved[i]}", $"KeyUp {codes[i]} v={moved[i]}"]);
        foreach (var (name, code, ascii) in new (string, int, int?)[] { ("A", 65, 97), ("Space", 32, 32), ("Enter", 13, 13), ("Escape", 27, 27), ("F2", 113, null), ("Delete", 46, null) })
            expected.AddRange(ascii is { } a ? [$"> {name}", $"KeyDown {code} v=50", $"KeyPress {a} v=50", $"KeyUp {code} v=50"] : [$"> {name}", $"KeyDown {code} v=50", $"KeyUp {code} v=50"]);
        Expect.Lines(expected, raised);
    }

    [TestMethod]
    public void AScrollBarAtItsEnd_RaisesNoChangeForAKeyThatWouldTakeItPast_AndAPageStopsAtTheEnd()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var bar = ScrollBar(root, 0, 100);
            var sink = new RecordingSink();
            var window = Open(root, bar, sink);
            var log = Log(sink, root, bar);
            var result = TypeEach(window, bar, log, 0, ScrollBarKeys[0], ScrollBarKeys[6]);
            result.AddRange(TypeEach(window, bar, log, 100, ScrollBarKeys[1], ScrollBarKeys[7]));
            result.AddRange(TypeEach(window, bar, log, 95, ScrollBarKeys[5]));
            window.Close();
            return result;
        });

        Expect.Lines(["> Left", "KeyDown 37 v=0", "KeyUp 37 v=0", "> Home", "KeyDown 36 v=0", "KeyUp 36 v=0",
            "> Right", "KeyDown 39 v=100", "KeyUp 39 v=100", "> End", "KeyDown 35 v=100", "KeyUp 35 v=100",
            "> PageDown", "KeyDown 34 v=95", "Change v=100", "KeyUp 34 v=100"], raised);
    }

    // A KeyCode of 0 that the bar's own KeyDown leaves, or the form's with KeyPreview, cancels the key: the bar does not move.
    [TestMethod]
    [DataRow(false, DisplayName = "the bar's KeyDown")]
    [DataRow(true, DisplayName = "the form's KeyDown")]
    public void AKeyCodeOf0_InAScrollBarsKeyDown_OrTheForms_MovesNothing(bool byTheForm)
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, byTheForm);
            var bar = ScrollBar(root, 0, 100);
            var sink = new RecordingSink();
            var window = Open(root, bar, sink);
            var log = Log(sink, root, bar);
            var logging = sink.Handler!;
            sink.Handler = (source, definition, arguments) =>
            {
                logging(source, definition, arguments);
                if (ReferenceEquals(definition, Events.KeyDown) && ReferenceEquals(source, byTheForm ? root : bar))
                    arguments[0] = FormValue.FromInteger(0);
            };
            var result = TypeEach(window, bar, log, keys: [ScrollBarKeys[1], ScrollBarKeys[5], ScrollBarKeys[7]]);
            window.Close();
            return result;
        });

        Expect.Lines(byTheForm
            ? ["> Right", "FKeyDown 39", "FKeyUp 39", "KeyUp 39 v=50", "> PageDown", "FKeyDown 34", "FKeyUp 34", "KeyUp 34 v=50",
               "> End", "FKeyDown 35", "FKeyUp 35", "KeyUp 35 v=50"]
            : ["> Right", "KeyDown 39 v=50", "KeyUp 39 v=50", "> PageDown", "KeyDown 34 v=50", "KeyUp 34 v=50", "> End", "KeyDown 35 v=50", "KeyUp 35 v=50"],
            raised);
    }

    [TestMethod]
    public void WithKeyPreview_TheFormRaisesEachOfAScrollBarsKeyEventsFirst_AndTheBarMovesBeforeTheFormsKeyUp()
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, true);
            var bar = ScrollBar(root, 0, 100);
            var sink = new RecordingSink();
            var window = Open(root, bar, sink);
            var result = TypeEach(window, bar, Log(sink, root, bar), keys: [ScrollBarKeys[1], ScrollBarKeys[8]]);
            window.Close();
            return result;
        });

        Expect.Lines(["> Right", "FKeyDown 39", "KeyDown 39 v=50", "Change v=51", "FKeyUp 39", "KeyUp 39 v=51",
            "> A", "FKeyDown 65", "KeyDown 65 v=50", "FKeyPress 97", "KeyPress 97 v=50", "FKeyUp 65", "KeyUp 65 v=50"], raised);
    }
}
