using System.Globalization;
using System.Resources;

namespace RDCore.Forms.Porting;

/// <summary>
/// What a <see cref="PortDiagnostic"/> reports. Each kind's name is the stable name of the diagnostic, and the key of its message.
/// </summary>
public enum PortDiagnosticKind
{
    /// <summary>
    /// An assignment to MSForms' <c>Accelerator</c>. RDForms writes an access key as a <c>&amp;</c> before its letter in the
    /// <c>Caption</c>, and the code port cannot place it without the caption.
    /// </summary>
    AcceleratorAssignment,

    /// <summary>
    /// A legacy value that is none of the documented values of its member, so it has no RDForms value. It is what
    /// <see cref="ValueConversion.Diagnostic"/> reports.
    /// </summary>
    ValueNotConvertible,

    /// <summary>
    /// A value of a member whose values convert, given by an expression that is neither a literal nor a constant, so the code port
    /// cannot convert it.
    /// </summary>
    ExpressionNotConvertible,

    /// <summary>
    /// A ProgID given to <c>Controls.Add</c> by an expression that is not a literal, so the code port cannot rewrite it.
    /// </summary>
    ComputedProgId,

    /// <summary>
    /// A literal ProgID given to <c>Controls.Add</c> that names no class the port map has.
    /// </summary>
    UnknownProgId,

    /// <summary>
    /// A late-bound call, on a variable declared <c>As Object</c> or <c>As Variant</c>, to a member the port renames or whose
    /// values or unit it converts. The code port cannot tell which control the call is on.
    /// </summary>
    LateBoundMember,

    /// <summary>
    /// A <c>ControlSource</c> or <c>RowSource</c> string that nothing resolves, such as a worksheet range.
    /// </summary>
    UnresolvedDataSource,

    /// <summary>
    /// An MSForms <c>Exit</c> handler, ported to <c>Validate</c>. RDForms raises Validate only when the control that takes the
    /// focus has <c>CausesValidation</c> set to <c>True</c>, the default.
    /// </summary>
    ExitPortedToValidate,

    /// <summary>
    /// A reference to MSForms' <c>ScrollBar</c> that the code port cannot tie to one control, so it is typed
    /// <c>RDForms.Control</c>: a ScrollBar ports to <c>RDForms.HScrollBar</c> or <c>RDForms.VScrollBar</c> by its orientation.
    /// </summary>
    ScrollBarReference,

    /// <summary>
    /// An expression the code port wrapped in a call of the <c>RDForms.Units</c> module, such as <c>FromTwips(...)</c>, to keep it
    /// in its legacy unit.
    /// </summary>
    UnitWrapped,

    /// <summary>
    /// A handler the code port gave two first lines converting <c>X</c> and <c>Y</c> back to the legacy unit.
    /// </summary>
    CoordinatesConverted,

    /// <summary>
    /// A <c>TypeName</c> or <c>TypeOf ... Is</c> comparison on a control ported from MSForms' <c>ScrollBar</c>: the control is an
    /// <c>RDForms.HScrollBar</c> or an <c>RDForms.VScrollBar</c>, so <c>TypeName(x) = "ScrollBar"</c> is <c>False</c> after the port.
    /// </summary>
    ScrollBarTypeNameChanged,

    /// <summary>
    /// A call of user32's <c>FindWindow</c> given the literal window class <c>"ThunderDFrame"</c>, the
    /// <see cref="FormsPortMap.FormWindowClass"/> of a UserForm. A ported form is an RDForms window, which has no such class, so the
    /// call finds no window. Code that finds the window to set its icon sets the form's <c>Icon</c> property instead.
    /// </summary>
    FindWindowThunderDFrame,
}

/// <summary>
/// How much a <see cref="PortDiagnostic"/> costs the port.
/// </summary>
public enum PortDiagnosticSeverity
{
    /// <summary>
    /// A fact about the ported code.
    /// </summary>
    Hint,

    /// <summary>
    /// A code action fixes it.
    /// </summary>
    Information,

    /// <summary>
    /// The port changes what the code does.
    /// </summary>
    Warning,

    /// <summary>
    /// The port cannot be done at the site: the code is left as written, for its author to port.
    /// </summary>
    Error,
}

/// <summary>
/// What the code port reports at a site of the ported code: a site it cannot rewrite, or a rewrite its author should know about.
/// </summary>
/// <remarks>
/// Its <see cref="Message"/> is read in the current UI culture each time, English or French. Two diagnostics are equal when they
/// are of the same kind with the same arguments.
/// </remarks>
public sealed record PortDiagnostic
{
    private static readonly ResourceManager Messages = new("RDCore.Forms.Porting.PortMessages", typeof(PortDiagnostic).Assembly);

    private PortDiagnostic(PortDiagnosticKind kind, params string[] arguments)
    {
        Kind = kind;
        Arguments = arguments;
    }

    /// <summary>
    /// What is reported.
    /// </summary>
    public PortDiagnosticKind Kind { get; }

    /// <summary>
    /// The diagnostic's code, such as <c>RDC40001</c>.
    /// </summary>
    public string Code => PortDiagnosticCodes.CodeOf(Kind);

    /// <summary>
    /// How much the reported thing costs the port.
    /// </summary>
    public PortDiagnosticSeverity Severity => PortDiagnosticCodes.SeverityOf(Kind);

    /// <summary>
    /// The values the message names, as VBA source writes them, in the order of its placeholders.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>
    /// The message, in the current UI culture.
    /// </summary>
    public string Message => MessageIn(CultureInfo.CurrentUICulture);

    /// <summary>
    /// The message in a given culture: French for a French culture, English for any other.
    /// </summary>
    /// <param name="culture">The culture to read the message in.</param>
    /// <returns>The message.</returns>
    public string MessageIn(CultureInfo culture)
    {
        var format = Messages.GetString(Kind.ToString(), culture) ?? Kind.ToString();
        return string.Format(culture, format, Arguments.Cast<object?>().ToArray());
    }

    /// <summary>
    /// The code of a kind of diagnostic.
    /// </summary>
    /// <param name="kind">The kind of diagnostic.</param>
    /// <returns>Its code, such as <c>RDC40001</c>.</returns>
    public static string CodeOf(PortDiagnosticKind kind) => PortDiagnosticCodes.CodeOf(kind);

    /// <summary>
    /// The severity of a kind of diagnostic.
    /// </summary>
    /// <param name="kind">The kind of diagnostic.</param>
    /// <returns>Its severity.</returns>
    public static PortDiagnosticSeverity SeverityOf(PortDiagnosticKind kind) => PortDiagnosticCodes.SeverityOf(kind);

    /// <summary>
    /// An assignment to <c>Accelerator</c>: <see cref="PortDiagnosticKind.AcceleratorAssignment"/>.
    /// </summary>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic AcceleratorAssignment() => new(PortDiagnosticKind.AcceleratorAssignment);

    /// <summary>
    /// A legacy value with no RDForms value: <see cref="PortDiagnosticKind.ValueNotConvertible"/>.
    /// </summary>
    /// <param name="legacy">The legacy value.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ValueNotConvertible(object? legacy) => new(PortDiagnosticKind.ValueNotConvertible, LegacyValue.Text(legacy));

    /// <summary>
    /// A value given by an expression the code port cannot convert: <see cref="PortDiagnosticKind.ExpressionNotConvertible"/>.
    /// </summary>
    /// <param name="legacyMember">The legacy member the value is for, such as <c>TextAlign</c>.</param>
    /// <param name="rdformsMember">The RDForms member it ports to, such as <c>Alignment</c>.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ExpressionNotConvertible(string legacyMember, string rdformsMember) =>
        new(PortDiagnosticKind.ExpressionNotConvertible, legacyMember, rdformsMember);

    /// <summary>
    /// A ProgID that is not a literal: <see cref="PortDiagnosticKind.ComputedProgId"/>.
    /// </summary>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ComputedProgId() => new(PortDiagnosticKind.ComputedProgId);

    /// <summary>
    /// A literal ProgID the port map has no class for: <see cref="PortDiagnosticKind.UnknownProgId"/>.
    /// </summary>
    /// <param name="progId">The ProgID.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic UnknownProgId(string progId) => new(PortDiagnosticKind.UnknownProgId, progId);

    /// <summary>
    /// A late-bound call to a member the port changes: <see cref="PortDiagnosticKind.LateBoundMember"/>.
    /// </summary>
    /// <param name="legacyMember">The legacy member the call names.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic LateBoundMember(string legacyMember) => new(PortDiagnosticKind.LateBoundMember, legacyMember);

    /// <summary>
    /// A data source string nothing resolves: <see cref="PortDiagnosticKind.UnresolvedDataSource"/>.
    /// </summary>
    /// <param name="source">The <c>ControlSource</c> or <c>RowSource</c> string.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic UnresolvedDataSource(string source) => new(PortDiagnosticKind.UnresolvedDataSource, source);

    /// <summary>
    /// An Exit handler ported to Validate: <see cref="PortDiagnosticKind.ExitPortedToValidate"/>.
    /// </summary>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ExitPortedToValidate() => new(PortDiagnosticKind.ExitPortedToValidate);

    /// <summary>
    /// A ScrollBar reference typed <c>RDForms.Control</c>: <see cref="PortDiagnosticKind.ScrollBarReference"/>.
    /// </summary>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ScrollBarReference() => new(PortDiagnosticKind.ScrollBarReference);

    /// <summary>
    /// An expression wrapped to keep its legacy unit: <see cref="PortDiagnosticKind.UnitWrapped"/>.
    /// </summary>
    /// <param name="function">The <c>RDForms.Units</c> function it is wrapped in, such as <c>FromTwips</c>.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic UnitWrapped(string function) => new(PortDiagnosticKind.UnitWrapped, function);

    /// <summary>
    /// A handler whose X and Y are converted back to the legacy unit: <see cref="PortDiagnosticKind.CoordinatesConverted"/>.
    /// </summary>
    /// <param name="function">The <c>RDForms.Units</c> function the lines call, such as <c>ToPoints</c>.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic CoordinatesConverted(string function) => new(PortDiagnosticKind.CoordinatesConverted, function);

    /// <summary>
    /// A type comparison on a ported ScrollBar: <see cref="PortDiagnosticKind.ScrollBarTypeNameChanged"/>.
    /// </summary>
    /// <param name="rdFormsName">The control's RDForms class, such as <c>RDForms.HScrollBar</c>.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic ScrollBarTypeNameChanged(string rdFormsName) =>
        new(PortDiagnosticKind.ScrollBarTypeNameChanged, rdFormsName);

    /// <summary>
    /// A <c>FindWindow</c> call looking for a UserForm's window: <see cref="PortDiagnosticKind.FindWindowThunderDFrame"/>.
    /// </summary>
    /// <param name="function">The name the code calls the declared function by, such as <c>FindWindow</c> or <c>FindWindowA</c>.</param>
    /// <returns>The diagnostic.</returns>
    public static PortDiagnostic FindWindowThunderDFrame(string function) => new(PortDiagnosticKind.FindWindowThunderDFrame, function);

    /// <summary>
    /// Whether another diagnostic is of the same kind, with the same arguments.
    /// </summary>
    /// <param name="other">The other diagnostic.</param>
    /// <returns><see langword="true"/> when the two are equal.</returns>
    public bool Equals(PortDiagnostic? other) =>
        other is not null && Kind == other.Kind && Arguments.SequenceEqual(other.Arguments, StringComparer.Ordinal);

    /// <summary>
    /// A hash of the kind and the arguments.
    /// </summary>
    /// <returns>The hash.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Kind);
        foreach (var argument in Arguments)
        {
            hash.Add(argument, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// The code and the message, in the current UI culture.
    /// </summary>
    /// <returns>The diagnostic as text.</returns>
    public override string ToString() => $"{Code}: {Message}";
}
