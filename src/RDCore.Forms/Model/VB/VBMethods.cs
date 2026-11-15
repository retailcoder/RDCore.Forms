namespace RDCore.Forms.Model.VB;

/// <summary>
/// The methods of the <c>VB</c> library's classes that RDForms runs: each definition is shared by every class that has the method.
/// </summary>
public static class VBMethods
{
    /// <summary>
    /// <c>SetFocus</c>: gives the focus to a control, or activates a form.
    /// </summary>
    public static readonly MethodDefinition SetFocus = new("SetFocus");

    /// <summary>
    /// <c>Move Left, [Top], [Width], [Height]</c>: sets a component's position and size in one call.
    /// </summary>
    public static readonly MethodDefinition Move = new("Move",
        new MethodParameter("Left", "Single"), new MethodParameter("Top", "Variant", IsOptional: true),
        new MethodParameter("Width", "Variant", IsOptional: true), new MethodParameter("Height", "Variant", IsOptional: true));

    /// <summary>
    /// <c>ZOrder [Position]</c>: brings a control to the front of its plane, or sends it to the back.
    /// </summary>
    public static readonly MethodDefinition ZOrder = new("ZOrder", new MethodParameter("Position", "Variant", IsOptional: true));

    /// <summary>
    /// <c>Refresh</c>: draws a component and its contents again.
    /// </summary>
    public static readonly MethodDefinition Refresh = new("Refresh");

    /// <summary>
    /// <c>UpdateRecord</c>: writes the values of a Data control's bound controls to its current record.
    /// </summary>
    public static readonly MethodDefinition UpdateRecord = new("UpdateRecord");

    /// <summary>
    /// <c>UpdateControls</c>: gives a Data control's bound controls the values of its current record again.
    /// </summary>
    public static readonly MethodDefinition UpdateControls = new("UpdateControls");
}
