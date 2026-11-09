using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

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
        Assert.AreSame(VBProperties.CheckBoxValue, VBClasses.CheckBox.Property("value"));
        Assert.IsTrue(VBClasses.OptionButton.Has(VBProperties.OptionButtonValue));
        Assert.IsFalse(VBClasses.OptionButton.Has(VBProperties.CheckBoxValue));
    }

    [TestMethod]
    public void TheCatalog_FindsAClassByItsTypeName_WithoutRegardToCase()
    {
        Assert.AreSame(VBClasses.TextBox, VBClasses.ByTypeName("vb.textbox"));
        Assert.IsNull(VBClasses.ByTypeName("MSComctlLib.ListView"));
        Assert.AreEqual("TextBox", VBClasses.TextBox.ClassName);
    }

    [TestMethod]
    public void TheCatalog_GivesEachControlItsPlaceAndSize_ExceptTheLineAndTheTimer()
    {
        var withoutExtent = VBClasses.All.Where(c => c.Kind == ComponentKind.Control && !c.Has(VBProperties.Height)).ToArray();

        CollectionAssert.AreEquivalent(new[] { VBClasses.Timer, VBClasses.Line }, withoutExtent);
        Assert.IsTrue(VBClasses.Timer.Has(VBProperties.Left));
        Assert.IsFalse(VBClasses.Line.Has(VBProperties.Left));
        Assert.IsFalse(VBClasses.Timer.HasExtent);
    }

    [TestMethod]
    public void TheCatalog_GivesAComboBoxLocked_AndAListBoxNone()
    {
        // VB6.OLB's ListBox has no Locked property: its file's Locked line is kept as an unmodelled property.
        Assert.IsTrue(VBClasses.ComboBox.Has(VBProperties.Locked));
        Assert.IsNull(VBClasses.ListBox.Property("Locked"));
    }

    [TestMethod]
    public void TheCatalog_DefaultsAPictureBoxToABorder_AndALabelToAnOpaqueBackground()
    {
        Assert.AreEqual(BorderStyle.FixedSingle, VBClasses.PictureBox.DefaultOf(VBProperties.BorderStyle));
        Assert.AreEqual(BorderStyle.None, VBClasses.Label.DefaultOf(VBProperties.BorderStyle));
        Assert.AreEqual(BackStyle.Opaque, VBClasses.Label.DefaultOf(VBProperties.BackStyle));
        Assert.AreEqual(BackStyle.Transparent, VBClasses.Shape.DefaultOf(VBProperties.BackStyle));
    }
}
