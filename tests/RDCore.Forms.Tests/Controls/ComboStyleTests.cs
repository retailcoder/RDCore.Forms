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

    // A Simple Combo of a height, its IntegralHeight and font, in the window: the component's Height and the height drawn, after each
    // height given from code.
    private static List<(double Stored, double Drawn)> Heights(double height, bool integral, FormFont? font, params double[] fromCode)
    {
        return UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            instance.Set(P.Height, height).Set(P.IntegralHeight, integral);
            if (font is { } given)
                instance.Set(P.Font, given);
            var (window, combo) = Open(root, instance, new RecordingSink());
            var result = new List<(double, double)> { (instance.Get(P.Height), combo.Bounds.Height) };
            foreach (var asked in fromCode)
            {
                instance.Set(P.Height, asked);
                Dispatcher.UIThread.RunJobs();
                result.Add((instance.Get(P.Height), combo.Bounds.Height));
            }
            window.Close();
            return result;
        });
    }

    private static string Of(List<(double Stored, double Drawn)> heights) =>
        string.Join(", ", heights.Select(h => $"{Math.Round(h.Stored * 15)}/{h.Drawn}"));

    // The heights measured with VB6 at the default font, in twips (15 to a pixel) as the form gives them: rows of 13 pixels under an
    // edit box of 21, in a border of 4; whole pixels first, so that 1140 twips are 76 pixels and 1150 are 77.
    [TestMethod]
    [DataRow(1000, 960, 64)]
    [DataRow(1100, 960, 64)]
    [DataRow(1140, 960, 64)]
    [DataRow(1150, 1150, 77)]
    [DataRow(1200, 1155, 77)]
    [DataRow(1500, 1350, 90)]
    [DataRow(2000, 1935, 129)]
    [DataRow(600, 570, 38)]
    [DataRow(500, 315, 21)]
    [DataRow(150, 315, 21)]
    public void ASimpleCombosHeight_FromTheForm_IsWholeRowsOfItsList_OrItsEditBoxAlone(int twips, int stored, int drawn)
    {
        Assert.AreEqual($"{stored}/{drawn}", Of(Heights(twips / 15d, true, null)));
    }

    [TestMethod]
    [DataRow(1000, 1000, 67)]
    [DataRow(500, 500, 33)]
    [DataRow(375, 315, 21)]
    [DataRow(150, 315, 21)]
    public void WithIntegralHeightFalse_ASimpleCombo_TakesTheHeightItIsGiven_UnlessItsListWouldBeNoMoreThanItsBorder(int twips, int stored, int drawn)
    {
        Assert.AreEqual($"{stored}/{drawn}", Of(Heights(twips / 15d, false, null)));
    }

    // From code, a height of the edit box's or less is ignored, and the Height reads the pixels drawn: 1000 twips, 66.67 pixels, read
    // 1005 after one.
    [TestMethod]
    public void AHeightFromCode_IsSnappedAsTheFormsIs_AndOneOfTheEditBoxsOrLess_IsIgnored()
    {
        var snapped = Heights(1000 / 15d, true, null, 1234 / 15d, 300 / 15d, 450 / 15d, 1000 / 15d, 322 / 15d, 1160 / 15d);
        var unsnapped = Heights(1000 / 15d, false, null, 322 / 15d, 330 / 15d, 400 / 15d);

        Assert.AreEqual("960/64, 1155/77, 1155/77, 315/21, 960/64, 960/64, 1160/77", Of(snapped));
        Assert.AreEqual("1000/67, 1005/67, 315/21, 400/27", Of(unsnapped));
    }

    // MS Sans Serif 12 is drawn at its 16-pixel size, whose rows VB6 makes 20 pixels high, under an edit box of 28.
    [TestMethod]
    public void ASimpleCombosRowsAndEditBox_AreItsFontsLine()
    {
        var (heights, edit, row) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            instance.Set(P.Height, 100d).Set(P.Font, new FormFont("MS Sans Serif", 12));
            var (window, combo) = Open(root, instance, new RecordingSink());
            var result = (Of([(instance.Get(P.Height), combo.Bounds.Height)]),
                combo.GetVisualDescendants().OfType<TextBox>().Single().Bounds.Height, Item(combo, 0).Bounds.Height);
            window.Close();
            return result;
        });

        Assert.AreEqual(("1380/92", 28d, 20d), (heights, edit, row));
    }

    // A font from code keeps the list's pixels and sizes the edit box to the font, then snaps where IntegralHeight is True: 72
    // pixels at 12 points are 64 at 8, then 60 at 14; with IntegralHeight False, 100 pixels at 12 points are 93 at 8.
    [TestMethod]
    public void AFontFromCode_KeepsTheListsPixels_AndSnapsToTheNewRows()
    {
        var (snapped, unsnapped) = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var other = new ComponentInstance(VBClasses.ComboBox, "Combo2").Set(P.ComboBoxStyle, ComboBoxStyle.SimpleCombo)
                .Set(P.Left, 160d).Set(P.Top, 8d).Set(P.Width, 140d).Set(P.Height, 100d).Set(P.IntegralHeight, false);
            other.SetContainer(root);
            var large = new FormFont("MS Sans Serif", 12);
            instance.Set(P.Height, 1234 / 15d).Set(P.Font, large);
            other.Set(P.Font, large);
            var (window, combo) = Open(root, instance, new RecordingSink());
            var second = window.View.ControlOf(other)!;
            var heights = new List<(double, double)> { (instance.Get(P.Height), combo.Bounds.Height) };
            foreach (var size in new[] { 8.25, 14 })
            {
                instance.Set(P.Font, new FormFont("MS Sans Serif", size));
                Dispatcher.UIThread.RunJobs();
                heights.Add((instance.Get(P.Height), combo.Bounds.Height));
            }
            other.Set(P.Font, FormFont.Default);
            Dispatcher.UIThread.RunJobs();
            var result = (Of(heights), Of([(other.Get(P.Height), second.Bounds.Height)]));
            window.Close();
            return result;
        });

        Assert.AreEqual("1080/72, 960/64, 900/60", snapped);
        Assert.AreEqual("1395/93", unsnapped);
    }

    // As measured with VB6: the press selects the item before the focus moves, so the control that loses it and the combo's
    // GotFocus read the new ListIndex and the text from before; the text is the item's by the Click.
    [TestMethod]
    [DataRow(false, DisplayName = "an item selected")]
    [DataRow(true, DisplayName = "typed text")]
    public void APressThatMovesTheFocusToASimpleCombo_SelectsTheItemFirst_AndTheGotFocusReadsTheTextFromBefore(bool typed)
    {
        var log = UI.Run(() =>
        {
            var (root, instance) = Form(ComboBoxStyle.SimpleCombo);
            var sink = new RecordingSink();
            var (window, combo) = Open(root, instance, sink, focused: "Text1");
            if (typed)
                combo.SetText("typed");
            else
                combo.ListIndex = 0;
            Dispatcher.UIThread.RunJobs();
            var log = new List<string>();
            sink.Handler = (source, definition, _) => log.Add($"{source.Name}.{definition.Name} {combo.ListIndex} {combo.Text} {instance.Get(P.Text)}");
            Press(Item(combo, 2));
            window.Close();
            return log.Where(line => !line.Contains(".Key") && !line.Contains(".Mouse")).ToList();
        });

        var before = typed ? "typed" : "apple";
        Expect.Lines([$"Text1.LostFocus 2 {before} {before}", $"Combo1.GotFocus 2 {before} {before}", "Combo1.Click 2 cherry cherry"], log);
    }
}
