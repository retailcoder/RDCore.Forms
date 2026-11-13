using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Simple Combo, as measured with VB6, native and p-code agreeing; and a Locked combo, whose rules are RDForms' own.
[TestClass]
public sealed class ComboStyleTests
{
    private static readonly string[] Fruit = ["apple", "Banana", "cherry"];

    // Combo1, the combo under test, 100 pixels high, and Text1 under it.
    private static (ComponentInstance Root, ComponentInstance Combo) Form(ComboBoxStyle style, bool locked = false)
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var combo = Add(root, VBClasses.ComboBox, "Combo1", 8, 100).Set(P.ComboBoxStyle, style).Set(P.List, Fruit).Set(P.Locked, locked);
        Add(root, VBClasses.TextBox, "Text1", 160, 21);
        return (root, combo);
    }

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double top, double height)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, 8d).Set(P.Top, top).Set(P.Width, 140d).Set(P.Height, height);
        component.SetContainer(container);
        return component;
    }

    // The window, with a control focused and what was raised so far forgotten.
    private static (VBFormWindow Window, VBComboBox Combo) Open(ComponentInstance root, ComponentInstance combo, RecordingSink sink, string focused = "Combo1")
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var control = (VBComboBox)window.View.ControlOf(combo)!;
        window.View.ControlOf(root.Descendants().Single(component => component.Name == focused))!.Focus();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return (window, control);
    }

    // A left press and release on a visual of the combo, the click count the window system gave it.
    private static void Press(Visual target, int clickCount = 1)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
        var root = TopLevel.GetTopLevel(target)!;
        var point = target.TranslatePoint(new Point(4, 4), root) ?? default;
        var source = (Interactive)target;
        source.RaiseEvent(new PointerPressedEventArgs(source, pointer, root, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, clickCount));
        source.RaiseEvent(new PointerReleasedEventArgs(source, pointer, root, point, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();
    }

    private static Control Item(VBComboBox combo, int index) => combo.ContainerFromIndex(index)!;

    private static Border List(VBComboBox combo) => combo.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_List");

    private static void Hit(VBFormWindow window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
        window.KeyRelease(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Type(VBFormWindow window, string text)
    {
        window.KeyTextInput(text);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<string> Events(RecordingSink sink) =>
        [.. sink.WithoutMoves.Where(line => !line.Contains(".Key"))];

    [TestMethod]
    public void ASimpleCombo_ShowsItsEditBoxWithItsListUnderIt_AlwaysShown_AndNoButton()
    {
        var (editHeight, listTop, firstItemTop, open, buttons) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var (window, combo) = Open(root, instance, new RecordingSink());
            var edit = combo.GetVisualDescendants().OfType<TextBox>().Single();
            var list = List(combo);
            var result = (edit.Bounds.Height, list.TranslatePoint(default, combo)!.Value.Y, Item(combo, 0).TranslatePoint(default, combo)!.Value.Y,
                combo.IsDropDownOpen, combo.GetVisualDescendants().OfType<ToggleButton>().Count());
            window.Close();
            return result;
        });

        Assert.AreEqual(21d, editHeight);
        Assert.AreEqual(21d, listTop);
        Assert.IsTrue(firstItemTop >= 21, $"The first item is at {firstItemTop}, inside the edit box.");
        Assert.IsFalse(open);
        Assert.AreEqual(0, buttons);
    }

    [TestMethod]
    public void APressOnASimpleCombosList_PicksTheItemUnderIt_WithClickAndNoChange_TheOneSelectedToo_AndBelowTheItemsTheLast()
    {
        var (log, index, text, focusInEditBox) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink);
            var log = new List<string>();
            sink.Handler = (source, definition, _) => log.Add($"{source.Name}.{definition.Name} {combo.ListIndex} {combo.Text}");
            Press(Item(combo, 1));
            Press(Item(combo, 1));
            Press(List(combo).GetVisualDescendants().OfType<ScrollViewer>().Single());
            var edit = combo.GetVisualDescendants().OfType<TextBox>().Single();
            var result = (log.Where(line => !line.Contains(".Key")).ToList(), combo.ListIndex, instance.Get(P.Text), edit.IsFocused);
            window.Close();
            return result;
        });

        Expect.Lines(["Combo1.Click 1 Banana", "Combo1.Click 1 Banana", "Combo1.Click 2 cherry"], log);
        Assert.AreEqual((2, "cherry", true), (index, text, focusInEditBox));
    }

    [TestMethod]
    public void ADoubleClickOnASimpleCombosItem_IsOneClick_ThenDblClick()
    {
        var raised = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink);
            Press(Item(combo, 2), 1);
            Press(Item(combo, 2), 2);
            var result = Events(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Combo1.Click", "Combo1.DblClick"], raised);
    }

    [TestMethod]
    public void APressOnTheListOfASimpleComboWithoutTheFocus_RaisesGotFocusThenClick()
    {
        var raised = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink, focused: "Text1");
            Press(Item(combo, 0));
            var result = Events(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Text1.LostFocus", "Combo1.GotFocus", "Combo1.Click"], raised);
    }

    [TestMethod]
    public void F4AndAltDown_DropNothingInASimpleCombo_AndRaiseNoDropDown()
    {
        var (raised, open) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink);
            Hit(window, Key.F4, PhysicalKey.F4);
            window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            Hit(window, Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Alt);
            window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Dispatcher.UIThread.RunJobs();
            var result = (Events(sink), combo.IsDropDownOpen);
            window.Close();
            return result;
        });

        Assert.IsEmpty(raised);
        Assert.IsFalse(open);
    }

    [TestMethod]
    public void TypingInASimpleCombo_IsAChange_AndSelectsNothing_AndDownPicksTheFirstItem()
    {
        var (log, narrowed) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink);
            var log = new List<string>();
            sink.Handler = (source, definition, _) => log.Add($"{source.Name}.{definition.Name} {combo.ListIndex} {combo.Text}");
            Type(window, "b");
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var hidden = Enumerable.Range(0, combo.ItemCount).Count(i => !Item(combo, i).IsVisible);
            Hit(window, Key.Down, PhysicalKey.ArrowDown);
            var result = (log.Where(line => !line.Contains(".Key")).ToList(), hidden);
            window.Close();
            return result;
        });

        Expect.Lines(["Combo1.Change -1 b", "Combo1.Click 0 apple"], log);
        Assert.AreEqual(0, narrowed);
    }

    [TestMethod]
    [DataRow(ComboBoxStyle.DropdownCombo)]
    [DataRow(ComboBoxStyle.SimpleCombo)]
    [DataRow(ComboBoxStyle.DropdownList)]
    public void ALockedCombo_TakesNoTypingAndNoPick_AndDropsNoList_ButRaisesItsKeyEvents(ComboBoxStyle style)
    {
        var (raised, index, text, open) = UI.Run(() =>
        {
            var (root, instance) = Form(style, locked: true);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink);
            Type(window, "b");
            Hit(window, Key.Down, PhysicalKey.ArrowDown);
            Hit(window, Key.F4, PhysicalKey.F4);
            if (style == ComboBoxStyle.SimpleCombo)
                Press(Item(combo, 1));
            var result = (sink.WithoutMoves, combo.ListIndex, combo.Text, combo.IsDropDownOpen);
            window.Close();
            return result;
        });

        Assert.AreEqual((-1, "", false), (index, text, open));
        CollectionAssert.Contains(raised, "Combo1.KeyPress(Integer 98)");
        CollectionAssert.Contains(raised, "Combo1.KeyDown(Integer 40, Integer 0)");
        Assert.IsFalse(raised.Any(line => line is "Combo1.Click" or "Combo1.Change" or "Combo1.DropDown"), string.Join(", ", raised));
    }

    [TestMethod]
    public void ALockedCombo_TakesCodesTextAndListIndex()
    {
        var (text, index) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.DropdownCombo, locked: true);
            var (window, combo) = Open(root, instance, new RecordingSink());
            combo.SetText("typed by code");
            var text = combo.Text;
            combo.ListIndex = 2;
            var result = (text, combo.ListIndex);
            window.Close();
            return result;
        });

        Assert.AreEqual(("typed by code", 2), (text, index));
    }

    [TestMethod]
    public void Locked_SetFromCode_KeepsTheUserOut_AndCleared_LetsThemIn()
    {
        var (whileLocked, after) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.DropdownCombo);
            var (window, combo) = Open(root, instance, new RecordingSink());
            instance.Set(P.Locked, true);
            Type(window, "a");
            var whileLocked = combo.Text;
            instance.Set(P.Locked, false);
            Type(window, "b");
            var result = (whileLocked, combo.Text);
            window.Close();
            return result;
        });

        Assert.AreEqual(("", "b"), (whileLocked, after));
    }
}
