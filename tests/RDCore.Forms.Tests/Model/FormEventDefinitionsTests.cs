using RDCore.Forms.Model;
using RDCore.Forms.Porting;

namespace RDCore.Forms.Tests.Model;

// The RDForms events of the classes: Validate where a control takes the focus, BeforeUpdate and AfterUpdate where it has a value, and
// the drag and drop events and Error where MSForms gives them, with the parameters an MSForms handler ports to.
[TestClass]
public sealed class FormEventDefinitionsTests
{
    private static readonly ComponentClass[] Roots =
        [ComponentClasses.Form, ComponentClasses.MDIForm, ComponentClasses.UserControl, ComponentClasses.PropertyPage];

    private static readonly ComponentClass[] WithAValue =
        [ComponentClasses.TextBox, ComponentClasses.ComboBox, ComponentClasses.ListBox, ComponentClasses.CheckBox, ComponentClasses.OptionButton,
         ComponentClasses.HScrollBar, ComponentClasses.VScrollBar];

    private static readonly ComponentClass[] Controls =
        [ComponentClasses.CommandButton, ComponentClasses.Label, ComponentClasses.Image, .. WithAValue];

    private static string Signature(EventDefinition definition) =>
        string.Join(", ", definition.Parameters.Select(p => $"{p.Name} As {p.TypeName}"));

    private static string Signature(EventPort port) =>
        string.Join(", ", port.Parameters.Select(p => $"{p.Name} As {p.RDFormsType}"));

    [TestMethod]
    public void EveryControlThatTakesTheFocus_HasValidate_AndCausesValidation_ThatDefaultsToTrue()
    {
        var focusable = ComponentClasses.All.Where(c => !Roots.Contains(c) && c.Events.Contains(Events.GotFocus)).ToList();

        Assert.IsNotEmpty(focusable);
        foreach (var type in focusable)
        {
            Assert.IsTrue(type.Events.Contains(Events.Validate), type.TypeName);
            Assert.IsTrue(type.Has(Properties.CausesValidation), type.TypeName);
            Assert.IsTrue(new ComponentInstance(type, "x").Get(Properties.CausesValidation), type.TypeName);
        }
        Assert.AreEqual("Cancel As Boolean", Signature(Events.Validate));
    }

    [TestMethod]
    public void ARootOrAControlThatTakesNoFocus_HasNoValidate()
    {
        foreach (var type in ComponentClasses.All.Where(c => Roots.Contains(c) || !c.Events.Contains(Events.GotFocus)))
            Assert.IsFalse(type.Events.Contains(Events.Validate), type.TypeName);
    }

    [TestMethod]
    public void EveryControlWithAValue_HasBeforeUpdateAndAfterUpdate()
    {
        foreach (var type in WithAValue)
        {
            Assert.IsTrue(type.Events.Contains(Events.BeforeUpdate), type.TypeName);
            Assert.IsTrue(type.Events.Contains(Events.AfterUpdate), type.TypeName);
        }
        Assert.IsFalse(ComponentClasses.CommandButton.Events.Contains(Events.BeforeUpdate));
        Assert.AreEqual("Cancel As Boolean", Signature(Events.BeforeUpdate));
        Assert.AreEqual("", Signature(Events.AfterUpdate));
    }

    [TestMethod]
    public void TheDragAndDropEventsAndError_AreOnTheControlsAndContainersMSFormsGivesThem()
    {
        foreach (var type in Controls)
            CollectionAssert.IsSubsetOf(new[] { Events.BeforeDragOver, Events.BeforeDropOrPaste, Events.Error }, type.Events.ToArray(), type.TypeName);
        foreach (var type in new[] { ComponentClasses.Form, ComponentClasses.Frame })
            CollectionAssert.IsSubsetOf(new[] { Events.ContainerBeforeDragOver, Events.ContainerBeforeDropOrPaste, Events.Error }, type.Events.ToArray(),
                type.TypeName);
    }

    [TestMethod]
    public void AMultiPageAndATabStrip_HaveTheUpdateEvents_AndAPage_WhoseEventsAreItsMultiPages_HasNone()
    {
        foreach (var type in new[] { ComponentClasses.MultiPage, ComponentClasses.TabStrip })
            CollectionAssert.IsSubsetOf(new[] { Events.Validate, Events.BeforeUpdate, Events.AfterUpdate }, type.Events.ToArray(), type.TypeName);
        Assert.IsEmpty(ComponentClasses.Page.Events);
        Assert.IsFalse(ComponentClasses.Page.Has(Properties.CausesValidation));
        Assert.AreSame(Events.Error, ComponentClasses.TabStrip.Event("Error"));
        Assert.AreSame(Events.PageError, ComponentClasses.MultiPage.Event("Error"));
    }

    [TestMethod]
    public void TheDataControl_KeepsItsOwnValidateAndError()
    {
        Assert.AreEqual("Action As Integer, Save As Integer", Signature(ComponentClasses.Data.Event("Validate")!));
        Assert.AreEqual("DataErr As Integer, Response As Integer", Signature(ComponentClasses.Data.Event("Error")!));
    }

    // An MSForms handler ports to the RDForms event of the same parameters: the port map and the model agree.
    [TestMethod]
    [DataRow("TextBox", "Exit", "RDForms.TextBox", "Validate")]
    [DataRow("TextBox", "BeforeUpdate", "RDForms.TextBox", "BeforeUpdate")]
    [DataRow("CheckBox", "BeforeUpdate", "RDForms.CheckBox", "BeforeUpdate")]
    [DataRow("TextBox", "BeforeDragOver", "RDForms.TextBox", "BeforeDragOver")]
    [DataRow("CommandButton", "BeforeDropOrPaste", "RDForms.CommandButton", "BeforeDropOrPaste")]
    [DataRow("Label", "Error", "RDForms.Label", "Error")]
    [DataRow("UserForm", "BeforeDragOver", "RDForms.Form", "BeforeDragOver")]
    [DataRow("UserForm", "BeforeDropOrPaste", "RDForms.Form", "BeforeDropOrPaste")]
    [DataRow("Frame", "BeforeDragOver", "RDForms.Frame", "BeforeDragOver")]
    [DataRow("Frame", "Error", "RDForms.Frame", "Error")]
    [DataRow("MultiPage", "Exit", "RDForms.MultiPage", "Validate")]
    [DataRow("MultiPage", "BeforeUpdate", "RDForms.MultiPage", "BeforeUpdate")]
    [DataRow("MultiPage", "BeforeDragOver", "RDForms.MultiPage", "BeforeDragOver")]
    [DataRow("MultiPage", "BeforeDropOrPaste", "RDForms.MultiPage", "BeforeDropOrPaste")]
    [DataRow("MultiPage", "Error", "RDForms.MultiPage", "Error")]
    [DataRow("MultiPage", "AddControl", "RDForms.MultiPage", "AddControl")]
    [DataRow("MultiPage", "RemoveControl", "RDForms.MultiPage", "RemoveControl")]
    [DataRow("MultiPage", "DblClick", "RDForms.MultiPage", "DblClick")]
    [DataRow("MultiPage", "MouseDown", "RDForms.MultiPage", "MouseDown")]
    [DataRow("TabStrip", "Exit", "RDForms.TabStrip", "Validate")]
    [DataRow("TabStrip", "BeforeUpdate", "RDForms.TabStrip", "BeforeUpdate")]
    [DataRow("TabStrip", "BeforeDragOver", "RDForms.TabStrip", "BeforeDragOver")]
    [DataRow("TabStrip", "BeforeDropOrPaste", "RDForms.TabStrip", "BeforeDropOrPaste")]
    [DataRow("TabStrip", "Error", "RDForms.TabStrip", "Error")]
    [DataRow("TabStrip", "MouseUp", "RDForms.TabStrip", "MouseUp")]
    public void AnMSFormsHandler_PortsToTheEventOfItsClass_WithTheSameParameters(string legacyClass, string legacyEvent, string rdformsClass,
        string rdformsEvent)
    {
        var port = FormsPortMap.MSForms.EventOf(legacyClass, legacyEvent)!;
        var definition = ComponentClasses.ByTypeName(rdformsClass)!.Event(rdformsEvent)!;

        Assert.AreEqual(rdformsEvent, port.RDFormsName);
        Assert.AreEqual(Signature(port), Signature(definition));
    }

    [TestMethod]
    public void TheDragAndDropEvents_NameTheirRDFormsTypes()
    {
        Assert.AreEqual("Cancel As Boolean, Data As RDForms.DataObject, X As Single, Y As Single, DragState As RDForms.DragState, "
                        + "Effect As RDForms.ReturnEffect, Shift As Integer", Signature(Events.BeforeDragOver));
        Assert.AreEqual("Cancel As Boolean, Control As RDForms.Control, Action As RDForms.Action, Data As RDForms.DataObject, X As Single, "
                        + "Y As Single, Effect As RDForms.ReturnEffect, Shift As Integer", Signature(Events.ContainerBeforeDropOrPaste));
    }
}
