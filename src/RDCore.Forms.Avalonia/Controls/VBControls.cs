using Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// Makes the Avalonia control that shows a component.
/// </summary>
public static class VBControls
{
    /// <summary>
    /// Makes the control that shows a component of one of VB's intrinsic control classes.
    /// </summary>
    /// <param name="instance">The component.</param>
    /// <returns>
    /// The control, or <see langword="null"/> where its class has none: a Timer, a scroll bar, a Shape, a Line, a menu, and a
    /// form's root.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    public static Control? Create(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var type = instance.Class;
        if (ReferenceEquals(type, VBClasses.CommandButton))
            return new VBCommandButton(instance);
        if (ReferenceEquals(type, VBClasses.TextBox))
            return new VBTextBox(instance);
        if (ReferenceEquals(type, VBClasses.Label))
            return new VBLabel(instance);
        if (ReferenceEquals(type, VBClasses.Frame))
            return new VBFrame(instance);
        if (ReferenceEquals(type, VBClasses.CheckBox))
            return new VBCheckBox(instance);
        if (ReferenceEquals(type, VBClasses.OptionButton))
            return new VBOptionButton(instance);
        if (ReferenceEquals(type, VBClasses.ListBox))
            return new VBListBox(instance);
        if (ReferenceEquals(type, VBClasses.ComboBox))
            return new VBComboBox(instance);
        if (ReferenceEquals(type, VBClasses.PictureBox))
            return new VBPictureBox(instance);
        if (ReferenceEquals(type, VBClasses.Image))
            return new VBImage(instance);
        return null;
    }
}
