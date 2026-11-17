namespace RDCore.Forms.Porting;

/// <summary>
/// What converting a member's values from the legacy library to RDForms involves: a <see cref="MemberPort.Unit"/>.
/// </summary>
public enum PortUnit
{
    /// <summary>
    /// The member is not a length: its values need no unit conversion.
    /// </summary>
    None,

    /// <summary>
    /// The member is a length in the library's <see cref="FormsPortMap.LegacyUnit"/>, and converts to DIP by a constant.
    /// </summary>
    Length,

    /// <summary>
    /// The member is a length in the scale of the control's container: its <c>ScaleMode</c>, twips by default. A VB6 control's
    /// <c>Left</c>, <c>Top</c>, <c>Width</c> and <c>Height</c> are such lengths. Converting one to DIP takes the container's scale,
    /// which the designer file gives.
    /// </summary>
    ContainerLength,
}
