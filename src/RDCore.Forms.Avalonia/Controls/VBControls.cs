using System.IO.Abstractions;
using Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// Makes the Avalonia control that shows a component.
/// </summary>
public static class VBControls
{
    private static readonly IFileSystem MachineFileSystem = new FileSystem();

    /// <summary>
    /// Makes the control that shows a component of one of VB's intrinsic control classes.
    /// </summary>
    /// <param name="instance">The component.</param>
    /// <returns>
    /// The control, or <see langword="null"/> where its class has none: a menu and a form's root.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A DriveListBox, a DirListBox and a FileListBox show the machine's file system, from the process's current directory.
    /// </remarks>
    public static Control? Create(ComponentInstance instance) => Create(instance, MachineFileSystem);

    /// <summary>
    /// Makes the control that shows a component of one of VB's intrinsic control classes, with the file system its file system lists
    /// show.
    /// </summary>
    /// <param name="instance">The component.</param>
    /// <param name="fileSystem">The file system a DriveListBox, a DirListBox and a FileListBox show, from its current directory.</param>
    /// <returns>
    /// The control, or <see langword="null"/> where its class has none: a menu and a form's root.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    public static Control? Create(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(fileSystem);
        var type = instance.Class;
        if (ReferenceEquals(type, VBClasses.FileListBox))
            return new VBFileListBox(instance, fileSystem);
        if (ReferenceEquals(type, VBClasses.DirListBox))
            return new VBDirListBox(instance, fileSystem);
        if (ReferenceEquals(type, VBClasses.DriveListBox))
            return new VBDriveListBox(instance, fileSystem);
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
        if (ReferenceEquals(type, VBClasses.Timer))
            return new VBTimer(instance);
        if (ReferenceEquals(type, VBClasses.HScrollBar) || ReferenceEquals(type, VBClasses.VScrollBar))
            return new VBScrollBar(instance);
        if (ReferenceEquals(type, VBClasses.Shape))
            return new VBShape(instance);
        if (ReferenceEquals(type, VBClasses.Line))
            return new VBLine(instance);
        return null;
    }
}
