using System.Globalization;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;

namespace RDCore.Forms.Tests.Porting;

[TestClass]
public sealed class FormsPortMapTests
{
    private static readonly FormsPortMap VB6 = FormsPortMap.VB6;
    private static readonly FormsPortMap MSForms = FormsPortMap.MSForms;

    private static readonly string[] MSFormsControls =
        ["CommandButton", "Label", "TextBox", "ComboBox", "ListBox", "CheckBox", "OptionButton", "Frame", "Image", "ToggleButton", "SpinButton",
         "MultiPage", "Page", "TabStrip"];

    [TestMethod]
    public void EachMap_NamesItsLibrary_AndItsUnit()
    {
        Assert.AreEqual("VB", VB6.Library);
        Assert.AreEqual(LegacyUnit.Twips, VB6.LegacyUnit);
        Assert.AreEqual("MSForms", MSForms.Library);
        Assert.AreEqual(LegacyUnit.Points, MSForms.LegacyUnit);
    }

    [TestMethod]
    public void TheMSFormsMap_NamesTheUserFormWindowClass_AndTheVB6MapNone()
    {
        Assert.AreEqual("ThunderDFrame", MSForms.FormWindowClass);
        Assert.IsNull(VB6.FormWindowClass);
    }

    // Types

    // The classes only MSForms has: a VB6 form has none of them.
    private static readonly ComponentClass[] MSFormsOnly =
        [ComponentClasses.ToggleButton, ComponentClasses.SpinButton, ComponentClasses.MultiPage, ComponentClasses.Page, ComponentClasses.TabStrip];

    [TestMethod]
    public void EveryClassOfTheModel_IsTheRDFormsClassItsVBClassPortsTo_ButThoseOnlyMSFormsHas()
    {
        foreach (var rdFormsClass in ComponentClasses.All.Except(MSFormsOnly))
        {
            var name = rdFormsClass.TypeName["RDForms.".Length..];
            var port = VB6.TypeOf("VB." + name);
            Assert.IsNotNull(port, rdFormsClass.TypeName);
            Assert.AreEqual(rdFormsClass.TypeName, port.RDFormsName);
            Assert.AreEqual(TypePortKind.Class, port.Kind);
            Assert.AreEqual("VB", port.LegacyLibrary);
            Assert.AreEqual(name, port.LegacyName);
            Assert.AreSame(port, VB6.TypeOf(name));
        }
    }

    [TestMethod]
    public void TheClassesOnlyMSFormsHas_AreTheRDFormsClassesItsClassesPortTo_AndVBHasNone()
    {
        foreach (var rdFormsClass in MSFormsOnly)
        {
            var name = rdFormsClass.TypeName["RDForms.".Length..];
            Assert.IsNull(VB6.TypeOf("VB." + name), rdFormsClass.TypeName);
            var port = MSForms.TypeOf("MSForms." + name);
            Assert.IsNotNull(port, rdFormsClass.TypeName);
            Assert.AreEqual(rdFormsClass.TypeName, port.RDFormsName);
            Assert.AreEqual(TypePortKind.Class, port.Kind);
            Assert.AreSame(port, MSForms.TypeOf(name));
        }
    }

    [TestMethod]
    public void VBsControlAndDataObject_PortToRDForms()
    {
        Assert.AreEqual("RDForms.Control", VB6.TypeOf("VB.Control")?.RDFormsName);
        Assert.AreEqual("RDForms.Control", VB6.TypeOf("Control")?.RDFormsName);
        Assert.AreEqual("RDForms.Form", VB6.TypeOf("Form")?.RDFormsName);
        Assert.AreEqual("RDForms.DataObject", VB6.TypeOf("DataObject")?.RDFormsName);
    }

    [TestMethod]
    public void AUserForm_PortsToAnRDFormsForm()
    {
        Assert.AreEqual("RDForms.Form", MSForms.TypeOf("UserForm")?.RDFormsName);
        Assert.AreEqual("RDForms.Form", MSForms.TypeOf("MSForms.UserForm")?.RDFormsName);
        Assert.AreEqual(TypePortKind.Class, MSForms.TypeOf("UserForm")?.Kind);
    }

    [TestMethod]
    public void EveryMSFormsControlClass_PortsToTheRDFormsClassOfItsName()
    {
        foreach (var name in MSFormsControls.Append("Control").Append("Tab").Append("Controls").Append("Pages").Append("Tabs"))
        {
            var port = MSForms.TypeOf("MSForms." + name);
            Assert.IsNotNull(port, name);
            Assert.AreEqual("RDForms." + name, port.RDFormsName);
            Assert.AreEqual(TypePortKind.Class, port.Kind);
            Assert.AreEqual("MSForms", port.LegacyLibrary);
            Assert.IsNull(port.ByOrientation);
            Assert.IsNull(port.Diagnostic);
        }
    }

    [TestMethod]
    public void MSFormsHelperClasses_PortToTheirRDFormsClasses()
    {
        Assert.AreEqual("RDForms.DataObject", MSForms.TypeOf("MSForms.DataObject")?.RDFormsName);
        Assert.AreEqual("RDForms.ReturnEffect", MSForms.TypeOf("MSForms.ReturnEffect")?.RDFormsName);
        Assert.AreEqual("RDForms.Font", MSForms.TypeOf("MSForms.NewFont")?.RDFormsName);
        Assert.AreEqual("RDForms.Font", MSForms.TypeOf("MSForms.Font")?.RDFormsName);
    }

    [TestMethod]
    [DataRow("ReturnInteger", "Integer")]
    [DataRow("ReturnBoolean", "Boolean")]
    [DataRow("ReturnString", "String")]
    [DataRow("ReturnSingle", "Single")]
    public void AReturnWrapper_PortsToItsValuesType_AsAParameter(string wrapper, string type)
    {
        var port = MSForms.TypeOf("MSForms." + wrapper);
        Assert.IsNotNull(port);
        Assert.AreEqual(type, port.RDFormsName);
        Assert.AreEqual(TypePortKind.Parameter, port.Kind);
    }

    [TestMethod]
    public void EveryFmEnum_PortsToTheRDFormsEnumOfItsNameWithoutFm()
    {
        var enums = MSForms.Types.Where(t => t.Kind == TypePortKind.Enum).ToList();
        Assert.HasCount(42, enums);
        foreach (var port in enums)
        {
            StringAssert.StartsWith(port.LegacyName, "fm");
            Assert.AreEqual("RDForms." + port.LegacyName[2..], port.RDFormsName);
        }

        Assert.AreEqual("RDForms.TextAlign", MSForms.TypeOf("MSForms.fmTextAlign")?.RDFormsName);
        Assert.AreEqual("RDForms.DragState", MSForms.TypeOf("fmDragState")?.RDFormsName);
    }

    [TestMethod]
    public void AScrollBar_PortsByItsOrientation_AndAReferenceToOneIsAControl()
    {
        var port = MSForms.TypeOf("MSForms.ScrollBar");
        Assert.IsNotNull(port);
        Assert.AreEqual("RDForms.Control", port.RDFormsName);
        Assert.IsNotNull(port.ByOrientation);
        Assert.AreEqual("RDForms.HScrollBar", port.ByOrientation.Horizontal);
        Assert.AreEqual("RDForms.VScrollBar", port.ByOrientation.Vertical);
        Assert.AreEqual(PortDiagnosticKind.ScrollBarReference, port.Diagnostic?.Kind);
    }

    // TypeName(x) = "ScrollBar" is False once x is an RDForms.HScrollBar: the port reports the comparison, naming the class
    [TestMethod]
    public void ATypeComparisonOnAPortedScrollBar_IsAWarningNamingItsClass()
    {
        var diagnostic = MSForms.TypeOf("ScrollBar")!.ByOrientation!.TypeNameChanged("RDForms.HScrollBar");

        Assert.AreEqual(("RDC40012", PortDiagnosticSeverity.Warning), (diagnostic.Code, diagnostic.Severity));
        StringAssert.Contains(diagnostic.MessageIn(CultureInfo.GetCultureInfo("en-CA")), "RDForms.HScrollBar");
    }

    [TestMethod]
    public void AnOrientation_PicksTheScrollBarClass_TheAutomaticOneByTheLongerSide()
    {
        var orientation = MSForms.TypeOf("ScrollBar")!.ByOrientation!;
        Assert.AreEqual("RDForms.HScrollBar", orientation.RDFormsNameOf(OrientationPort.HorizontalOrientation, 10, 100));
        Assert.AreEqual("RDForms.VScrollBar", orientation.RDFormsNameOf(OrientationPort.VerticalOrientation, 100, 10));
        Assert.AreEqual("RDForms.HScrollBar", orientation.RDFormsNameOf(OrientationPort.Auto, 100, 12.75));
        Assert.AreEqual("RDForms.VScrollBar", orientation.RDFormsNameOf(OrientationPort.Auto, 12.75, 63.8));
        Assert.AreEqual("RDForms.VScrollBar", orientation.RDFormsNameOf(OrientationPort.Auto, 20, 20));
        Assert.IsNull(orientation.RDFormsNameOf(2, 100, 10));
    }

    [TestMethod]
    public void ANameNeitherMapHas_PortsUnchanged()
    {
        Assert.IsNull(VB6.TypeOf("Collection"));
        Assert.IsNull(MSForms.TypeOf("MSForms.HTMLSubmit2"));
        Assert.IsNull(VB6.TypeOf("MSForms.TextBox"));
    }

    // Members

    [TestMethod]
    [DataRow("Left")]
    [DataRow("Top")]
    [DataRow("Width")]
    [DataRow("Height")]
    [DataRow("Move")]
    public void AVBControlsPlace_IsInItsContainersScale_AndAFormsInTwips(string member)
    {
        var control = VB6.MemberOf("VB.TextBox", member);
        Assert.IsNotNull(control);
        Assert.AreEqual(PortUnit.ContainerLength, control.Unit);
        Assert.AreEqual(FormsPortMap.ExtenderClass, control.LegacyClass);
        Assert.AreEqual(member, control.RDFormsName);
        Assert.IsFalse(control.IsRenamed);

        Assert.AreEqual(PortUnit.Length, VB6.MemberOf("Form", member)?.Unit);
        Assert.AreEqual(PortUnit.Length, VB6.MemberOf("MDIForm", member)?.Unit);
    }

    [TestMethod]
    [DataRow("X1")]
    [DataRow("Y1")]
    [DataRow("X2")]
    [DataRow("Y2")]
    public void AVBLinesEnds_AreInItsContainersScale(string member) =>
        Assert.AreEqual(PortUnit.ContainerLength, VB6.MemberOf("Line", member)?.Unit);

    [TestMethod]
    public void VBsAppearance_IsSpecialEffect()
    {
        var port = VB6.MemberOf("CommandButton", "Appearance");
        Assert.IsNotNull(port);
        Assert.AreEqual("SpecialEffect", port.RDFormsName);
        Assert.IsTrue(port.IsRenamed);
        Assert.IsNotNull(port.Value);
        Assert.AreEqual(PortUnit.None, port.Unit);
    }

    [TestMethod]
    public void AVBImagesStretch_IsPictureSizeMode()
    {
        var port = VB6.MemberOf("Image", "Stretch");
        Assert.AreEqual("PictureSizeMode", port?.RDFormsName);
        Assert.IsNotNull(port?.Value);
    }

    [TestMethod]
    public void AVBCheckBoxsValue_ConvertsItsValues_AndAnOptionButtonsPortsUnchanged()
    {
        var port = VB6.MemberOf("CheckBox", "Value");
        Assert.AreEqual("Value", port?.RDFormsName);
        Assert.IsTrue(port?.Value?.GrayedComparesWithIsNull);
        Assert.IsNull(VB6.MemberOf("OptionButton", "Value"));
    }

    [TestMethod]
    [DataRow("Left")]
    [DataRow("Top")]
    [DataRow("Width")]
    [DataRow("Height")]
    [DataRow("Move")]
    public void AnMSFormsPlace_IsALengthInPoints(string member)
    {
        Assert.AreEqual(PortUnit.Length, MSForms.MemberOf("MSForms.TextBox", member)?.Unit);
        Assert.AreEqual(PortUnit.Length, MSForms.MemberOf("UserForm", member)?.Unit);
    }

    [TestMethod]
    [DataRow("Controls", "Move")]
    [DataRow("UserForm", "ScrollLeft")]
    [DataRow("Frame", "ScrollTop")]
    [DataRow("Page", "ScrollWidth")]
    [DataRow("UserForm", "ScrollHeight")]
    [DataRow("TabStrip", "TabFixedWidth")]
    [DataRow("MultiPage", "TabFixedHeight")]
    [DataRow("TabStrip", "ClientLeft")]
    [DataRow("TabStrip", "ClientTop")]
    [DataRow("TabStrip", "ClientWidth")]
    [DataRow("TabStrip", "ClientHeight")]
    [DataRow("ListBox", "ListWidth")]
    [DataRow("ComboBox", "ListWidth")]
    public void AnMSFormsLength_KeepsItsName_InDIP(string legacyClass, string member)
    {
        var port = MSForms.MemberOf(legacyClass, member);
        Assert.IsNotNull(port);
        Assert.AreEqual(PortUnit.Length, port.Unit);
        Assert.AreEqual(member, port.RDFormsName);
    }

    [TestMethod]
    [DataRow("TextBox")]
    [DataRow("Page")]
    [DataRow("Tab")]
    [DataRow("UserForm")]
    public void ControlTipText_IsToolTipText(string legacyClass)
    {
        var port = MSForms.MemberOf(legacyClass, "ControlTipText");
        Assert.AreEqual("ToolTipText", port?.RDFormsName);
        Assert.IsTrue(port?.IsRenamed);
        Assert.IsNull(VB6.MemberOf(legacyClass, "ToolTipText"));
    }

    [TestMethod]
    [DataRow("UserForm")]
    [DataRow("Frame")]
    [DataRow("Page")]
    public void TheInsideOfAContainer_IsItsScale_InDIP(string legacyClass)
    {
        var width = MSForms.MemberOf(legacyClass, "InsideWidth");
        var height = MSForms.MemberOf(legacyClass, "InsideHeight");
        Assert.AreEqual("ScaleWidth", width?.RDFormsName);
        Assert.AreEqual("ScaleHeight", height?.RDFormsName);
        Assert.AreEqual(PortUnit.Length, width?.Unit);
        Assert.AreEqual(PortUnit.Length, height?.Unit);
    }

    [TestMethod]
    [DataRow("Label")]
    [DataRow("TextBox")]
    [DataRow("ListBox")]
    [DataRow("ComboBox")]
    public void TextAlign_IsAlignment_WithVBsValues(string legacyClass)
    {
        var port = MSForms.MemberOf(legacyClass, "TextAlign");
        Assert.AreEqual("Alignment", port?.RDFormsName);
        Assert.AreEqual(0, port?.Value?.Convert(1).Value);
    }

    [TestMethod]
    [DataRow("CheckBox")]
    [DataRow("OptionButton")]
    [DataRow("ToggleButton")]
    public void AnMSFormsCaptionsSide_IsAlignment_Inverted(string legacyClass)
    {
        var port = MSForms.MemberOf(legacyClass, "Alignment");
        Assert.AreEqual("Alignment", port?.RDFormsName);
        Assert.IsFalse(port?.IsRenamed);
        Assert.AreEqual(1, port?.Value?.Convert(0).Value);
        Assert.AreEqual(0, port?.Value?.Convert(1).Value);
    }

    [TestMethod]
    [DataRow("Label")]
    [DataRow("CommandButton")]
    [DataRow("CheckBox")]
    [DataRow("OptionButton")]
    [DataRow("ToggleButton")]
    [DataRow("Page")]
    [DataRow("Tab")]
    public void AnAcceleratorAssignment_IsReported(string legacyClass)
    {
        var port = MSForms.MemberOf(legacyClass, "Accelerator");
        Assert.IsNotNull(port);
        Assert.AreEqual("Caption", port.RDFormsName);
        Assert.AreEqual(PortDiagnosticKind.AcceleratorAssignment, port.Diagnostic?.Kind);
    }

    [TestMethod]
    public void AnMSFormsTextBoxsAndAComboBoxsValue_IsItsText_AndAListBoxsIsKept()
    {
        Assert.AreEqual("Text", MSForms.MemberOf("MSForms.TextBox", "Value")?.RDFormsName);
        Assert.AreEqual("Text", MSForms.MemberOf("MSForms.ComboBox", "Value")?.RDFormsName);
        Assert.AreEqual("Text", MSForms.MemberOf("ComboBox", "value")?.RDFormsName);
        Assert.IsNull(MSForms.MemberOf("ListBox", "Value"));
        Assert.IsNull(MSForms.MemberOf("CheckBox", "Value"));
    }

    [TestMethod]
    [DataRow("Caption")]
    [DataRow("BackColor")]
    [DataRow("Enabled")]
    [DataRow("List")]
    [DataRow("ListIndex")]
    [DataRow("MousePointer")]
    [DataRow("Style")]
    [DataRow("BorderStyle")]
    [DataRow("SpecialEffect")]
    [DataRow("PictureSizeMode")]
    public void AMemberOfTheSameNameAndMeaning_PortsUnchanged(string member)
    {
        Assert.IsNull(MSForms.MemberOf("ComboBox", member));
        Assert.IsNull(VB6.MemberOf("ComboBox", member));
    }

    // Events

    [TestMethod]
    public void AVBKeyPressHandler_PortsUnchanged() => Assert.IsNull(VB6.EventOf("TextBox", "KeyPress"));

    [TestMethod]
    [DataRow("MouseDown")]
    [DataRow("MouseMove")]
    [DataRow("MouseUp")]
    public void AVBMouseHandler_ConvertsItsCoordinates(string name)
    {
        var port = VB6.EventOf("VB.PictureBox", name);
        Assert.IsNotNull(port);
        Assert.AreEqual(name, port.RDFormsName);
        Assert.IsTrue(port.CoordinatesInLegacyUnit);
        CollectionAssert.AreEqual(new[] { "Button", "Shift", "X", "Y" }, port.Parameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(port.Parameters.All(p => p.ByRef && !p.UnwrapsValue && p.LegacyType == p.RDFormsType));
    }

    [TestMethod]
    public void AVBDragHandler_ConvertsItsCoordinates_AndTypesItsSourceAnRDFormsControl()
    {
        var dragDrop = VB6.EventOf("Form", "DragDrop");
        Assert.IsTrue(dragDrop?.CoordinatesInLegacyUnit);
        Assert.AreEqual("RDForms.Control", dragDrop?.Parameters[0].RDFormsType);
        Assert.AreEqual("Control", dragDrop?.Parameters[0].LegacyType);
        Assert.IsTrue(VB6.EventOf("Form", "DragOver")?.CoordinatesInLegacyUnit);
        Assert.AreEqual("RDForms.DataObject", VB6.EventOf("TextBox", "OLEDragDrop")?.Parameters[0].RDFormsType);
        Assert.IsTrue(VB6.EventOf("TextBox", "OLEDragOver")?.CoordinatesInLegacyUnit);
        Assert.IsTrue(VB6.EventOf("UserControl", "HitTest")?.CoordinatesInLegacyUnit);
    }

    [TestMethod]
    public void AnMSFormsKeyPressHandler_TakesItsKeyAsciiByRef_Unwrapped()
    {
        var port = MSForms.EventOf("MSForms.TextBox", "KeyPress");
        Assert.IsNotNull(port);
        Assert.AreEqual("KeyPress", port.RDFormsName);
        var keyAscii = port.Parameters.Single();
        Assert.AreEqual(new ParameterPort("KeyAscii", "MSForms.ReturnInteger", "Integer", ByRef: true, UnwrapsValue: true), keyAscii);
        Assert.IsFalse(port.CoordinatesInLegacyUnit);
        Assert.IsNull(port.Note);
    }

    [TestMethod]
    [DataRow("KeyDown")]
    [DataRow("KeyUp")]
    public void AnMSFormsKeyHandler_TakesItsKeyCodeByRef_Unwrapped(string name)
    {
        var port = MSForms.EventOf("ComboBox", name);
        Assert.AreEqual(2, port?.Parameters.Count);
        Assert.AreEqual(new ParameterPort("KeyCode", "MSForms.ReturnInteger", "Integer", true, true), port?.Parameters[0]);
        Assert.AreEqual(new ParameterPort("Shift", "Integer", "Integer", true, false), port?.Parameters[1]);
    }

    [TestMethod]
    public void Enter_IsGotFocus()
    {
        var port = MSForms.EventOf("TextBox", "Enter");
        Assert.AreEqual("GotFocus", port?.RDFormsName);
        Assert.IsTrue(port?.IsRenamed);
        Assert.AreEqual(0, port?.Parameters.Count);
        Assert.IsNull(port?.Note);
    }

    [TestMethod]
    public void Exit_IsValidate_WithANote()
    {
        var port = MSForms.EventOf("TextBox", "Exit");
        Assert.AreEqual("Validate", port?.RDFormsName);
        Assert.AreEqual(new ParameterPort("Cancel", "MSForms.ReturnBoolean", "Boolean", true, true), port?.Parameters.Single());
        Assert.AreEqual(PortDiagnosticKind.ExitPortedToValidate, port?.Note?.Kind);
    }

    [TestMethod]
    public void BeforeUpdate_TakesItsCancelByRef_AndAfterUpdatePortsUnchanged()
    {
        var port = MSForms.EventOf("CheckBox", "BeforeUpdate");
        Assert.AreEqual("BeforeUpdate", port?.RDFormsName);
        Assert.AreEqual("Boolean", port?.Parameters.Single().RDFormsType);
        Assert.IsNull(MSForms.EventOf("CheckBox", "AfterUpdate"));
    }

    [TestMethod]
    public void AnMSFormsDblClickHandler_TakesItsCancelByRef() =>
        Assert.AreEqual("Boolean", MSForms.EventOf("CommandButton", "DblClick")?.Parameters.Single().RDFormsType);

    [TestMethod]
    [DataRow("MouseDown")]
    [DataRow("MouseMove")]
    [DataRow("MouseUp")]
    public void AnMSFormsMouseHandler_ConvertsItsCoordinates(string name)
    {
        Assert.IsTrue(MSForms.EventOf("Label", name)?.CoordinatesInLegacyUnit);
        Assert.IsTrue(MSForms.EventOf("UserForm", name)?.CoordinatesInLegacyUnit);
    }

    [TestMethod]
    [DataRow("MultiPage")]
    [DataRow("TabStrip")]
    public void AMultiPagesAndATabStripsHandlers_NameTheIndexFirst(string legacyClass)
    {
        foreach (var name in new[] { "MouseDown", "MouseMove", "MouseUp", "DblClick", "BeforeDragOver", "BeforeDropOrPaste" })
        {
            var port = MSForms.EventOf(legacyClass, name);
            Assert.AreEqual(legacyClass, port?.LegacyClass, name);
            Assert.AreEqual(new ParameterPort("Index", "Long", "Long", true, false), port?.Parameters[0], name);
        }

        Assert.AreEqual("Index", MSForms.EventOf("MultiPage", "Error")?.Parameters[0].Name);
        Assert.AreEqual("Number", MSForms.EventOf("TabStrip", "Error")?.Parameters[0].Name);
    }

    [TestMethod]
    public void ADragDropHandler_TakesRDFormsTypes()
    {
        var port = MSForms.EventOf("UserForm", "BeforeDragOver");
        Assert.IsNotNull(port);
        Assert.IsTrue(port.CoordinatesInLegacyUnit);
        var parameters = port.Parameters.ToDictionary(p => p.Name);
        Assert.AreEqual(new ParameterPort("Cancel", "MSForms.ReturnBoolean", "Boolean", true, true), parameters["Cancel"]);
        Assert.AreEqual("RDForms.Control", parameters["Control"].RDFormsType);
        Assert.AreEqual("RDForms.DataObject", parameters["Data"].RDFormsType);
        Assert.AreEqual("RDForms.DragState", parameters["State"].RDFormsType);
        Assert.AreEqual(new ParameterPort("Effect", "MSForms.ReturnEffect", "RDForms.ReturnEffect", true, false), parameters["Effect"]);

        var dropOrPaste = MSForms.EventOf("TextBox", "BeforeDropOrPaste");
        Assert.AreEqual("RDForms.Action", dropOrPaste?.Parameters[1].RDFormsType);
        Assert.AreEqual("DragState", MSForms.EventOf("TextBox", "BeforeDragOver")?.Parameters[4].Name);
    }

    [TestMethod]
    public void AnErrorHandler_UnwrapsItsDescriptionAndItsCancelDisplay()
    {
        var parameters = MSForms.EventOf("TextBox", "Error")!.Parameters.ToDictionary(p => p.Name);
        Assert.HasCount(7, parameters);
        Assert.AreEqual(new ParameterPort("Description", "MSForms.ReturnString", "String", true, true), parameters["Description"]);
        Assert.AreEqual("Boolean", parameters["CancelDisplay"].RDFormsType);
        Assert.IsFalse(parameters["SCode"].UnwrapsValue);
    }

    [TestMethod]
    [DataRow("UserForm")]
    [DataRow("Frame")]
    [DataRow("MultiPage")]
    public void AContainersHandlers_TypeTheirControlAnRDFormsControl(string legacyClass)
    {
        Assert.AreEqual("RDForms.Control", MSForms.EventOf(legacyClass, "AddControl")?.Parameters[^1].RDFormsType);
        Assert.AreEqual("RDForms.Control", MSForms.EventOf(legacyClass, "RemoveControl")?.Parameters[^1].RDFormsType);
        var scroll = MSForms.EventOf(legacyClass, "Scroll")!.Parameters.ToDictionary(p => p.Name);
        Assert.AreEqual(new ParameterPort("ActualDx", "MSForms.ReturnSingle", "Single", true, true), scroll["ActualDx"]);
        Assert.AreEqual("RDForms.ScrollAction", scroll["ActionX"].RDFormsType);
    }

    [TestMethod]
    public void QueryClose_IsQueryUnload_KeepingItsParametersNames()
    {
        var port = MSForms.EventOf("UserForm", "QueryClose");
        Assert.IsNotNull(port);
        Assert.AreEqual("QueryUnload", port.RDFormsName);
        CollectionAssert.AreEqual(
            new[] { new ParameterPort("Cancel", "Integer", "Integer", true, false), new ParameterPort("CloseMode", "Integer", "Integer", true, false) },
            port.Parameters.ToArray());
    }

    [TestMethod]
    public void AnEventOnlyOneLibraryHas_PortsUnchanged()
    {
        Assert.IsNull(MSForms.EventOf("UserForm", "Layout"));
        Assert.IsNull(MSForms.EventOf("CheckBox", "Change"));
        Assert.IsNull(MSForms.EventOf("UserForm", "Initialize"));
        Assert.IsNull(MSForms.EventOf("ScrollBar", "Scroll"));
        Assert.IsNull(MSForms.EventOf("SpinButton", "SpinUp"));
    }

    // ProgIDs and handler names

    [TestMethod]
    public void AnMSFormsProgID_NamesItsRDFormsClass()
    {
        foreach (var name in new[] { "CommandButton", "Label", "TextBox", "ListBox", "ComboBox", "CheckBox", "OptionButton", "ToggleButton",
                     "Frame", "Image", "SpinButton", "MultiPage", "TabStrip" })
        {
            Assert.AreEqual("RDForms." + name, MSForms.ProgIdOf("Forms." + name + ".1"), name);
        }

        Assert.AreEqual("RDForms.Form", MSForms.ProgIdOf("Forms.Form.1"));
        Assert.AreEqual("RDForms.VScrollBar", MSForms.ProgIdOf("Forms.ScrollBar.1"));
        Assert.IsNull(MSForms.ProgIdOf("Forms.HTMLText.1"));
        Assert.IsNull(MSForms.ProgIdOf("VB.TextBox"));
    }

    [TestMethod]
    public void AVBProgID_NamesItsRDFormsClass()
    {
        Assert.AreEqual("RDForms.TextBox", VB6.ProgIdOf("VB.TextBox"));
        Assert.AreEqual("RDForms.HScrollBar", VB6.ProgIdOf("VB.HScrollBar"));
        Assert.AreEqual("RDForms.Timer", VB6.ProgIdOf("VB.Timer"));
        Assert.IsNull(VB6.ProgIdOf("Forms.TextBox.1"));
        Assert.IsNull(VB6.ProgIdOf("MSComctlLib.TreeCtrl.2"));
    }

    [TestMethod]
    public void AUserFormsHandlers_AreAFormsHandlers()
    {
        Assert.AreEqual("Form", MSForms.HandlerObjectOf("UserForm"));
        Assert.IsNull(MSForms.HandlerObjectOf("TextBox1"));
        Assert.IsNull(VB6.HandlerObjectOf("Form"));
    }

    // Constants

    [TestMethod]
    public void EveryFmConstant_IsRewrittenAsItsValue()
    {
        Assert.HasCount(191, MSForms.Constants);
        Assert.AreEqual("1", MSForms.ConstantOf("fmBorderStyleSingle"));
        Assert.AreEqual("-1", MSForms.ConstantOf("fmOrientationAuto"));
        Assert.AreEqual("99", MSForms.ConstantOf("fmMousePointerCustom"));
        Assert.IsNull(MSForms.ConstantOf("vbChecked"));
        Assert.IsEmpty(VB6.Constants);
        Assert.IsNull(VB6.ConstantOf("vbChecked"));
    }

    // Names compare as VBA compares them

    [TestMethod]
    public void EveryLookup_IgnoresCase()
    {
        Assert.AreEqual("RDForms.TextBox", MSForms.TypeOf("msforms.TEXTBOX")?.RDFormsName);
        Assert.AreEqual("RDForms.TextBox", VB6.TypeOf("vb.textbox")?.RDFormsName);
        Assert.AreEqual("ToolTipText", MSForms.MemberOf("msforms.textbox", "controltiptext")?.RDFormsName);
        Assert.AreEqual("ScaleWidth", MSForms.MemberOf("USERFORM", "insidewidth")?.RDFormsName);
        Assert.AreEqual("Validate", MSForms.EventOf("control", "EXIT")?.RDFormsName);
        Assert.AreEqual("QueryUnload", MSForms.EventOf("userform", "queryclose")?.RDFormsName);
        Assert.AreEqual("RDForms.TextBox", MSForms.ProgIdOf("forms.textbox.1"));
        Assert.AreEqual("RDForms.TextBox", VB6.ProgIdOf("vb.TEXTBOX"));
        Assert.AreEqual("Form", MSForms.HandlerObjectOf("userform"));
        Assert.AreEqual("2", MSForms.ConstantOf("FMSPECIALEFFECTSUNKEN"));
        Assert.AreEqual("2", MSForms.MemberOf("TextBox", "TextAlign")?.Value?.Constants["FMTEXTALIGNCENTER"]);
    }

    [TestMethod]
    public void EveryEntry_IsFoundByItsOwnLookup()
    {
        foreach (var map in new[] { VB6, MSForms })
        {
            foreach (var type in map.Types)
            {
                Assert.AreSame(type, map.TypeOf(type.LegacyName), type.LegacyName);
                Assert.AreSame(type, map.TypeOf(map.Library + "." + type.LegacyName), type.LegacyName);
                Assert.AreEqual(map.Library, type.LegacyLibrary);
            }

            foreach (var member in map.Members)
            {
                Assert.AreSame(member, map.MemberOf(member.LegacyClass, member.LegacyName), $"{member.LegacyClass}.{member.LegacyName}");
            }

            foreach (var port in map.Events)
            {
                Assert.AreSame(port, map.EventOf(port.LegacyClass, port.LegacyName), $"{port.LegacyClass}.{port.LegacyName}");
            }
        }
    }
}
