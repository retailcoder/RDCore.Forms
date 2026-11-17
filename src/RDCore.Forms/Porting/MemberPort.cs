namespace RDCore.Forms.Porting;

/// <summary>
/// How a legacy property or method ports to RDForms: its RDForms name, the unit of its values, how its values convert, or why the
/// code port cannot rewrite it.
/// </summary>
/// <param name="LegacyClass">
/// The legacy class the member is on, without the library, such as <c>TextBox</c>. A member every control has is on
/// <see cref="FormsPortMap.ExtenderClass"/>.
/// </param>
/// <param name="LegacyName">The member's legacy name, such as <c>ControlTipText</c>.</param>
/// <param name="RDFormsName">The RDForms member it is rewritten to, such as <c>ToolTipText</c>. It is <paramref name="LegacyName"/> for a member that keeps its name.</param>
/// <param name="Unit">
/// What converting its values involves. For a method, such as <c>Move</c>, it is the unit of its length arguments: the first four.
/// </param>
/// <param name="Value">How its legacy values convert to RDForms values, or <see langword="null"/> when they are the same.</param>
/// <param name="Diagnostic">
/// What the code port reports at a use of the member it cannot rewrite, or <see langword="null"/> when it rewrites every use.
/// </param>
public sealed record MemberPort(string LegacyClass, string LegacyName, string RDFormsName, PortUnit Unit, ValuePort? Value, PortDiagnostic? Diagnostic)
{
    /// <summary>
    /// Whether the member's RDForms name differs from its legacy name, compared without regard to case.
    /// </summary>
    public bool IsRenamed => !string.Equals(LegacyName, RDFormsName, StringComparison.OrdinalIgnoreCase);
}
