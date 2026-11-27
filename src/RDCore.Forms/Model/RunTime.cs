using RDCore.Forms.Menus;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Model;

/// <summary>
/// A running program's access to its forms' components, with the rules VB6 has for it.
/// </summary>
public static class RunTime
{
    /// <summary>
    /// A running program's assignment of <paramref name="value"/> to <paramref name="property"/> on <paramref name="component"/>:
    /// refused with VB's error where VB6 refuses it, dropped where VB6 drops it (<see cref="Drops"/>), otherwise set.
    /// </summary>
    /// <param name="component">The component assigned.</param>
    /// <param name="property">The property assigned.</param>
    /// <param name="value">The value, of the property's type; an extent is in pixels.</param>
    /// <param name="onAVisualBasic6Form">
    /// Whether the component is on a Visual Basic 6.0 form, rendered with VB's library, where more properties are read-only at run time
    /// (<see cref="ComponentClass.IsReadOnlyAtRunTimeOnAVisualBasic6Form"/>); <see langword="false"/> for an RDForms form.
    /// </param>
    /// <exception cref="ArgumentException">The component's class does not have <paramref name="property"/>.</exception>
    /// <exception cref="FormErrorException">
    /// 382 for a property a program cannot set (<see cref="ComponentClass.IsReadOnlyAtRunTime"/>, and on a Visual Basic 6.0 form
    /// <see cref="ComponentClass.IsReadOnlyAtRunTimeOnAVisualBasic6Form"/>); 380 for a value the property cannot
    /// hold; 387 for an extent of an aligned control that its alignment fixes (<see cref="AlignLayout.Refusal"/>); a menu item's
    /// errors for its <c>Caption</c>, <c>Checked</c>, <c>Enabled</c> and <c>Visible</c> (<see cref="FormMenuItem"/>).
    /// </exception>
    public static void Assign(ComponentInstance component, PropertyDefinition property, object? value, bool onAVisualBasic6Form = true)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        if (!component.Class.Has(property))
            throw new ArgumentException($"{component.Class.ClassName} has no property {property.Name}.", nameof(property));
        if (onAVisualBasic6Form ? component.Class.IsReadOnlyAtRunTimeOnAVisualBasic6Form(property) : component.Class.IsReadOnlyAtRunTime(property))
            throw new FormErrorException(FormError.SetNotSupportedAtRuntime);
        if (!property.Accepts(value) || !property.AcceptsOn(component, value))
            throw new FormErrorException(FormError.InvalidPropertyValue);
        if (AlignLayout.Refusal(component, property) is { } refused)
            throw new FormErrorException(refused);
        if (Drops(component, property))
            return;
        if (ReferenceEquals(component.Class, ComponentClasses.Menu) && AssignToMenu(FormMenuItem.Of(component), property, value))
            return;
        component.TrySet(property, value);
    }

    /// <summary>
    /// Whether VB6 drops a running program's assignment to <paramref name="property"/> on <paramref name="component"/> as it stands: it raises
    /// no error, and the property keeps the value it had. A transparent Label's <c>BackColor</c> is dropped, and making the label opaque
    /// later shows the colour it had. A form's file sets it all the same.
    /// </summary>
    /// <param name="component">The component assigned.</param>
    /// <param name="property">The property assigned.</param>
    /// <returns><see langword="true"/> when the assignment is dropped.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <see cref="Assign"/> drops it once the value is checked. A host that assigns through its own checks asks this before it sets the value.
    /// </remarks>
    public static bool Drops(ComponentInstance component, PropertyDefinition property)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        return ReferenceEquals(property, P.BackColor) && ReferenceEquals(component.Class, ComponentClasses.Label)
            && component.Get(P.BackStyle) == BackStyle.Transparent;
    }

    private static bool AssignToMenu(FormMenuItem item, PropertyDefinition property, object? value)
    {
        switch (property.Name)
        {
            case nameof(P.Caption):
                item.Caption = (string)value!;
                return true;
            case nameof(P.Checked):
                item.Checked = (bool)value!;
                return true;
            case nameof(P.Enabled):
                item.Enabled = (bool)value!;
                return true;
            case nameof(P.Visible):
                item.Visible = (bool)value!;
                return true;
            default:
                return false;
        }
    }
}
