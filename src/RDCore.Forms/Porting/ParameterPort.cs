namespace RDCore.Forms.Porting;

/// <summary>
/// One parameter of a ported event handler: its legacy declaration and its RDForms declaration.
/// </summary>
/// <param name="Name">The parameter's name. The port keeps it, so the handler's body reads as written.</param>
/// <param name="LegacyType">
/// The parameter's type as the legacy library declares it, such as <c>MSForms.ReturnInteger</c>; <see langword="null"/> for a
/// parameter the legacy handler does not declare, which the port adds to it, as it adds <c>Cancel</c> to a VB6 <c>DblClick</c> handler.
/// </param>
/// <param name="RDFormsType">The parameter's type in the RDForms handler, such as <c>Integer</c> or <c>RDForms.DataObject</c>.</param>
/// <param name="ByRef">
/// Whether the RDForms handler declares the parameter ByRef. RDForms passes every event argument ByRef, so a handler that assigns
/// it gives the value back to the event's source.
/// </param>
/// <param name="UnwrapsValue">
/// Whether the legacy parameter wraps its value in an object, as MSForms' <c>ReturnInteger</c> does, so that the code port
/// rewrites <c>x.Value</c> on it to <c>x</c>.
/// </param>
public sealed record ParameterPort(string Name, string? LegacyType, string RDFormsType, bool ByRef, bool UnwrapsValue)
{
    /// <summary>
    /// Whether the port adds the parameter to the legacy handler, which does not declare it.
    /// </summary>
    public bool IsAdded => LegacyType is null;
}
