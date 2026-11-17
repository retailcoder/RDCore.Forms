using Avalonia.Headless;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Controls;
using static RDCore.Forms.Tests.Model.TestPictures;
using M = RDCore.Forms.Model.Methods;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// A ListBox's and a ComboBox's list members, as a program's code calls them on the form's window: AddItem, RemoveItem, Clear, List,
// ItemData, ItemIcon, Selected, ListIndex, ListCount, NewIndex, SelCount and Text. The errors are VB6's, as lists.md gives them; List
// outside the items and List assigned are RDForms' rules, and ItemIcon is an RD-VBA addition.
[TestClass]
public sealed class ListMemberTests
{
    private const string ListBox = "RDForms.ListBox";
    private const string ComboBox = "RDForms.ComboBox";

    // A shown form with one list of the class, holding the items, and the window that shows it.
    private static (RDFormsFormWindow Window, ComponentInstance List, RecordingSink Sink) Open(string type, params string[] items) =>
        Open(ComponentClasses.All.Single(c => c.TypeName == type), null, items);

    private static (RDFormsFormWindow Window, ComponentInstance List, RecordingSink Sink) Open(ComponentClass type, ComboBoxStyle? style, string[] items)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var list = new ComponentInstance(type, "List1").Set(P.Left, 8d).Set(P.Top, 8d).Set(P.Width, 120d).Set(P.Height, 90d)
            .Set(P.List, items);
        if (style is { } chosen)
            list.Set(P.ComboBoxStyle, chosen);
        list.SetContainer(root);
        var sink = new RecordingSink();
        var window = new RDFormsFormWindow(new FormDefinition(root));
        RDFormsControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return (window, list, sink);
    }

    private static FormValue S(string text) => FormValue.FromString(text);

    private static FormValue I(int value) => FormValue.FromInteger((short)value);

    private static FormValue Get(RDFormsFormWindow window, ComponentInstance list, MethodDefinition member, params FormValue?[] arguments) =>
        window.Invoke(list, member, arguments, CodeUnit.VB6);

    private static void Call(RDFormsFormWindow window, ComponentInstance list, MethodDefinition member, params FormValue?[] arguments) =>
        window.Invoke(list, member, arguments, CodeUnit.VB6);

    // The items as List(0) to List(ListCount - 1) read them.
    private static string Items(RDFormsFormWindow window, ComponentInstance list)
    {
        var count = Get(window, list, M.ListCount).AsInteger();
        return string.Join(", ", Enumerable.Range(0, count).Select(i => Get(window, list, M.List, I(i)).AsString()));
    }

    // The error a call answers with, and its description, or 0.
    private static (int Number, string Description) ErrorOf(Action call)
    {
        try
        {
            call();
            return (0, "");
        }
        catch (FormErrorException e)
        {
            return (e.Error.Number, e.Error.Description);
        }
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void AddItem_AddsAtTheEnd_OrAtItsIndex_AndNewIndexIsWhereTheItemWent(string type)
    {
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(type);
            List<string> result = [$"{Get(window, list, M.NewIndex).AsInteger()}"];
            Call(window, list, M.AddItem, S("Human"));
            Call(window, list, M.AddItem, S("Merciless AI"));
            result.Add($"{Items(window, list)} | {Get(window, list, M.NewIndex).AsInteger()}");
            Call(window, list, M.AddItem, S("Random AI"), I(0));
            result.Add($"{Items(window, list)} | {Get(window, list, M.NewIndex).AsInteger()} | {Get(window, list, M.ItemData, I(0)).AsLong()}");
            window.Close();
            return result;
        });

        Expect.Lines(["-1", "Human, Merciless AI | 1", "Random AI, Human, Merciless AI | 0 | 0"], lines);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void RemoveItemAndClear_RemoveItems_AndLeaveNoNewIndex(string type)
    {
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(type);
            Call(window, list, M.AddItem, S("a"));
            Call(window, list, M.AddItem, S("b"));
            Call(window, list, M.AddItem, S("c"));
            Call(window, list, M.RemoveItem, I(1));
            List<string> result = [$"{Items(window, list)} | {Get(window, list, M.NewIndex).AsInteger()}"];
            Call(window, list, M.AddItem, S("d"));
            Call(window, list, M.Clear);
            result.Add($"{Get(window, list, M.ListCount).AsInteger()} | {Get(window, list, M.NewIndex).AsInteger()} | {list.Get(P.List).Count}");
            window.Close();
            return result;
        });

        Expect.Lines(["a, c | -1", "0 | -1 | 0"], lines);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void AnIndexOutsideTheItems_Is5ForAddItemAndRemoveItem_381ForItemData_AndListReadsEmpty(string type)
    {
        var (add, remove, data, dataSet, list, none) = UI.Run(() =>
        {
            var (window, component, _) = Open(type, "one", "two");
            var result = (ErrorOf(() => Call(window, component, M.AddItem, S("x"), I(3))).Number,
                ErrorOf(() => Call(window, component, M.RemoveItem, I(2))).Number,
                ErrorOf(() => Get(window, component, M.ItemData, I(-1))).Number,
                ErrorOf(() => window.Assign(component, M.ItemData, [I(2)], FormValue.FromLong(7))).Number,
                Get(window, component, M.List, I(2)).AsString(),
                Get(window, component, M.List, I(-1)).AsString());
            window.Close();
            return result;
        });

        Assert.AreEqual(5, add);
        Assert.AreEqual(5, remove);
        Assert.AreEqual(381, data);
        Assert.AreEqual(381, dataSet);
        Assert.AreEqual("", list);
        Assert.AreEqual("", none);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void ItemData_IsTheLongOfEachItem_AndFollowsItsItem(string type)
    {
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(type);
            Call(window, list, M.AddItem, S("a"));
            window.Assign(list, M.ItemData, [I(0)], FormValue.FromLong(100000));
            Call(window, list, M.AddItem, S("b"), I(0));
            var result = $"{Get(window, list, M.ItemData, I(0)).AsLong()}, {Get(window, list, M.ItemData, I(1)).AsLong()}";
            window.Close();
            return result;
        });

        Assert.AreEqual("0, 100000", lines);
    }

    // What ItemIcon(0) to ItemIcon(ListCount - 1) read: "gif" for the picture, "-" for Nothing.
    private static string Icons(RDFormsFormWindow window, ComponentInstance list, byte[] gif) =>
        string.Join(", ", Enumerable.Range(0, Get(window, list, M.ListCount).AsInteger()).Select(i => Get(window, list, M.ItemIcon, I(i)) switch
        {
            { Kind: FormValueKind.Object, ClrValue: null } => "-",
            { Kind: FormValueKind.Object, ClrValue: var picture } when ReferenceEquals(picture, gif) => "gif",
            var other => $"{other.Kind} {other.ClrValue}",
        }));

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void ItemIcon_IsThePictureOfEachItem_AndFollowsItsItem_ThroughAddItemRemoveItemAndClear(string type)
    {
        var gif = Gif1x1;
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(type, "a", "b");
            window.Assign(list, M.ItemIcon, [I(1)], FormValue.FromObject(gif));
            var result = new List<string> { Icons(window, list, gif) };
            Call(window, list, M.AddItem, S("x"), I(0));
            result.Add(Icons(window, list, gif));
            Call(window, list, M.RemoveItem, I(1));
            result.Add(Icons(window, list, gif));
            window.Assign(list, M.ItemIcon, [I(1)], FormValue.Nothing);
            result.Add(Icons(window, list, gif));
            window.Assign(list, M.ItemIcon, [I(0)], FormValue.FromObject(gif));
            Call(window, list, M.Clear);
            Call(window, list, M.AddItem, S("y"));
            result.Add(Icons(window, list, gif));
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "-, gif", "-, -, gif", "-, gif", "-, -", "-" }, lines);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void ItemIcon_OutsideTheItemsIs381_AValueThatIsNoPictureIs13_AndAPictureTheInspectionRefusesIs481(string type)
    {
        var (read, assigned, text, number, refused) = UI.Run(() =>
        {
            var (window, list, _) = Open(type, "a");
            var result = (ErrorOf(() => Get(window, list, M.ItemIcon, I(1))).Number,
                ErrorOf(() => window.Assign(list, M.ItemIcon, [I(1)], FormValue.FromObject(Gif1x1))).Number,
                ErrorOf(() => window.Assign(list, M.ItemIcon, [I(0)], S("a.gif"))).Number,
                ErrorOf(() => window.Assign(list, M.ItemIcon, [I(0)], FormValue.FromLong(1))).Number,
                ErrorOf(() => window.Assign(list, M.ItemIcon, [I(0)], FormValue.FromObject(Hex("89504E47")))).Number);
            window.Close();
            return result;
        });

        Assert.AreEqual(381, read);
        Assert.AreEqual(381, assigned);
        Assert.AreEqual(13, text);
        Assert.AreEqual(13, number);
        Assert.AreEqual(481, refused);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void AssigningList_ChangesTheItemsText_AndKeepsItsDataAndSelection_RaisingNothing(string type)
    {
        var (lines, raised) = UI.Run(() =>
        {
            var (window, list, sink) = Open(type, "one", "two");
            window.Assign(list, M.ItemData, [I(1)], FormValue.FromLong(42));
            window.Assign(list, M.ListIndex, [], I(1));
            sink.Raised.Clear();
            window.Assign(list, M.List, [I(1)], S("TWO"));
            var result = $"{Items(window, list)} | {Get(window, list, M.ItemData, I(1)).AsLong()} | {Get(window, list, M.ListIndex).AsInteger()} | "
                + string.Join(", ", list.Get(P.List));
            var outside = ErrorOf(() => window.Assign(list, M.List, [I(2)], S("three"))).Number;
            window.Close();
            return ($"{result} | {outside}", sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal)).ToList());
        });

        Assert.AreEqual("one, TWO | 42 | 1 | one, TWO | 381", lines);
        Expect.Lines([], raised);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void AssigningList_KeepsTheItemsIcon_AndNewIndex(string type)
    {
        var (icons, newIndex) = UI.Run(() =>
        {
            var (window, list, _) = Open(type);
            Call(window, list, M.AddItem, S("one"));
            Call(window, list, M.AddItem, S("two"));
            var icon = PictureControlTests.Png(16, 16, [.. Enumerable.Repeat(0xFFFF0000u, 256)]);
            list.Set(P.ItemIcon, [null, icon]);
            window.Assign(list, M.List, [I(1)], S("TWO"));
            var result = (list.Get(P.ItemIcon) is [null, var kept] && ReferenceEquals(kept, icon), Get(window, list, M.NewIndex).AsInteger());
            window.Close();
            return result;
        });

        Assert.IsTrue(icons, "the item keeps its icon");
        Assert.AreEqual(1, newIndex);
    }

    [TestMethod]
    public void AssigningTheSelectedItemsList_PutsItsTextInADropdownCombosEditBox_AsADropdownListsText()
    {
        var (dropdown, list) = UI.Run(() =>
        {
            string TextAfter(ComboBoxStyle style)
            {
                var (window, combo, _) = Open(ComponentClasses.ComboBox, style, ["one", "two"]);
                window.Assign(combo, M.ListIndex, [], I(1));
                window.Assign(combo, M.List, [I(1)], S("TWO"));
                var text = $"{Get(window, combo, M.ListText).AsString()} | {Get(window, combo, M.ListIndex).AsInteger()}"
                    + (style == ComboBoxStyle.DropdownCombo ? $" | {combo.Get(P.Text)}" : "");
                window.Close();
                return text;
            }

            return (TextAfter(ComboBoxStyle.DropdownCombo), TextAfter(ComboBoxStyle.DropdownList));
        });

        Assert.AreEqual("TWO | 1 | TWO", dropdown);
        Assert.AreEqual("TWO | 1", list);
    }

    [TestMethod]
    public void AssigningList_OnAMultiSelectListBox_KeepsItsSelectionAndItsFocus()
    {
        var lines = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
            var list = new ComponentInstance(ComponentClasses.ListBox, "List1").Set(P.Left, 8d).Set(P.Top, 8d).Set(P.Width, 120d).Set(P.Height, 90d)
                .Set(P.List, ["a", "b", "c"]).Set(P.MultiSelect, ListBoxMultiSelect.Simple);
            list.SetContainer(root);
            var window = new RDFormsFormWindow(new FormDefinition(root));
            RDFormsControl.SetEventSink(window, new RecordingSink());
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.Assign(list, M.Selected, [I(0)], FormValue.FromBoolean(true));
            window.Assign(list, M.Selected, [I(2)], FormValue.FromBoolean(true));
            window.Assign(list, M.ListIndex, [], I(1));
            window.Assign(list, M.List, [I(2)], S("C"));
            window.Assign(list, M.List, [I(1)], S("B"));
            var result = $"{Items(window, list)} | {string.Join(", ", Enumerable.Range(0, 3).Select(i => Get(window, list, M.Selected, I(i)).AsBoolean()))} | "
                + $"{Get(window, list, M.SelCount).AsInteger()} | {Get(window, list, M.ListIndex).AsInteger()}";
            window.Close();
            return result;
        });

        Assert.AreEqual("a, B, C | True, False, True | 2 | 1", lines);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void NewIndex_IsMinus1_AfterTheComponentsListIsReplaced(string type)
    {
        var newIndex = UI.Run(() =>
        {
            var (window, list, _) = Open(type);
            Call(window, list, M.AddItem, S("one"));
            list.Set(P.List, ["x", "y"]);
            var result = Get(window, list, M.NewIndex).AsInteger();
            window.Close();
            return result;
        });

        Assert.AreEqual(-1, newIndex);
    }

    [TestMethod]
    public void AssigningText_SelectsAListBoxsItem_AndIsADropdownCombosEditText()
    {
        var (listBox, combo) = UI.Run(() =>
        {
            var (window, list, _) = Open(ListBox, "Human", "Merciless AI");
            window.Assign(list, M.ListText, [], S("merciless ai"));
            var listResult = $"{Get(window, list, M.ListIndex).AsInteger()} | {Get(window, list, M.ListText).AsString()}";
            window.Close();

            var (comboWindow, dropdown, _) = Open(ComponentClasses.ComboBox, ComboBoxStyle.DropdownCombo, ["Human", "Merciless AI"]);
            comboWindow.Assign(dropdown, M.ListText, [], S("Someone"));
            var comboResult = $"{Get(comboWindow, dropdown, M.ListIndex).AsInteger()} | {Get(comboWindow, dropdown, M.ListText).AsString()} | {dropdown.Get(P.Text)}";
            comboWindow.Close();
            return (listResult, comboResult);
        });

        Assert.AreEqual("1 | Merciless AI", listBox);
        Assert.AreEqual("-1 | Someone | Someone", combo);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void ListIndex_SelectsAnItem_RaisingClickWhenItMoves_And380OutsideMinus1ToListCountMinus1(string type)
    {
        var (lines, raised) = UI.Run(() =>
        {
            var (window, list, sink) = Open(type, "Human", "Merciless AI");
            List<string> result = [$"{Get(window, list, M.ListIndex).AsInteger()}"];
            window.Assign(list, M.ListIndex, [], I(1));
            window.Assign(list, M.ListIndex, [], I(1));
            result.Add($"{Get(window, list, M.ListIndex).AsInteger()} | {Get(window, list, M.ListText).AsString()}");
            result.Add($"{ErrorOf(() => window.Assign(list, M.ListIndex, [], I(2))).Number}, {ErrorOf(() => window.Assign(list, M.ListIndex, [], I(-2))).Number}");
            window.Assign(list, M.ListIndex, [], I(-1));
            result.Add($"{Get(window, list, M.ListIndex).AsInteger()}");
            window.Close();
            return (result, sink.Raised.Where(e => e.EndsWith(".Click", StringComparison.Ordinal)).ToList());
        });

        Expect.Lines(["-1", "1 | Merciless AI", "380, 380", "-1"], lines);
        Expect.Lines(["List1.Click", "List1.Click"], raised);
    }

    [TestMethod]
    public void ADropdownListsText_IsItsSelectedItem_AndAssigningItSelectsTheItem_Or383()
    {
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(ComponentClasses.ComboBox, ComboBoxStyle.DropdownList, ["Human", "Merciless AI"]);
            window.Assign(list, M.ListIndex, [], I(1));
            List<string> result = [Get(window, list, M.ListText).AsString()];
            window.Assign(list, M.ListText, [], S("human"));
            result.Add($"{Get(window, list, M.ListIndex).AsInteger()} | {Get(window, list, M.ListText).AsString()}");
            var (number, description) = ErrorOf(() => window.Assign(list, M.ListText, [], S("nobody")));
            result.Add($"{number} {description}");
            window.Close();
            return result;
        });

        Expect.Lines(["Merciless AI", "0 | Human", $"383 {FormError.PropertyIsReadOnly("Text").Description}"], lines);
    }

    [TestMethod]
    [DataRow(ListBox, "ListCount")]
    [DataRow(ListBox, "NewIndex")]
    [DataRow(ListBox, "SelCount")]
    [DataRow(ComboBox, "ListCount")]
    [DataRow(ComboBox, "NewIndex")]
    public void AReadOnlyListProperty_Is383WithItsName_AndAValueOfTheWrongType13First(string type, string name)
    {
        var (assigned, wrongType) = UI.Run(() =>
        {
            var (window, list, _) = Open(type, "one");
            var member = list.Class.Method(name)!;
            var result = (ErrorOf(() => window.Assign(list, member, [], I(0))), ErrorOf(() => window.Assign(list, member, [], S("x"))).Number);
            window.Close();
            return result;
        });

        Assert.AreEqual((383, FormError.PropertyIsReadOnly(name).Description), assigned);
        Assert.AreEqual(13, wrongType);
    }

    [TestMethod]
    public void AListBoxsSelected_ReadsAndSetsAnItemsSelection_AndSelCountCountsThem()
    {
        var lines = UI.Run(() =>
        {
            var (window, list, _) = Open(ListBox, "one", "two");
            List<string> result = [$"{Get(window, list, M.Selected, I(1)).AsBoolean()} | {Get(window, list, M.SelCount).AsInteger()}"];
            window.Assign(list, M.Selected, [I(1)], FormValue.FromBoolean(true));
            result.Add($"{Get(window, list, M.Selected, I(1)).AsBoolean()} | {Get(window, list, M.SelCount).AsInteger()} | {Get(window, list, M.ListIndex).AsInteger()}");
            result.Add($"{ErrorOf(() => Get(window, list, M.Selected, I(2))).Number}");
            window.Close();
            return result;
        });

        Expect.Lines(["False | 0", "True | 1 | 1", "381"], lines);
    }

    [TestMethod]
    public void AComboBox_HasNoSelectedAndNoSelCount_AndAListBoxNoColumn_AsVB6sHaveNone()
    {
        Assert.IsNull(ComponentClasses.ComboBox.Method("Selected"));
        Assert.IsNull(ComponentClasses.ComboBox.Method("SelCount"));
        // Column is an MSForms ListBox's: VB6's ListBox has no member of that name, and a program's call is 438.
        Assert.IsNull(ComponentClasses.ListBox.Method("Column"));
        Assert.IsNull(ComponentClasses.ListBox.Property("Column"));
    }

    [TestMethod]
    public void List_WithoutItsIndex_Is449_AndWithTwo450()
    {
        var (none, two) = UI.Run(() =>
        {
            var (window, list, _) = Open(ListBox, "one");
            var result = (ErrorOf(() => Get(window, list, M.List)).Number, ErrorOf(() => Get(window, list, M.List, I(0), I(0))).Number);
            window.Close();
            return result;
        });

        Assert.AreEqual(449, none);
        Assert.AreEqual(450, two);
    }

    [TestMethod]
    public void AnIndex_IsAnInteger_RoundedHalfToEven_AndNullIs94()
    {
        var (rounded, ofNull, overflow) = UI.Run(() =>
        {
            var (window, list, _) = Open(ListBox, "zero", "one", "two");
            var result = (Get(window, list, M.List, FormValue.FromDouble(1.5)).AsString(),
                ErrorOf(() => Get(window, list, M.List, FormValue.Null)).Number,
                ErrorOf(() => Get(window, list, M.List, FormValue.FromLong(40000))).Number);
            window.Close();
            return result;
        });

        Assert.AreEqual("two", rounded);
        Assert.AreEqual(94, ofNull);
        Assert.AreEqual(6, overflow);
    }

    [TestMethod]
    [DataRow(ListBox)]
    [DataRow(ComboBox)]
    public void AListAddedAtRunTime_TakesTheListMembers_AsADesignedOneDoes(string type)
    {
        var lines = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
            var form = new FormDefinition(root);
            var window = new RDFormsFormWindow(form);
            RDFormsControl.SetEventSink(window, new RecordingSink());
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var list = form.AddControl(type, "cboPlayer");
            Dispatcher.UIThread.RunJobs();
            Call(window, list, M.AddItem, S("Human"));
            Call(window, list, M.AddItem, S("Merciless AI"));
            window.Assign(list, M.ListIndex, [], I(1));
            var result = $"{Get(window, list, M.ListCount).AsInteger()} | {Get(window, list, M.ListIndex).AsInteger()} | {Get(window, list, M.ListText).AsString()}";
            window.Close();
            return result;
        });

        Assert.AreEqual("2 | 1 | Merciless AI", lines);
    }
}
