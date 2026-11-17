namespace RDCore.Forms.Porting;

/// <summary>
/// What a legacy type name stands for: a <see cref="TypePort.Kind"/>.
/// </summary>
public enum TypePortKind
{
    /// <summary>
    /// A class: a form, a control, a collection or a helper object. An <c>As</c>, <c>TypeOf ... Is</c>, <c>New</c> or
    /// <c>Implements</c> reference to it is rewritten to the RDForms class.
    /// </summary>
    Class,

    /// <summary>
    /// An enum. A reference to it is rewritten to the RDForms enum, and its members to <see cref="FormsPortMap.ConstantOf"/>.
    /// </summary>
    Enum,

    /// <summary>
    /// A wrapper an event passes a value in, such as MSForms' <c>ReturnInteger</c>. A parameter of that type becomes a ByRef
    /// parameter of the RDForms type, and <c>x.Value</c> on it becomes <c>x</c>.
    /// </summary>
    Parameter,
}

/// <summary>
/// How a legacy type name ports to RDForms: <c>MSForms.TextBox</c> to <c>RDForms.TextBox</c>, or <c>MSForms.ReturnInteger</c> to
/// <c>Integer</c>.
/// </summary>
/// <param name="LegacyLibrary">The legacy library the type is in, such as <c>VB</c> or <c>MSForms</c>.</param>
/// <param name="LegacyName">The type's name in that library, without the library, such as <c>TextBox</c>.</param>
/// <param name="RDFormsName">
/// The RDForms type a reference to it is rewritten to, such as <c>RDForms.TextBox</c>, or an intrinsic type such as <c>Integer</c>
/// for a <see cref="TypePortKind.Parameter"/>.
/// </param>
/// <param name="Kind">What the legacy name stands for.</param>
/// <param name="ByOrientation">
/// For a legacy class that is one of two RDForms classes by its orientation, such as MSForms' <c>ScrollBar</c>, the two classes;
/// otherwise <see langword="null"/>.
/// </param>
/// <param name="Diagnostic">
/// What the code port reports where it rewrites a reference to the type, or <see langword="null"/> when the rewrite is exact.
/// </param>
public sealed record TypePort(string LegacyLibrary, string LegacyName, string RDFormsName, TypePortKind Kind,
    OrientationPort? ByOrientation = null, PortDiagnostic? Diagnostic = null);

/// <summary>
/// The two RDForms classes a legacy class with an <c>Orientation</c> ports to, such as MSForms' <c>ScrollBar</c> to
/// <c>RDForms.HScrollBar</c> or <c>RDForms.VScrollBar</c>.
/// </summary>
/// <remarks>
/// The designer port knows each control's orientation and size, so it picks one class per control. A reference in code that the
/// rewriter cannot tie to one control is typed <see cref="TypePort.RDFormsName"/>.
/// </remarks>
/// <param name="Horizontal">The RDForms class of a horizontal control.</param>
/// <param name="Vertical">The RDForms class of a vertical control.</param>
public sealed record OrientationPort(string Horizontal, string Vertical)
{
    /// <summary>
    /// The value of <c>fmOrientationAuto</c>: the orientation follows the control's longer side.
    /// </summary>
    public const int Auto = -1;

    /// <summary>
    /// The value of <c>fmOrientationVertical</c>.
    /// </summary>
    public const int VerticalOrientation = 0;

    /// <summary>
    /// The value of <c>fmOrientationHorizontal</c>.
    /// </summary>
    public const int HorizontalOrientation = 1;

    /// <summary>
    /// The RDForms class of a control with a given orientation and size.
    /// </summary>
    /// <remarks>
    /// With <see cref="Auto"/>, a control wider than it is tall is horizontal, and any other is vertical.
    /// </remarks>
    /// <param name="orientation">The control's <c>Orientation</c>: <see cref="Auto"/>, <see cref="VerticalOrientation"/> or <see cref="HorizontalOrientation"/>.</param>
    /// <param name="width">The control's width, in any unit.</param>
    /// <param name="height">The control's height, in the unit of <paramref name="width"/>.</param>
    /// <returns>The class, or <see langword="null"/> when <paramref name="orientation"/> is none of the three values.</returns>
    public string? RDFormsNameOf(int orientation, double width, double height) => orientation switch
    {
        HorizontalOrientation => Horizontal,
        VerticalOrientation => Vertical,
        Auto => width > height ? Horizontal : Vertical,
        _ => null,
    };

    /// <summary>
    /// What the code port reports at a <c>TypeName</c> or <c>TypeOf ... Is</c> comparison on a control ported to one of the two
    /// classes, whose type name is no longer the legacy class's.
    /// </summary>
    /// <param name="rdFormsName">The control's RDForms class: <see cref="Horizontal"/> or <see cref="Vertical"/>.</param>
    /// <returns>The diagnostic.</returns>
    public PortDiagnostic TypeNameChanged(string rdFormsName) => PortDiagnostic.ScrollBarTypeNameChanged(rdFormsName);
}
