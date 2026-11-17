namespace RDCore.Forms.Porting;

/// <summary>
/// How one legacy library's forms port to RDForms: its types, members, events, ProgIDs, handler names and constants, and the unit
/// its code uses. A code-porting tool reads it to rewrite the code behind a VB6 form or a VBA UserForm.
/// </summary>
/// <remarks>
/// <para>
/// The map holds data only. Each lookup compares names without regard to case, as VBA does, and takes a name with or without its
/// library, so <c>TextBox</c> and <c>MSForms.TextBox</c> find the same entry.
/// </para>
/// <para>
/// A lookup that finds nothing means the legacy name ports unchanged: RDForms has a type, member or event of that name and meaning.
/// Members with the same name and meaning in both libraries, such as <c>Caption</c>, <c>BackColor</c> and <c>ListIndex</c>, have no
/// entry.
/// </para>
/// <para>
/// A member or event every control has is held once, under <see cref="ExtenderClass"/>. A lookup on a class looks under the class
/// first, then under <see cref="ExtenderClass"/>. A lookup does not check that the class has the member: the tool asks only about
/// members its code uses.
/// </para>
/// </remarks>
public sealed class FormsPortMap
{
    /// <summary>
    /// The legacy class that holds the members and events every control has: <c>Control</c>, the extender of both libraries.
    /// </summary>
    public const string ExtenderClass = "Control";

    private readonly Dictionary<string, TypePort> types;
    private readonly Dictionary<string, MemberPort> members;
    private readonly Dictionary<string, EventPort> events;
    private readonly Dictionary<string, string> progIds;
    private readonly Dictionary<string, string> handlerObjects;
    private readonly Dictionary<string, string> constants;

    internal FormsPortMap(string library, LegacyUnit legacyUnit, IReadOnlyList<TypePort> types, IReadOnlyList<MemberPort> members,
        IReadOnlyList<EventPort> events, IEnumerable<(string ProgId, string RDFormsName)> progIds,
        IEnumerable<(string LegacyObject, string RDFormsObject)> handlerObjects, IEnumerable<(string Legacy, string RDForms)> constants,
        string? formWindowClass = null)
    {
        Library = library;
        LegacyUnit = legacyUnit;
        FormWindowClass = formWindowClass;
        Types = types;
        Members = members;
        Events = events;
        this.types = types.ToDictionary(t => t.LegacyName, StringComparer.OrdinalIgnoreCase);
        this.members = members.ToDictionary(m => Key(m.LegacyClass, m.LegacyName), StringComparer.OrdinalIgnoreCase);
        this.events = events.ToDictionary(e => Key(e.LegacyClass, e.LegacyName), StringComparer.OrdinalIgnoreCase);
        this.progIds = progIds.ToDictionary(p => p.ProgId, p => p.RDFormsName, StringComparer.OrdinalIgnoreCase);
        this.handlerObjects = handlerObjects.ToDictionary(h => h.LegacyObject, h => h.RDFormsObject, StringComparer.OrdinalIgnoreCase);
        this.constants = constants.ToDictionary(c => c.Legacy, c => c.RDForms, StringComparer.OrdinalIgnoreCase);
        Constants = this.constants;
    }

    /// <summary>
    /// The map of VB6's <c>VB</c> library: VB6 forms, UserControls and PropertyPages.
    /// </summary>
    public static FormsPortMap VB6 { get; } = VB6PortData.Build();

    /// <summary>
    /// The map of the <c>MSForms</c> library: VBA UserForms.
    /// </summary>
    public static FormsPortMap MSForms { get; } = MSFormsPortData.Build();

    /// <summary>
    /// The legacy library's name, as its code qualifies a type with it: <c>VB</c> or <c>MSForms</c>.
    /// </summary>
    public string Library { get; }

    /// <summary>
    /// The unit the library's code uses for the places and sizes of controls and for mouse coordinates. A VB6 control's place is in
    /// its container's scale: see <see cref="PortUnit.ContainerLength"/>.
    /// </summary>
    public LegacyUnit LegacyUnit { get; }

    /// <summary>
    /// Every type entry, for a report.
    /// </summary>
    public IReadOnlyList<TypePort> Types { get; }

    /// <summary>
    /// Every member entry, for a report.
    /// </summary>
    public IReadOnlyList<MemberPort> Members { get; }

    /// <summary>
    /// Every event entry, for a report.
    /// </summary>
    public IReadOnlyList<EventPort> Events { get; }

    /// <summary>
    /// Every legacy constant the code port rewrites, and the RDForms constant or literal it writes for it, as VBA source text. The
    /// names compare without regard to case. A member's own constants, which convert with its values, are in its
    /// <see cref="ValuePort.Constants"/>, and take precedence over these where the constant is that member's value.
    /// </summary>
    public IReadOnlyDictionary<string, string> Constants { get; }

    /// <summary>
    /// The window class of the library's forms, as code that finds a form's window gives it to user32's <c>FindWindow</c>:
    /// <c>ThunderDFrame</c> for a UserForm; <see langword="null"/> for the VB6 map, which has none. A ported form is an RDForms window,
    /// so the code port reports a call given this class as <see cref="PortDiagnosticKind.FindWindowThunderDFrame"/>. Windows compares
    /// class names without regard to case.
    /// </summary>
    public string? FormWindowClass { get; }

    /// <summary>
    /// How a legacy type name ports.
    /// </summary>
    /// <param name="legacyName">The type's name, such as <c>TextBox</c>, <c>MSForms.TextBox</c> or <c>ReturnInteger</c>.</param>
    /// <returns>The type's entry, or <see langword="null"/> when the name ports unchanged.</returns>
    public TypePort? TypeOf(string legacyName) => types.GetValueOrDefault(Unqualified(legacyName));

    /// <summary>
    /// How a legacy member of a class ports.
    /// </summary>
    /// <param name="legacyClass">The class the member is used on, such as <c>TextBox</c> or <c>MSForms.TextBox</c>.</param>
    /// <param name="member">The member's legacy name, such as <c>ControlTipText</c>.</param>
    /// <returns>
    /// The member's entry under the class, or else under <see cref="ExtenderClass"/>; <see langword="null"/> when the member ports
    /// unchanged.
    /// </returns>
    public MemberPort? MemberOf(string legacyClass, string member) =>
        members.GetValueOrDefault(Key(Unqualified(legacyClass), member)) ?? members.GetValueOrDefault(Key(ExtenderClass, member));

    /// <summary>
    /// How a handler of a legacy event of a class ports.
    /// </summary>
    /// <param name="legacyClass">The class that raises the event, such as <c>TextBox</c> or <c>MSForms.UserForm</c>.</param>
    /// <param name="legacyEvent">The event's legacy name, such as <c>Exit</c> or <c>QueryClose</c>.</param>
    /// <returns>
    /// The event's entry under the class, or else under <see cref="ExtenderClass"/>; <see langword="null"/> when its handlers port
    /// unchanged.
    /// </returns>
    public EventPort? EventOf(string legacyClass, string legacyEvent) =>
        events.GetValueOrDefault(Key(Unqualified(legacyClass), legacyEvent)) ?? events.GetValueOrDefault(Key(ExtenderClass, legacyEvent));

    /// <summary>
    /// The RDForms class a legacy ProgID names, as a literal given to <c>Controls.Add</c>.
    /// </summary>
    /// <param name="progId">The ProgID, such as <c>Forms.TextBox.1</c> or <c>VB.TextBox</c>.</param>
    /// <returns>
    /// The RDForms class name to write in its place, such as <c>RDForms.TextBox</c>, or <see langword="null"/> when the map has no
    /// class for it, which the code port reports as <see cref="PortDiagnosticKind.UnknownProgId"/>.
    /// </returns>
    public string? ProgIdOf(string progId) => progIds.GetValueOrDefault(progId);

    /// <summary>
    /// The object name an event handler of a legacy object is renamed to: <c>UserForm</c> to <c>Form</c>, so that
    /// <c>UserForm_Initialize</c> becomes <c>Form_Initialize</c>.
    /// </summary>
    /// <param name="legacyObject">The object part of a handler's name, before its underscore.</param>
    /// <returns>The RDForms object name, or <see langword="null"/> when the handler keeps its object name.</returns>
    public string? HandlerObjectOf(string legacyObject) => handlerObjects.GetValueOrDefault(legacyObject);

    /// <summary>
    /// The RDForms constant or literal a legacy constant is rewritten to.
    /// </summary>
    /// <param name="legacyConstant">The constant, such as <c>fmBorderStyleSingle</c>.</param>
    /// <returns>Its RDForms constant or literal, as VBA source text, or <see langword="null"/> when the constant ports unchanged.</returns>
    public string? ConstantOf(string legacyConstant) => constants.GetValueOrDefault(legacyConstant);

    private static string Key(string legacyClass, string name) => legacyClass + "." + name;

    private string Unqualified(string name) =>
        name.Length > Library.Length + 1 && name[Library.Length] == '.' && name.StartsWith(Library, StringComparison.OrdinalIgnoreCase)
            ? name[(Library.Length + 1)..]
            : name;
}
