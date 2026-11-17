using RDCore.Forms.Model;
using RDCore.Forms.Xaml;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class ToggleAndSpinClassTests
{
    private const string Ns = FormXaml.Namespace;

    private static ComponentInstance ReadBack(ComponentInstance component)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        component.SetContainer(root);
        var form = new FormDefinition(root);
        var read = FormXaml.Read(FormXaml.Write(form));
        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        return read.Form!.Components.Single(c => c.Name == component.Name);
    }

    [TestMethod]
    [DataRow("RDForms.ToggleButton", "ToggleButton")]
    [DataRow("RDForms.SpinButton", "SpinButton")]
    public void EachClass_IsInTheCatalog_WithValueAsItsDefaultMember(string typeName, string className)
    {
        var type = ComponentClasses.ByTypeName(typeName);

        Assert.IsNotNull(type);
        Assert.AreEqual(className, type.ClassName);
        Assert.AreEqual(ComponentKind.Control, type.Kind);
        Assert.AreEqual("Value", type.DefaultMember);
    }

    [TestMethod]
    public void AToggleButton_HasItsMembers_AndTheirDefaults()
    {
        var button = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1");

        Assert.IsFalse(button.Get(P.ToggleButtonValue));
        Assert.IsFalse(button.Get(P.TripleState));
        Assert.IsFalse(button.Get(P.Locked));
        Assert.IsTrue(button.Get(P.WordWrap));
        Assert.AreEqual(BackStyle.Opaque, button.Get(P.BackStyle));
        Assert.AreEqual(PicturePosition.AboveCenter, button.Get(P.PicturePosition));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonFace), button.Get(P.BackColor));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonText), button.Get(P.ForeColor));
        CollectionAssert.AreEquivalent(
            new[] { "Click", "DblClick", "Change", "GotFocus", "LostFocus", "KeyDown", "KeyPress", "KeyUp", "MouseDown", "MouseMove", "MouseUp",
                "Validate", "BeforeUpdate", "AfterUpdate", "BeforeDragOver", "BeforeDropOrPaste", "Error" },
            ComponentClasses.ToggleButton.Events.Select(e => e.Name).ToArray());
    }

    [TestMethod]
    public void AToggleButtonsValue_CanBeNull()
    {
        var button = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1");

        button.Set(P.ToggleButtonValue, null);

        Assert.IsNull(button.Get(P.ToggleButtonValue));
        Assert.IsTrue(P.ToggleButtonValue.Accepts(null));
    }

    [TestMethod]
    public void ASpinButton_HasItsMembers_AndTheirDefaults()
    {
        var spin = new ComponentInstance(ComponentClasses.SpinButton, "SpinButton1");

        Assert.AreEqual((0, 0, 100, 1), (spin.Get(P.SpinButtonValue), spin.Get(P.SpinButtonMin), spin.Get(P.SpinButtonMax), spin.Get(P.SpinButtonSmallChange)));
        Assert.AreEqual(ControlOrientation.Auto, spin.Get(P.Orientation));
        Assert.AreEqual(50, spin.Get(P.Delay));
        Assert.IsFalse(P.Delay.Accepts(-1));
        CollectionAssert.AreEquivalent(
            new[] { "Change", "SpinUp", "SpinDown", "GotFocus", "LostFocus", "KeyDown", "KeyPress", "KeyUp", "Validate", "BeforeUpdate",
                "AfterUpdate", "BeforeDragOver", "BeforeDropOrPaste", "Error" },
            ComponentClasses.SpinButton.Events.Select(e => e.Name).ToArray());
    }

    [TestMethod]
    public void ASpinButtonsValue_HoldsALong()
    {
        var spin = new ComponentInstance(ComponentClasses.SpinButton, "SpinButton1").Set(P.SpinButtonMax, 1_000_000).Set(P.SpinButtonValue, 70_000);

        Assert.AreEqual(70_000, spin.Get(P.SpinButtonValue));
        Assert.AreEqual(typeof(int), P.SpinButtonValue.ValueType);
    }

    [TestMethod]
    [DataRow(true, DisplayName = "True")]
    [DataRow(false, DisplayName = "False")]
    [DataRow(null, DisplayName = "Null")]
    public void AToggleButton_IsWrittenAndReadBack(bool? value)
    {
        var button = new ComponentInstance(ComponentClasses.ToggleButton, "ToggleButton1")
            .Set(P.Caption, "&Bold").Set(P.TripleState, true).Set(P.ToggleButtonValue, value);

        var read = ReadBack(button);

        Assert.AreEqual(value, read.Get(P.ToggleButtonValue));
        Assert.IsTrue(read.Get(P.TripleState));
        Assert.AreEqual("&Bold", read.Get(P.Caption));
    }

    [TestMethod]
    public void ASpinButton_IsWrittenAndReadBack()
    {
        var spin = new ComponentInstance(ComponentClasses.SpinButton, "SpinButton1")
            .Set(P.SpinButtonMin, -5).Set(P.SpinButtonMax, 500_000).Set(P.SpinButtonValue, 70_000).Set(P.SpinButtonSmallChange, 25)
            .Set(P.Orientation, ControlOrientation.Horizontal).Set(P.Delay, 120);

        var read = ReadBack(spin);

        Assert.AreEqual((-5, 500_000, 70_000, 25), (read.Get(P.SpinButtonMin), read.Get(P.SpinButtonMax), read.Get(P.SpinButtonValue), read.Get(P.SpinButtonSmallChange)));
        Assert.AreEqual(ControlOrientation.Horizontal, read.Get(P.Orientation));
        Assert.AreEqual(120, read.Get(P.Delay));
    }

    [TestMethod]
    public void ADocumentsNullToggleButton_IsRead()
    {
        var read = FormXaml.Read($"<Form xmlns=\"{Ns}\" FormatVersion=\"1\" Name=\"Form1\"><ToggleButton Name=\"ToggleButton1\" Value=\"Null\" /></Form>");

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        var button = read.Form!.Components.Single(c => c.Name == "ToggleButton1");
        Assert.IsTrue(button.IsSet(P.ToggleButtonValue));
        Assert.IsNull(button.Get(P.ToggleButtonValue));
    }
}
