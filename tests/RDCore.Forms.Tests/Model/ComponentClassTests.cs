using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class ComponentClassTests
{
    private static readonly PropertyDefinition<string> Caption = new("Caption", PropertyCategory.Appearance, "");

    [TestMethod]
    public void TwoPropertiesOfOneName_AreRefused_WhateverTheirCase()
    {
        var other = new PropertyDefinition<int>("CAPTION", PropertyCategory.Misc);

        Assert.ThrowsExactly<ArgumentException>(() =>
            new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [Caption, other], []));
    }

    // The member each VB class's default member stands for, as VB6 reads and assigns it through the instance.
    [TestMethod]
    [DataRow("RDForms.Form", "Controls")]
    [DataRow("RDForms.MDIForm", "Controls")]
    [DataRow("RDForms.CommandButton", "Value")]
    [DataRow("RDForms.CheckBox", "Value")]
    [DataRow("RDForms.OptionButton", "Value")]
    [DataRow("RDForms.HScrollBar", "Value")]
    [DataRow("RDForms.VScrollBar", "Value")]
    [DataRow("RDForms.TextBox", "Text")]
    [DataRow("RDForms.ComboBox", "Text")]
    [DataRow("RDForms.ListBox", "Text")]
    [DataRow("RDForms.Label", "Caption")]
    [DataRow("RDForms.Frame", "Caption")]
    [DataRow("RDForms.PictureBox", "Picture")]
    [DataRow("RDForms.Image", "Picture")]
    [DataRow("RDForms.DriveListBox", "Drive")]
    [DataRow("RDForms.DirListBox", "Path")]
    [DataRow("RDForms.FileListBox", "FileName")]
    [DataRow("RDForms.Shape", "Shape")]
    [DataRow("RDForms.Line", "Visible")]
    [DataRow("RDForms.Timer", "Enabled")]
    [DataRow("RDForms.Data", "Caption")]
    [DataRow("RDForms.Menu", "Enabled")]
    [DataRow("RDForms.UserControl", null)]
    [DataRow("RDForms.PropertyPage", null)]
    [DataRow("RDForms.MultiPage", "Value")]
    [DataRow("RDForms.TabStrip", "Value")]
    [DataRow("RDForms.Page", null)]
    public void AVBClassesDefaultMember_IsTheMemberVB6ReadsAndAssignsThroughTheInstance(string typeName, string? member) =>
        Assert.AreEqual(member, ComponentClasses.ByTypeName(typeName)!.DefaultMember);

    [TestMethod]
    public void EveryClass_ButAUserControlAPropertyPageAPageAndAnImageList_HasADefaultMember() =>
        CollectionAssert.AreEquivalent(
            new[] { ComponentClasses.UserControl, ComponentClasses.PropertyPage, ComponentClasses.Page, ComponentClasses.ImageList },
            ComponentClasses.All.Where(c => c.DefaultMember is null).ToArray());

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void ADefaultMemberWithNoName_IsRefused(string name) =>
        Assert.ThrowsExactly<ArgumentException>(() => new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [], [], defaultMember: name));

    [TestMethod]
    public void AClassGivenNoDefaultMember_HasNone() =>
        Assert.IsNull(new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [], []).DefaultMember);

    [TestMethod]
    public void ADefault_ForAPropertyTheClassHasNot_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [], [],
                new Dictionary<PropertyDefinition, object?> { [Caption] = "x" }));
    }

    [TestMethod]
    public void ADefault_OfTheWrongType_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [Caption], [],
                new Dictionary<PropertyDefinition, object?> { [Caption] = 42 }));
    }

    [TestMethod]
    public void Property_IsFoundWithoutRegardToCase_AndHasNamesTheDefinitionItself()
    {
        Assert.AreSame(Properties.CheckBoxValue, ComponentClasses.CheckBox.Property("value"));
        Assert.IsTrue(ComponentClasses.OptionButton.Has(Properties.OptionButtonValue));
        Assert.IsFalse(ComponentClasses.OptionButton.Has(Properties.CheckBoxValue));
    }

    [TestMethod]
    public void TheCatalog_FindsAClassByItsTypeName_WithoutRegardToCase()
    {
        Assert.AreSame(ComponentClasses.TextBox, ComponentClasses.ByTypeName("rdforms.textbox"));
        Assert.IsNull(ComponentClasses.ByTypeName("MSComctlLib.ListView"));
        Assert.AreEqual("TextBox", ComponentClasses.TextBox.ClassName);
    }

    [TestMethod]
    public void TheCatalog_GivesEachControlItsPlaceAndSize_ExceptTheLineTheTimerAnImageListAndAPage()
    {
        var withoutExtent = ComponentClasses.All.Where(c => c.Kind == ComponentKind.Control && !c.Has(Properties.Height)).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { ComponentClasses.Timer, ComponentClasses.Line, ComponentClasses.Page, ComponentClasses.ImageList }, withoutExtent);
        Assert.IsTrue(ComponentClasses.Timer.Has(Properties.Left));
        Assert.IsTrue(ComponentClasses.ImageList.Has(Properties.Left));
        Assert.IsFalse(ComponentClasses.ImageList.HasExtent);
        Assert.IsFalse(ComponentClasses.Line.Has(Properties.Left));
        Assert.IsFalse(ComponentClasses.Page.Has(Properties.Left));
        Assert.IsFalse(ComponentClasses.Timer.HasExtent);
        Assert.IsFalse(ComponentClasses.Page.HasExtent);
    }

    [TestMethod]
    public void TheCatalog_GivesAComboBoxLocked_AndAListBoxNone()
    {
        // VB6.OLB's ListBox has no Locked property: its file's Locked line is kept as an unmodelled property.
        Assert.IsTrue(ComponentClasses.ComboBox.Has(Properties.Locked));
        Assert.IsNull(ComponentClasses.ListBox.Property("Locked"));
    }

    [TestMethod]
    public void TheCatalog_DefaultsAPictureBoxToABorder_AndALabelToAnOpaqueBackground()
    {
        Assert.AreEqual(BorderStyle.FixedSingle, ComponentClasses.PictureBox.DefaultOf(Properties.BorderStyle));
        Assert.AreEqual(BorderStyle.None, ComponentClasses.Label.DefaultOf(Properties.BorderStyle));
        Assert.AreEqual(BackStyle.Opaque, ComponentClasses.Label.DefaultOf(Properties.BackStyle));
        Assert.AreEqual(BackStyle.Transparent, ComponentClasses.Shape.DefaultOf(Properties.BackStyle));
    }

    // VB6 gives the three buttons a MaskColor of silver and UseMaskColor False, a UserControl a MaskColor of the button face and no
    // UseMaskColor, and an ImageList a MaskColor of silver and UseMaskColor True; the model gives them to no other class.
    [TestMethod]
    public void TheCatalog_GivesTheButtonsAMaskColor_SilverAndNotUsed_AUserControlTheButtonFace_AndAnImageListSilverAndUsed()
    {
        var buttons = new[] { ComponentClasses.CommandButton, ComponentClasses.CheckBox, ComponentClasses.OptionButton };

        CollectionAssert.AreEquivalent(buttons.Append(ComponentClasses.UserControl).Append(ComponentClasses.ImageList).ToArray(),
            ComponentClasses.All.Where(c => c.Has(Properties.MaskColor)).ToArray());
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonFace), ComponentClasses.UserControl.DefaultOf(Properties.MaskColor));
        CollectionAssert.AreEquivalent(
            buttons.Append(ComponentClasses.ImageList).ToArray(), ComponentClasses.All.Where(c => c.Has(Properties.UseMaskColor)).ToArray());
        Assert.IsTrue(buttons.All(c => c.DefaultOf(Properties.MaskColor) == FormColor.FromRgb(0xC0, 0xC0, 0xC0) && !c.DefaultOf(Properties.UseMaskColor)));
        Assert.AreEqual(FormColor.FromRgb(0xC0, 0xC0, 0xC0), ComponentClasses.ImageList.DefaultOf(Properties.MaskColor));
        Assert.IsTrue(ComponentClasses.ImageList.DefaultOf(Properties.UseMaskColor));
    }

    // MSComctlLib's ImageList: a BackColor of the window colour, no pictures and a picture size of 0 by default, and a place a running
    // program does not reach.
    [TestMethod]
    public void TheCatalog_GivesAnImageListTheWindowColour_NoPictures_AndASizeOfZero()
    {
        var imageList = ComponentClasses.ImageList;

        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), imageList.DefaultOf(Properties.BackColor));
        Assert.IsEmpty(imageList.DefaultOf(Properties.ListImages));
        Assert.AreEqual((short)0, imageList.DefaultOf(Properties.ImageWidth));
        Assert.AreEqual((short)0, imageList.DefaultOf(Properties.ImageHeight));
        Assert.IsTrue(imageList.IsNotAvailableAtRunTime(Properties.Left) && imageList.IsNotAvailableAtRunTime(Properties.Top));
    }

    [TestMethod]
    [DataRow("RDForms.HScrollBar")]
    [DataRow("RDForms.VScrollBar")]
    public void TheCatalog_GivesAScrollBarItsKeyEvents(string typeName)
    {
        var bar = ComponentClasses.ByTypeName(typeName)!;

        CollectionAssert.AreEquivalent(new[] { "Change", "GotFocus", "KeyDown", "KeyPress", "KeyUp", "LostFocus", "Scroll", "Validate", "BeforeUpdate",
            "AfterUpdate", "BeforeDragOver", "BeforeDropOrPaste", "Error" },
            bar.Events.Select(e => e.Name).ToArray());
        Assert.AreSame(Events.KeyPress, bar.Event("keypress"));
    }
}
