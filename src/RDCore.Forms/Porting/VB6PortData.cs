using static RDCore.Forms.Porting.PortUnit;

namespace RDCore.Forms.Porting;

// The port map of VB6's VB library. RDForms takes VB's members and values first, so most of VB ports unchanged: the class names
// take the RDForms library, places convert to DIP, and a few members take the RDForms member of their concept.
internal static class VB6PortData
{
    private const string Library = "VB";
    private const string Extender = FormsPortMap.ExtenderClass;

    // The designer roots, the controls and the menu, as the model has them.
    private static readonly string[] Roots = ["Form", "MDIForm", "UserControl", "PropertyPage"];

    private static readonly string[] Controls =
        ["CommandButton", "TextBox", "Label", "CheckBox", "OptionButton", "Frame", "ListBox", "ComboBox", "PictureBox", "Image", "Timer",
         "HScrollBar", "VScrollBar", "Shape", "Line", "DriveListBox", "DirListBox", "FileListBox", "Data"];

    private static readonly string[] Place = ["Left", "Top", "Width", "Height", "Move"];

    public static FormsPortMap Build() => new(Library, LegacyUnit.Twips, Types(), Members(), Events(),
        progIds: Controls.Select(c => (Library + "." + c, "RDForms." + c)),
        handlerObjects: [],
        constants: []);

    private static TypePort[] Types() =>
        [.. Roots.Concat(Controls).Append("Menu").Append(Extender).Append("DataObject")
            .Select(c => new TypePort(Library, c, "RDForms." + c, TypePortKind.Class))];

    private static MemberPort[] Members() =>
    [
        // A control's place is in its container's scale; a form's, on the screen, is in twips whatever its ScaleMode.
        .. Place.Select(m => Member(Extender, m, m, ContainerLength)),
        .. Place.Select(m => Member("Form", m, m, Length)),
        .. Place.Select(m => Member("MDIForm", m, m, Length)),
        .. new[] { "X1", "Y1", "X2", "Y2" }.Select(m => Member("Line", m, m, ContainerLength)),

        // Appearance's 3-D edge is SpecialEffect's sunken one. VB6's VB and VBRUN libraries name no constant of its values.
        Member(Extender, "Appearance", "SpecialEffect", None, new ValuePort(
            new Dictionary<object, object> { [0] = 0, [1] = 2 },
            new Dictionary<string, string>())),

        // An Image's Stretch is PictureSizeMode's clip or stretch.
        Member("Image", "Stretch", "PictureSizeMode", None, new ValuePort(
            new Dictionary<object, object> { [false] = 0, [true] = 1 },
            new Dictionary<string, string>())),

        // A CheckBox's Value is True, False or Null in RDForms, as in MSForms.
        Member("CheckBox", "Value", "Value", None, new ValuePort(
            new Dictionary<object, object> { [0] = false, [1] = true, [2] = DBNull.Value },
            new Dictionary<string, string> { ["vbUnchecked"] = "False", ["vbChecked"] = "True", ["vbGrayed"] = "Null" },
            grayedComparesWithIsNull: true)),
    ];

    // The events whose X and Y are in the container's scale, and DblClick, whose RDForms handler takes Cancel, as an MSForms one does.
    // Every other event ports unchanged.
    private static EventPort[] Events() =>
    [
        new(Extender, "DblClick", "DblClick", [new ParameterPort("Cancel", LegacyType: null, "Boolean", ByRef: true, UnwrapsValue: false)],
            CoordinatesInLegacyUnit: false, Note: null),
        Event(Extender, "MouseDown", "Button As Integer, Shift As Integer, X As Single, Y As Single"),
        Event(Extender, "MouseMove", "Button As Integer, Shift As Integer, X As Single, Y As Single"),
        Event(Extender, "MouseUp", "Button As Integer, Shift As Integer, X As Single, Y As Single"),
        Event(Extender, "DragDrop", "Source As Control, X As Single, Y As Single"),
        Event(Extender, "DragOver", "Source As Control, X As Single, Y As Single, State As Integer"),
        Event(Extender, "OLEDragDrop", "Data As DataObject, Effect As Long, Button As Integer, Shift As Integer, X As Single, Y As Single"),
        Event(Extender, "OLEDragOver", "Data As DataObject, Effect As Long, Button As Integer, Shift As Integer, X As Single, Y As Single, State As Integer"),
        Event("UserControl", "HitTest", "X As Single, Y As Single, HitResult As Integer"),
    ];

    private static MemberPort Member(string legacyClass, string legacyName, string rdformsName, PortUnit unit, ValuePort? value = null) =>
        new(legacyClass, legacyName, rdformsName, unit, value, null);

    private static EventPort Event(string legacyClass, string name, string parameters) =>
        new(legacyClass, name, name, PortSignatures.Parse(parameters, RDFormsType), CoordinatesInLegacyUnit: true, Note: null);

    // VB's own classes in a handler's parameters take the RDForms library.
    private static (string Type, bool UnwrapsValue) RDFormsType(string legacyType) => legacyType switch
    {
        "Control" or "DataObject" => ("RDForms." + legacyType, false),
        _ => (legacyType, false),
    };
}

// The parameters of a handler, from a list such as "KeyCode As MSForms.ReturnInteger, Shift As Integer". RDForms passes every
// event argument ByRef.
internal static class PortSignatures
{
    public static ParameterPort[] Parse(string parameters, Func<string, (string Type, bool UnwrapsValue)> rdformsType) =>
        parameters.Length == 0
            ? []
            : [.. parameters.Split(", ").Select(p =>
            {
                var parts = p.Split(" As ");
                var (type, unwraps) = rdformsType(parts[1]);
                return new ParameterPort(parts[0], parts[1], type, ByRef: true, UnwrapsValue: unwraps);
            })];
}
