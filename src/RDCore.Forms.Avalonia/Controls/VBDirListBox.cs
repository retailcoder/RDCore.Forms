using System.IO.Abstractions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB DirListBox: a <see cref="ListBox"/> that shows a <see cref="VBClasses.DirListBox"/> component, a folder with the folders
/// above it and the ones it holds.
/// </summary>
/// <remarks>
/// <para>
/// It shows the folders from the root down to <see cref="Path"/>, then the folders <see cref="Path"/> holds, as their names are on
/// disk, sorted without regard to case; a hidden folder is left out. Each is indented one step further than the folder above it, and
/// its subfolders one step further than <see cref="Path"/>. The folders it holds are items 0 to <see cref="ListCount"/> - 1;
/// <see cref="Path"/> itself is item -1, and the folders above it are items -2, -3 and so on up to the root.
/// </para>
/// <para>
/// It raises Change, Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp, and no DblClick. A press,
/// a key or a typed character raises Click as a ListBox's does, and a double click on a folder makes it the <see cref="Path"/>, with no Click for its second release.
/// </para>
/// </remarks>
public sealed class VBDirListBox : ListBox, IRaisesKeyEvents
{
    private const double IndentStep = 8;
    private readonly IFileSystem fileSystem;
    private string path;
    // The folders from the root down to Path: the items before the ones Path holds.
    private int above;

    static VBDirListBox()
    {
        // First, so that a key's KeyDown comes before the Click of the selection it moves.
        ControlKeys.Attach<VBDirListBox>();
        SingleSelectList.Attach<VBDirListBox>();
        // A double click's second press opens the folder pressed, after the Click of its first release; its own release raises none.
        PointerPressedEvent.AddClassHandler<VBDirListBox>((list, e) =>
        {
            if (e.ClickCount == 2 && e.GetCurrentPoint(list).Properties.IsLeftButtonPressed
                && SingleSelectList.IndexAt(list, e.Source) is var index and >= 0)
            {
                SingleSelectList.Forget(list);
                list.Path = ((Folder)list.Items[index]!).FullPath;
            }
        }, handledEventsToo: true);
        ControlEvents.AttachMouse<VBDirListBox>();
        FocusableProperty.OverrideDefaultValue<VBDirListBox>(true);
        ControlEvents.AttachFocus<VBDirListBox>();
    }

    /// <summary>
    /// Creates a list that shows a DirListBox.
    /// </summary>
    /// <param name="instance">The DirListBox.</param>
    /// <param name="fileSystem">The file system it lists the folders of. Its current directory is the list's first <see cref="Path"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a DirListBox.</exception>
    public VBDirListBox(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        this.fileSystem = fileSystem;
        path = fileSystem.Path.TrimEndingDirectorySeparator(fileSystem.Directory.GetCurrentDirectory());
        ComponentBinding.Bind(this, instance!, VBClasses.DirListBox, e => ComponentBinding.ApplyCommon(this, e.Property, e.After));
        Relist();
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ListBox);

    /// <summary>
    /// The folder shown, without a separator at its end, in the case it was given.
    /// </summary>
    /// <exception cref="FormErrorException">Set to a folder that does not exist: 76, Path not found.</exception>
    /// <remarks>
    /// A relative path is taken from the list's own <see cref="Path"/>. Setting a new folder lists it, makes <see cref="ListIndex"/>
    /// -1, and raises Change; setting the same one again raises nothing.
    /// </remarks>
    public string Path
    {
        get => path;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var folder = FolderPaths.Resolve(fileSystem, path, value);
            if (!fileSystem.Directory.Exists(folder))
                throw new FormErrorException(FormError.PathNotFound);
            if (string.Equals(folder, path, StringComparison.Ordinal))
                return;
            path = folder;
            Relist();
            VBControl.Raise(this, VBEvents.Change);
        }
    }

    /// <summary>
    /// The number of folders <see cref="Path"/> holds.
    /// </summary>
    public int ListCount => ItemCount - above;

    /// <summary>
    /// The selected folder: 0 to <see cref="ListCount"/> - 1 for a folder <see cref="Path"/> holds, -1 for <see cref="Path"/>, and -2
    /// and below for the folders above it.
    /// </summary>
    /// <exception cref="FormErrorException">Set to no folder of the list: 380, Invalid property value.</exception>
    /// <remarks>
    /// Setting it raises Click when it moves the selection, and none when it is set again. It leaves <see cref="Path"/> as it is.
    /// </remarks>
    public int ListIndex
    {
        get => SelectedIndex - above;
        set
        {
            if (value < -above || value >= ListCount)
                throw new FormErrorException(FormError.InvalidPropertyValue);
            if (value == ListIndex)
                return;
            SelectedIndex = value + above;
            VBControl.Raise(this, VBEvents.Click);
        }
    }

    /// <summary>
    /// Gets a folder's full path.
    /// </summary>
    /// <param name="index">The folder's index: 0 to <see cref="ListCount"/> - 1 for a folder <see cref="Path"/> holds, -1 for
    /// <see cref="Path"/>, and -2 and below for the folders above it.</param>
    /// <returns>The folder's full path, or "" for an index of no folder.</returns>
    public string GetList(int index) =>
        index >= -above && index < ListCount ? ((Folder)Items[index + above]!).FullPath : "";

    /// <summary>
    /// Reads the folders <see cref="Path"/> holds again, and selects <see cref="Path"/>.
    /// </summary>
    public void Refresh() => Relist();

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var before = SelectedIndex;
        base.OnKeyDown(e);
        SingleSelectList.KeyMoved(this, before);
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        SingleSelectList.Typed(this, e);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (item is Folder folder && container is ListBoxItem listItem)
            listItem.Padding = new Thickness(IndentStep * folder.Depth + 2, listItem.Padding.Top, listItem.Padding.Right, listItem.Padding.Bottom);
    }

    private void Relist()
    {
        var chain = new List<string>();
        for (var folder = path; folder is not null; folder = fileSystem.Path.GetDirectoryName(folder))
            chain.Insert(0, folder);
        var held = new List<string>();
        try
        {
            foreach (var folder in fileSystem.Directory.EnumerateDirectories(path))
                if (!fileSystem.File.GetAttributes(folder).HasFlag(FileAttributes.Hidden))
                    held.Add(folder);
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            // A folder removed or closed since it was set holds nothing.
        }
        held.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(fileSystem.Path.GetFileName(a), fileSystem.Path.GetFileName(b)));
        Items.Clear();
        for (var depth = 0; depth < chain.Count; depth++)
            Items.Add(new Folder(chain[depth], depth == 0 ? chain[depth] : fileSystem.Path.GetFileName(chain[depth]), depth));
        foreach (var folder in held)
            Items.Add(new Folder(folder, fileSystem.Path.GetFileName(folder), chain.Count));
        above = chain.Count;
        SelectedIndex = above - 1;
    }

    // A folder of the list: its full path, the text shown, and how far it is indented.
    private sealed record Folder(string FullPath, string Text, int Depth)
    {
        public override string ToString() => Text;
    }
}
