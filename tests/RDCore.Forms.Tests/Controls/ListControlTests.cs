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
}
