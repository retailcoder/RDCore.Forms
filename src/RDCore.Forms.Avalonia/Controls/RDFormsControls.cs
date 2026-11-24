using System.IO.Abstractions;
using Avalonia.Controls;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// Makes the Avalonia control that shows a component.
/// </summary>
public static class RDFormsControls
{
    private static readonly IFileSystem MachineFileSystem = new FileSystem();

    /// <summary>
    /// Makes the control that shows a component of one of RDForms' control classes, or a placed UserControl: a
    /// <see cref="RDFormsUserControl"/>.
    /// </summary>
    /// <param name="instance">The component.</param>
    /// <returns>
    /// The control, or <see langword="null"/> where its class has none: a menu, a form's root, an ImageList, a UserControl that is
    /// invisible at run time, and a class that is neither one of <see cref="ComponentClasses"/> nor one <see cref="UserControlClasses.Of"/>
    /// made.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A DriveListBox, a DirListBox and a FileListBox show the machine's file system, from the process's current directory.
    /// </remarks>
    public static Control? Create(ComponentInstance instance) => Create(instance, MachineFileSystem);

    /// <summary>
    /// Makes the control that shows a component of one of RDForms' control classes, or a placed UserControl, with the file
    /// system its file system lists show.
    /// </summary>
    /// <param name="instance">The component.</param>
    /// <param name="fileSystem">
    /// The file system a DriveListBox, a DirListBox and a FileListBox show, from its current directory, those of a placed UserControl
    /// included.
    /// </param>
    /// <returns>
    /// The control, or <see langword="null"/> where its class has none: a menu, a form's root, an ImageList, a UserControl that is
    /// invisible at run time, and a class that is neither one of <see cref="ComponentClasses"/> nor one <see cref="UserControlClasses.Of"/>
    /// made.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    public static Control? Create(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(fileSystem);
        var type = instance.Class;
        if (ReferenceEquals(type, ComponentClasses.FileListBox))
            return new RDFormsFileListBox(instance, fileSystem);
        if (ReferenceEquals(type, ComponentClasses.DirListBox))
            return new RDFormsDirListBox(instance, fileSystem);
        if (ReferenceEquals(type, ComponentClasses.DriveListBox))
            return new RDFormsDriveListBox(instance, fileSystem);
        if (ReferenceEquals(type, ComponentClasses.CommandButton))
            return new RDFormsCommandButton(instance);
        if (ReferenceEquals(type, ComponentClasses.TextBox))
            return new RDFormsTextBox(instance);
        if (ReferenceEquals(type, ComponentClasses.Label))
            return new RDFormsLabel(instance);
        if (ReferenceEquals(type, ComponentClasses.Frame))
            return new RDFormsFrame(instance);
        if (ReferenceEquals(type, ComponentClasses.CheckBox))
            return new RDFormsCheckBox(instance);
        if (ReferenceEquals(type, ComponentClasses.OptionButton))
            return new RDFormsOptionButton(instance);
        if (ReferenceEquals(type, ComponentClasses.ListBox))
            return new RDFormsListBox(instance);
        if (ReferenceEquals(type, ComponentClasses.ComboBox))
            return new RDFormsComboBox(instance);
        if (ReferenceEquals(type, ComponentClasses.PictureBox))
            return new RDFormsPictureBox(instance);
        if (ReferenceEquals(type, ComponentClasses.Image))
            return new RDFormsImage(instance);
        if (ReferenceEquals(type, ComponentClasses.Timer))
            return new RDFormsTimer(instance);
        if (ReferenceEquals(type, ComponentClasses.HScrollBar) || ReferenceEquals(type, ComponentClasses.VScrollBar))
            return new RDFormsScrollBar(instance);
        if (ReferenceEquals(type, ComponentClasses.Shape))
            return new RDFormsShape(instance);
        if (ReferenceEquals(type, ComponentClasses.Line))
            return new RDFormsLine(instance);
        if (ReferenceEquals(type, ComponentClasses.Data))
            return new RDFormsData(instance);
        if (ReferenceEquals(type, ComponentClasses.ToggleButton))
            return new RDFormsToggleButton(instance);
        if (ReferenceEquals(type, ComponentClasses.SpinButton))
            return new RDFormsSpinButton(instance);
        if (ReferenceEquals(type, ComponentClasses.MultiPage))
            return new RDFormsMultiPage(instance);
        if (ReferenceEquals(type, ComponentClasses.Page))
            return new RDFormsPage(instance);
        if (ReferenceEquals(type, ComponentClasses.TabStrip))
            return new RDFormsTabStrip(instance);
        if (UserControlClasses.DefinitionOf(type) is not null)
            return type.HasExtent ? new RDFormsUserControl(instance, fileSystem) : null;
        return null;
    }
}
