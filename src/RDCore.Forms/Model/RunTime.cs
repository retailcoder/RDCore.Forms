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
    /// refused with VB's error where VB6 refuses it, otherwise set.
    /// </summary>
    /// <param name="component">The component assigned.</param>
    /// <param name="property">The property assigned.</param>
    /// <param name="value">The value, of the property's type; an extent is in pixels.</param>
    /// <exception cref="ArgumentException">The component's class does not have <paramref name="property"/>.</exception>
    /// <exception cref="FormErrorException">
    /// 382 for a property a program cannot set (<see cref="ComponentClass.IsReadOnlyAtRunTime"/>); 380 for a value the property cannot
    /// hold; 387 for an extent of an aligned control that its alignment fixes (<see cref="AlignLayout.Refusal"/>); a menu item's
    /// errors for its <c>Caption</c>, <c>Checked</c>, <c>Enabled</c> and <c>Visible</c> (<see cref="FormMenuItem"/>).
    /// </exception>
    public static void Assign(ComponentInstance component, PropertyDefinition property, object? value)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        if (!component.Class.Has(property))
            throw new ArgumentException($"{component.Class.ClassName} has no property {property.Name}.", nameof(property));
        if (component.Class.IsReadOnlyAtRunTime(property))
            throw new FormErrorException(FormError.SetNotSupportedAtRuntime);
        if (!property.Accepts(value))
            throw new FormErrorException(FormError.InvalidPropertyValue);
        if (AlignLayout.Refusal(component, property) is { } refused)
            throw new FormErrorException(refused);
        if (ReferenceEquals(component.Class, ComponentClasses.Menu) && AssignToMenu(FormMenuItem.Of(component), property, value))
            return;
        component.TrySet(property, value);
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
