using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ListControlTests
{
    private static ComponentInstance List(string name, params string[] items) =>
        new ComponentInstance(VBClasses.ListBox, name).Set(P.List, items);

    [TestMethod]
    public void AListBoxsListIndexFromCode_RaisesClick_WhenItMoves_AndNoneWhenSetAgain()
    {
        var (index, text, raised) = UI.Run(() =>
        {
            var list = new VBListBox(List("List1", "one", "two", "three"));
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 1;
            list.ListIndex = 1;
            window.Close();
            return (list.ListIndex, list.Text, sink.Raised);
        });

        Assert.AreEqual(1, index);
        Assert.AreEqual("two", text);
        Expect.Lines(new[] { "List1.Click" }, raised);
    }

    [TestMethod]
    public void AListIndexOutsideTheItems_Is380_AndASelectedIndexOutsideThem381()
    {
        var (listIndex, selected) = UI.Run(() =>
        {
            var list = new VBListBox(List("List1", "one"));
            return (Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = 1).Error.Number,
                Assert.ThrowsExactly<FormErrorException>(() => list.GetSelected(-1)).Error.Number);
        });

        Assert.AreEqual(380, listIndex);
        Assert.AreEqual(381, selected);
    }

    [TestMethod]
    public void AddItemAndRemoveItem_ChangeTheComponentsList_AndKeepTheSelectionOnItsItem()
    {
        var (items, text) = UI.Run(() =>
        {
            var instance = List("List1", "b", "c");
            var list = new VBListBox(instance);
            var (window, _) = ControlHost.Show(list);
            list.ListIndex = 0;
            list.AddItem("a", 0);
            list.AddItem("d");
            list.RemoveItem(3);
            window.Close();
            return (instance.Get(P.List), list.Text);
        });

        Expect.Lines(new[] { "a", "b", "c" }, items.ToArray());
        Assert.AreEqual("b", text);
    }

    [TestMethod]
    public void AddItemPastTheEnd_Is5()
    {
        var number = UI.Run(() => Assert.ThrowsExactly<FormErrorException>(() => new VBListBox(List("List1", "a")).AddItem("x", 2)).Error.Number);

        Assert.AreEqual(5, number);
    }

    [TestMethod]
    public void AMultiSelectListsSelected_AddsAnItem_AndMovesTheFocusToIt()
    {
        var (count, index, raised) = UI.Run(() =>
        {
            var list = new VBListBox(List("List1", "a", "b", "c").Set(P.MultiSelect, ListBoxMultiSelect.Simple));
            var (window, sink) = ControlHost.Show(list);
            list.SetSelected(2, true);
            list.SetSelected(0, true);
            list.SetSelected(0, true);
            window.Close();
            return (list.SelCount, list.ListIndex, sink.Raised.Count);
        });

        Assert.AreEqual(2, count);
        Assert.AreEqual(0, index);
        Assert.AreEqual(2, raised);
    }

    [TestMethod]
    public void ADropdownListsText_SelectsTheItemWithoutRegardToCase_AndAnyOtherTextIs383()
    {
        var (index, raised, error) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.ComboBox, "Combo1").Set(P.List, ["Red", "Green"]).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList);
            var combo = new VBComboBox(instance);
            var (window, sink) = ControlHost.Show(combo);
            combo.SetText("green");
            combo.SetText("green");
            var error = Assert.ThrowsExactly<FormErrorException>(() => combo.SetText("blue")).Error;
            window.Close();
            return (combo.ListIndex, sink.Raised, error);
        });

        Assert.AreEqual(1, index);
        Expect.Lines(new[] { "Combo1.Click", "Combo1.Click" }, raised);
        Assert.AreEqual(new FormError(383, "'Text' property is read-only"), error);
    }

    [TestMethod]
    public void ADropdownCombosText_FromCode_RaisesChange_AndSelectsNoItem_EvenAnItemsText()
    {
        var (text, index, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.ComboBox, "Combo1").Set(P.List, ["Red", "Green"]);
            var combo = new VBComboBox(instance);
            var (window, sink) = ControlHost.Show(combo);
            combo.SetText("Red");
            combo.SetText("Red");
            window.Close();
            return (instance.Get(P.Text), combo.ListIndex, sink.Raised);
        });

        Assert.AreEqual("Red", text);
        Assert.AreEqual(-1, index);
        Expect.Lines(new[] { "Combo1.Change" }, raised);
    }

    [TestMethod]
    public void TheFactory_MakesTheControlOfEachIntrinsicClass_AndNoneForAMenu()
    {
        var made = UI.Run(() => new[]
        {
            VBClasses.CommandButton, VBClasses.TextBox, VBClasses.Label, VBClasses.Frame, VBClasses.CheckBox, VBClasses.OptionButton,
            VBClasses.ListBox, VBClasses.ComboBox, VBClasses.PictureBox, VBClasses.Image, VBClasses.Timer, VBClasses.HScrollBar,
            VBClasses.VScrollBar, VBClasses.Shape, VBClasses.Line, VBClasses.DriveListBox, VBClasses.DirListBox,
            VBClasses.FileListBox, VBClasses.Menu,
        }.Select(type => VBControls.Create(new ComponentInstance(type, "x"))?.GetType().Name ?? "none").ToArray());

        Expect.Lines(new[]
        {
            "VBCommandButton", "VBTextBox", "VBLabel", "VBFrame", "VBCheckBox", "VBOptionButton",
            "VBListBox", "VBComboBox", "VBPictureBox", "VBImage", "VBTimer", "VBScrollBar",
            "VBScrollBar", "VBShape", "VBLine", "VBDriveListBox", "VBDirListBox", "VBFileListBox", "none",
        }, made);
    }

    [TestMethod]
    public void AControlForAnotherClass_IsRefused()
    {
        UI.Run(() => Assert.ThrowsExactly<ArgumentException>(() => new VBTextBox(new ComponentInstance(VBClasses.Label, "Label1"))));
    }

    [TestMethod]
    public void ASystemColour_IsDrawnInTheWindowsPalette_AndAnRgbColourAsItIs()
    {
        Assert.AreEqual(Color.FromRgb(0xF0, 0xF0, 0xF0), FormPalette.ToColor(FormColor.FromSystem(SystemColor.ButtonFace)));
        Assert.AreEqual(Color.FromRgb(0xFF, 0xFF, 0xE1), FormPalette.ToColor(FormColor.FromSystem(SystemColor.InfoBackground)));
        Assert.AreEqual(Color.FromRgb(1, 2, 3), FormPalette.ToColor(FormColor.FromRgb(1, 2, 3)));
    }

    [TestMethod]
    public void AnImageWithNothingDrawn_TakesThePointerOverItsWholeRectangle()
    {
        var raised = UI.Run(() =>
        {
            var (window, sink) = ControlHost.Show(new VBImage(new ComponentInstance(VBClasses.Image, "Image1")));
            window.MouseDown(new Point(70, 20), MouseButton.Left);
            window.MouseUp(new Point(70, 20), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves;
        });

        Expect.Lines(new[]
        {
            "Image1.MouseDown(Integer 1, Integer 0, Single 900, Single 225)",
            "Image1.MouseUp(Integer 1, Integer 0, Single 900, Single 225)",
            "Image1.Click",
        }, raised);
    }

    [TestMethod]
    public void APictureBoxsBorder_IsSunkenWhen3D_AndOnePixelWhenFlat_AndItHoldsControls()
    {
        var (sunken, flat, container) = UI.Run(() =>
        {
            var sunken = new VBPictureBox(new ComponentInstance(VBClasses.PictureBox, "Picture1"));
            var flat = new VBPictureBox(new ComponentInstance(VBClasses.PictureBox, "Picture2").Set(P.Appearance, Appearance.Flat));
            return (sunken.Edge, flat.Edge, sunken is IVBContainer && new VBFrame(new ComponentInstance(VBClasses.Frame, "Frame1")) is IVBContainer);
        });

        Assert.AreEqual(VBEdgeStyle.Sunken, sunken);
        Assert.AreEqual(VBEdgeStyle.Single, flat);
        Assert.IsTrue(container);
    }

    // A ListBox of a height and IntegralHeight, and a font: the component's Height in twips (15 to a pixel) and the height drawn,
    // after each height given from code, then after the font given from code.
    private static string Heights(int twips, bool integral, int[] fromCode, FormFont? font = null)
    {
        return UI.Run(() =>
        {
            var instance = List("List1", "one", "two", "three").Set(P.IntegralHeight, integral);
            var list = new VBListBox(instance);
            var (window, _) = ControlHost.Show((list, new Rect(10, 5, 120, twips / 15d)));
            var heights = new List<string>();
            void Read()
            {
                global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                heights.Add($"{Math.Round(instance.Get(P.Height) * 15)}/{list.Bounds.Height}");
            }
            Read();
            foreach (var asked in fromCode)
            {
                instance.Set(P.Height, asked / 15d);
                Read();
            }
            if (font is { } given)
            {
                instance.Set(P.Font, given);
                Read();
            }
            window.Close();
            return string.Join(", ", heights);
        });
    }

    // As measured with VB6 at the default font: rows of 13 pixels in a border of 4, at least one row; whole pixels first.
    [TestMethod]
    [DataRow(500, "450/30")]
    [DataRow(1000, "840/56")]
    [DataRow(1100, "1035/69")]
    [DataRow(1150, "1035/69")]
    [DataRow(1300, "1230/82")]
    [DataRow(200, "255/17")]
    public void AListBoxsHeight_IsWholeRows_WhereIntegralHeightIsTrue(int twips, string expected)
    {
        Assert.AreEqual(expected, Heights(twips, true, []));
    }

    [TestMethod]
    public void AListBoxsHeightFromCode_IsWholeRowsToo_AtLeastOne_AndKeptWhereItRoundsToThem()
    {
        Assert.AreEqual("840/56, 1234/82, 840/56, 1035/69, 255/17, 255/17, 255/17", Heights(1000, true, [1234, 1000, 1150, 300, 100, 0]));
    }

    [TestMethod]
    [DataRow(500, "500/33")]
    [DataRow(1000, "1000/67")]
    [DataRow(1300, "1300/87")]
    public void WithIntegralHeightFalse_AListBox_TakesTheHeightItIsGiven(int twips, string expected)
    {
        Assert.AreEqual(expected, Heights(twips, false, []));
    }

    // MS Sans Serif 12 is drawn at its 16-pixel size, whose rows VB6 makes 20 pixels high: 69 pixels at 8 points are 64 at 12.
    [TestMethod]
    public void AFontFromCode_SnapsAListBoxToItsRows()
    {
        Assert.AreEqual("1035/69, 960/64", Heights(1100, true, [], new FormFont("MS Sans Serif", 12)));
    }
}
