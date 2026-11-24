namespace RDCore.Forms.Porting;

/// <summary>
/// How a legacy event handler ports to RDForms: the event's RDForms name and the handler's RDForms parameters.
/// </summary>
/// <param name="LegacyClass">
/// The legacy class that raises the event, without the library, such as <c>TextBox</c>. An event every control raises with the
/// same parameters is on <see cref="FormsPortMap.ExtenderClass"/>.
/// </param>
/// <param name="LegacyName">The event's legacy name, such as <c>Exit</c>.</param>
/// <param name="RDFormsName">The RDForms event the handler is renamed to, such as <c>Validate</c>. It is <paramref name="LegacyName"/> for an event that keeps its name.</param>
/// <param name="Parameters">
/// The handler's parameters, in order, each with its legacy and its RDForms declaration. A parameter the legacy handler does not
/// declare (<see cref="ParameterPort.IsAdded"/>) follows the ones it does.
/// </param>
/// <param name="CoordinatesInLegacyUnit">
/// Whether the event's <c>X</c> and <c>Y</c> are in the legacy unit: <see cref="FormsPortMap.LegacyUnit"/> for MSForms, the
/// container's scale for VB6. RDForms raises them in twips, and a form's own in its scale. Where the legacy code expects another
/// unit, as MSForms code expects points, the code port gives the handler two first lines that convert them, and its body runs as
/// written.
/// </param>
/// <param name="Note">
/// What the code port reports at each handler it ports, where RDForms raises the event differently, or <see langword="null"/>.
/// </param>
public sealed record EventPort(string LegacyClass, string LegacyName, string RDFormsName, IReadOnlyList<ParameterPort> Parameters,
    bool CoordinatesInLegacyUnit, PortDiagnostic? Note)
{
    /// <summary>
    /// Whether the event's RDForms name differs from its legacy name, compared without regard to case.
    /// </summary>
    public bool IsRenamed => !string.Equals(LegacyName, RDFormsName, StringComparison.OrdinalIgnoreCase);
}
