using System.Globalization;
using static RDCore.Forms.Porting.PortUnit;

namespace RDCore.Forms.Porting;

// The port map of the MSForms library, from its FM20 type library: the classes take RDForms' names, places convert from points
// to DIP and mouse coordinates from points to twips, the Return* wrappers become ByRef values, and the members MSForms names differently take the RDForms
// member of their concept.
internal static class MSFormsPortData
{
    private const string Library = "MSForms";
    private const string Extender = FormsPortMap.ExtenderClass;

    // The classes of the same name in RDForms. UserForm is RDForms.Form, ScrollBar one of the two scroll bars, NewFont RDForms.Font.
    private static readonly string[] SameNamedClasses =
        [Extender, "CommandButton", "Label", "TextBox", "ComboBox", "ListBox", "CheckBox", "OptionButton", "Frame", "Image", "ToggleButton",
         "SpinButton", "MultiPage", "Page", "TabStrip", "Tab", "Controls", "Pages", "Tabs", "DataObject", "ReturnEffect"];

    // The classes Controls.Add creates by ProgID, Forms.<class>.1.
    private static readonly string[] CreatableControls =
        ["CommandButton", "Label", "TextBox", "ListBox", "ComboBox", "CheckBox", "OptionButton", "ToggleButton", "Frame", "Image",
         "SpinButton", "MultiPage", "TabStrip"];

    private static readonly (string Wrapper, string Type)[] ReturnWrappers =
        [("ReturnInteger", "Integer"), ("ReturnBoolean", "Boolean"), ("ReturnString", "String"), ("ReturnSingle", "Single")];

    private static readonly OrientationPort ScrollBars = new("RDForms.HScrollBar", "RDForms.VScrollBar");

    private static readonly PortDiagnostic Accelerator = PortDiagnostic.AcceleratorAssignment();

    public static FormsPortMap Build() => new(Library, LegacyUnit.Points, Types(), Members(), Events(),
        progIds:
        [
            ("Forms.Form.1", "RDForms.Form"),
            .. CreatableControls.Select(c => ("Forms." + c + ".1", "RDForms." + c)),
            // A ScrollBar Controls.Add creates is taller than it is wide, so its automatic orientation is vertical.
            ("Forms.ScrollBar.1", ScrollBars.Vertical),
        ],
        handlerObjects: [("UserForm", "Form")],
        constants: MSFormsEnums.All.SelectMany(e => e.Members)
            .Select(m => (m.Name, m.Value.ToString(CultureInfo.InvariantCulture))),
        formWindowClass: "ThunderDFrame");

    private static TypePort[] Types() =>
    [
        new(Library, "UserForm", "RDForms.Form", TypePortKind.Class),
        .. SameNamedClasses.Select(c => new TypePort(Library, c, "RDForms." + c, TypePortKind.Class)),
        new(Library, "NewFont", "RDForms.Font", TypePortKind.Class),
        new(Library, "Font", "RDForms.Font", TypePortKind.Class),
        new(Library, "ScrollBar", "RDForms." + Extender, TypePortKind.Class, ScrollBars, PortDiagnostic.ScrollBarReference()),
        .. ReturnWrappers.Select(r => new TypePort(Library, r.Wrapper, r.Type, TypePortKind.Parameter)),
        .. MSFormsEnums.All.Select(e => new TypePort(Library, e.Name, "RDForms." + e.Name[2..], TypePortKind.Enum)),
    ];

    private static MemberPort[] Members() =>
    [
        .. new[] { "Left", "Top", "Width", "Height", "Move" }.Select(m => Member(Extender, m, m, Length)),
        Member("Controls", "Move", "Move", Length),
        Member(Extender, "ControlTipText", "ToolTipText"),

        // The inside of a form, a Frame or a Page is its scale, in DIP.
        .. new[] { "UserForm", "Frame", "Page" }.SelectMany(c => new[]
        {
            Member(c, "InsideWidth", "ScaleWidth", Length),
            Member(c, "InsideHeight", "ScaleHeight", Length),
            Member(c, "ScrollLeft", "ScrollLeft", Length),
            Member(c, "ScrollTop", "ScrollTop", Length),
            Member(c, "ScrollWidth", "ScrollWidth", Length),
            Member(c, "ScrollHeight", "ScrollHeight", Length),
        }),
        .. new[] { "TabStrip", "MultiPage" }.SelectMany(c => new[]
        {
            Member(c, "TabFixedWidth", "TabFixedWidth", Length),
            Member(c, "TabFixedHeight", "TabFixedHeight", Length),
        }),
        .. new[] { "ClientLeft", "ClientTop", "ClientWidth", "ClientHeight" }.Select(m => Member("TabStrip", m, m, Length)),
        .. new[] { "ListBox", "ComboBox" }.Select(c => Member(c, "ListWidth", "ListWidth", Length)),

        // TextAlign's left, center and right are VB's Alignment values.
        .. new[] { "Label", "TextBox", "ListBox", "ComboBox" }.Select(c => Member(c, "TextAlign", "Alignment", value: new ValuePort(
            new Dictionary<object, object> { [1] = 0, [2] = 2, [3] = 1 },
            new Dictionary<string, string> { ["fmTextAlignLeft"] = "0", ["fmTextAlignCenter"] = "2", ["fmTextAlignRight"] = "1" }))),

        // MSForms names the caption's side, VB the box's: fmAlignmentRight, the default, has the box on the left, as VB's 0.
        .. new[] { "CheckBox", "OptionButton", "ToggleButton" }.Select(c => Member(c, "Alignment", "Alignment", value: new ValuePort(
            new Dictionary<object, object> { [0] = 1, [1] = 0 },
            new Dictionary<string, string> { ["fmAlignmentLeft"] = "1", ["fmAlignmentRight"] = "0" }))),

        // An access key is a & in the caption.
        .. new[] { "Label", "CommandButton", "CheckBox", "OptionButton", "ToggleButton", "Page", "Tab" }
            .Select(c => new MemberPort(c, "Accelerator", "Caption", None, null, Accelerator)),

        // A TextBox and a ComboBox keep their text as their Value; RDForms' have Text, their default member, and no Value.
        Member("TextBox", "Value", "Text"),
        Member("ComboBox", "Value", "Text"),
    ];

    private static EventPort[] Events()
    {
        const string KeyCode = "KeyCode As MSForms.ReturnInteger, Shift As Integer";
        const string Mouse = "Button As Integer, Shift As Integer, X As Single, Y As Single";
        const string Error = "Number As Integer, Description As MSForms.ReturnString, SCode As Long, Source As String, HelpFile As String, " +
            "HelpContext As Long, CancelDisplay As MSForms.ReturnBoolean";
        const string Scroll = "ActionX As MSForms.fmScrollAction, ActionY As MSForms.fmScrollAction, RequestDx As Single, RequestDy As Single, " +
            "ActualDx As MSForms.ReturnSingle, ActualDy As MSForms.ReturnSingle";
        const string ControlDragOver = "Cancel As MSForms.ReturnBoolean, Data As MSForms.DataObject, X As Single, Y As Single, " +
            "DragState As MSForms.fmDragState, Effect As MSForms.ReturnEffect, Shift As Integer";
        const string ControlDropOrPaste = "Cancel As MSForms.ReturnBoolean, Action As MSForms.fmAction, Data As MSForms.DataObject, " +
            "X As Single, Y As Single, Effect As MSForms.ReturnEffect, Shift As Integer";
        const string ContainerDragOver = "Cancel As MSForms.ReturnBoolean, Control As MSForms.Control, Data As MSForms.DataObject, " +
            "X As Single, Y As Single, State As MSForms.fmDragState, Effect As MSForms.ReturnEffect, Shift As Integer";
        const string ContainerDropOrPaste = "Cancel As MSForms.ReturnBoolean, Control As MSForms.Control, Action As MSForms.fmAction, " +
            "Data As MSForms.DataObject, X As Single, Y As Single, Effect As MSForms.ReturnEffect, Shift As Integer";
        const string Index = "Index As Long, ";

        return
        [
            // Every control's, and a form's where it raises them.
            Event(Extender, "KeyDown", KeyCode),
            Event(Extender, "KeyUp", KeyCode),
            Event(Extender, "KeyPress", "KeyAscii As MSForms.ReturnInteger"),
            Event(Extender, "DblClick", "Cancel As MSForms.ReturnBoolean"),
            Event(Extender, "MouseDown", Mouse, coordinates: true),
            Event(Extender, "MouseMove", Mouse, coordinates: true),
            Event(Extender, "MouseUp", Mouse, coordinates: true),
            Event(Extender, "BeforeDragOver", ControlDragOver, coordinates: true),
            Event(Extender, "BeforeDropOrPaste", ControlDropOrPaste, coordinates: true),
            Event(Extender, "Error", Error),
            Event(Extender, "Enter", "", rdformsName: "GotFocus"),
            Event(Extender, "Exit", "Cancel As MSForms.ReturnBoolean", rdformsName: "Validate", note: PortDiagnostic.ExitPortedToValidate()),
            Event(Extender, "BeforeUpdate", "Cancel As MSForms.ReturnBoolean"),

            // A form's and a Frame's, which name the control they are about.
            .. new[] { "UserForm", "Frame" }.SelectMany(c => new[]
            {
                Event(c, "BeforeDragOver", ContainerDragOver, coordinates: true),
                Event(c, "BeforeDropOrPaste", ContainerDropOrPaste, coordinates: true),
                Event(c, "Scroll", Scroll),
                Event(c, "AddControl", "Control As MSForms.Control"),
                Event(c, "RemoveControl", "Control As MSForms.Control"),
            }),

            // The VBA designer's event of a UserForm, VB's QueryUnload.
            Event("UserForm", "QueryClose", "Cancel As Integer, CloseMode As Integer", rdformsName: "QueryUnload"),

            // A MultiPage's and a TabStrip's, which name the page or tab first.
            Event("MultiPage", "BeforeDragOver", Index + ContainerDragOver, coordinates: true),
            Event("MultiPage", "BeforeDropOrPaste", Index + ContainerDropOrPaste, coordinates: true),
            Event("MultiPage", "Scroll", Index + Scroll),
            Event("MultiPage", "AddControl", Index + "Control As MSForms.Control"),
            Event("MultiPage", "RemoveControl", Index + "Control As MSForms.Control"),
            Event("MultiPage", "Error", Index + Error),
            Event("TabStrip", "BeforeDragOver", Index + ControlDragOver, coordinates: true),
            Event("TabStrip", "BeforeDropOrPaste", Index + ControlDropOrPaste, coordinates: true),
            .. new[] { "MultiPage", "TabStrip" }.SelectMany(c => new[]
            {
                Event(c, "DblClick", Index + "Cancel As MSForms.ReturnBoolean"),
                Event(c, "MouseDown", Index + Mouse, coordinates: true),
                Event(c, "MouseMove", Index + Mouse, coordinates: true),
                Event(c, "MouseUp", Index + Mouse, coordinates: true),
            }),
        ];
    }

    private static MemberPort Member(string legacyClass, string legacyName, string rdformsName, PortUnit unit = None, ValuePort? value = null) =>
        new(legacyClass, legacyName, rdformsName, unit, value, null);

    private static EventPort Event(string legacyClass, string name, string parameters, bool coordinates = false, string? rdformsName = null,
        PortDiagnostic? note = null) =>
        new(legacyClass, name, rdformsName ?? name, PortSignatures.Parse(parameters, RDFormsType), coordinates, note);

    // A Return* wrapper is its value's type, unwrapped; MSForms' other classes and its enums take the RDForms library.
    private static (string Type, bool UnwrapsValue) RDFormsType(string legacyType)
    {
        if (!legacyType.StartsWith(Library + ".", StringComparison.Ordinal))
        {
            return (legacyType, false);
        }

        var name = legacyType[(Library.Length + 1)..];
        foreach (var (wrapper, type) in ReturnWrappers)
        {
            if (name == wrapper)
            {
                return (type, true);
            }
        }

        return (name.StartsWith("fm", StringComparison.Ordinal) ? "RDForms." + name[2..] : "RDForms." + name, false);
    }
}
