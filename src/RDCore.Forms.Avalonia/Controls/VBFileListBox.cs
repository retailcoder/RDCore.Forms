using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB FileListBox: a <see cref="ListBox"/> that shows a <see cref="VBClasses.FileListBox"/> component, the files of a folder.
/// </summary>
/// <remarks>
/// <para>
/// Its items are the names of the files in <see cref="Path"/> that match the component's <c>Pattern</c>, as the names are on disk,
/// sorted without regard to case. A hidden file is listed only where <c>Hidden</c> is True, and a system file only where
/// <c>System</c> is True; <c>Archive</c>, <c>Normal</c> and <c>ReadOnly</c> leave the list as it is, as measured with VB6. The list
/// is read again when <see cref="Path"/>, <c>Pattern</c>, <c>Hidden</c> or <c>System</c> changes and on <see cref="Refresh"/>; a file
/// made or removed on disk shows only then.
/// </para>
/// <para>
/// <c>Pattern</c> holds one or more patterns separated by semicolons, such as <c>*.txt;*.bas</c>, and a file that matches any of them
/// is listed. <c>*</c> stands for any characters and <c>?</c> for one, without regard to case, and a pattern that ends with <c>.*</c>
/// also matches a name with no extension. A change of <c>Pattern</c> raises PatternChange.
/// </para>
/// <para>
/// It raises Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, PathChange and
/// PatternChange. A press, a key or a typed character raises Click as a ListBox's does.
/// </para>
/// </remarks>
public sealed class VBFileListBox : ListBox, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly IFileSystem fileSystem;
    private string path;
    // What the items were read with: Relist reads them again where one of these changed.
    private string? listedPattern;
    private bool listedHidden;
    private bool listedSystem;
    static VBFileListBox()
    {
        // First, so that a key's KeyDown comes before the Click of the selection it moves.
        ControlKeys.Attach<VBFileListBox>();
        SingleSelectList.Attach<VBFileListBox>();
        ControlEvents.AttachMouse<VBFileListBox>(DblClick.InPlaceOfMouseDown);
        FocusableProperty.OverrideDefaultValue<VBFileListBox>(true);
        ControlEvents.AttachFocus<VBFileListBox>();
    }

    /// <summary>
    /// Creates a list that shows a FileListBox.
    /// </summary>
    /// <param name="instance">The FileListBox.</param>
    /// <param name="fileSystem">The file system it lists the files of. Its current directory is the list's first <see cref="Path"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a FileListBox.</exception>
    public VBFileListBox(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        this.instance = instance;
        this.fileSystem = fileSystem;
        path = fileSystem.Path.TrimEndingDirectorySeparator(fileSystem.Directory.GetCurrentDirectory());
        ComponentBinding.Bind(this, instance!, VBClasses.FileListBox, Apply);
        Relist();
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ListBox);

    /// <summary>
    /// The folder whose files are listed, without a separator at its end.
    /// </summary>
    /// <exception cref="FormErrorException">Set to a folder that does not exist: 76, Path not found.</exception>
    /// <remarks>
    /// A relative path is taken from the list's own <see cref="Path"/>. Setting a new folder lists its files, leaves no file
    /// selected, and raises PathChange; setting the same one again raises nothing.
    /// </remarks>
    public string Path
    {
        get => path;
        set => SetPath(Folder(value ?? throw new ArgumentNullException(nameof(value))));
    }

    /// <summary>
    /// The component's <c>Pattern</c>: the patterns a file's name must match to be listed.
    /// </summary>
    /// <remarks>
    /// Setting it lists the files again, and raises PatternChange where it changes.
    /// </remarks>
    public string Pattern
    {
        get => instance.Get(P.Pattern);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            instance.Set(P.Pattern, value);
            Relist();
        }
    }

    /// <summary>
    /// The selected file's name, or "" where no file is selected.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set to a file that does not exist: 53, File not found. Set in a folder that does not exist: 76, Path not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Setting a file's name makes it the <c>Pattern</c>, which lists that file alone, and selects it; it reads back as the name is on
    /// disk. Setting a pattern, one with <c>*</c> or <c>?</c>, makes it the <c>Pattern</c> and selects nothing. A name or a pattern
    /// with a folder sets <see cref="Path"/> to that folder first.
    /// </para>
    /// <para>
    /// It raises PathChange where the folder changes and PatternChange where the <c>Pattern</c> does, and no Click.
    /// </para>
    /// </remarks>
    public string FileName
    {
        get => SelectedIndex >= 0 ? (string)Items[SelectedIndex]! : "";
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var folderPart = fileSystem.Path.GetDirectoryName(value);
            var name = fileSystem.Path.GetFileName(value);
            var folder = string.IsNullOrEmpty(folderPart) ? path : Folder(folderPart);
            if (!fileSystem.Directory.Exists(folder))
                throw new FormErrorException(FormError.PathNotFound);
            string? file = null;
            if (name.IndexOfAny(['*', '?']) < 0)
                file = FileIn(folder, name) ?? throw new FormErrorException(FormError.FileNotFound);
            SetPath(folder);
            Pattern = name;
            if (file is not null)
                SelectedIndex = Items.IndexOf(file);
        }
    }

    /// <summary>
    /// The number of files listed.
    /// </summary>
    public int ListCount => ItemCount;

    /// <summary>
    /// The selected file, -1 for none.
    /// </summary>
    /// <exception cref="FormErrorException">Set outside -1 to <see cref="ListCount"/> - 1: 380, Invalid property value.</exception>
    /// <remarks>
    /// Setting it raises Click when it moves the selection, and none when it is set again.
    /// </remarks>
    public int ListIndex
    {
        get => SelectedIndex;
        set
        {
            if (value < -1 || value >= ItemCount)
                throw new FormErrorException(FormError.InvalidPropertyValue);
            if (value == SelectedIndex)
                return;
            SelectedIndex = value;
            VBControl.Raise(this, VBEvents.Click);
        }
    }

    /// <summary>
    /// Gets a file's name.
    /// </summary>
    /// <param name="index">The file's index, from 0.</param>
    /// <returns>The file's name, or "" for an index outside the list.</returns>
    public string GetList(int index) => index >= 0 && index < ItemCount ? (string)Items[index]! : "";

    /// <summary>
    /// Reads the files of <see cref="Path"/> again, and leaves no file selected.
    /// </summary>
    public void Refresh()
    {
        listedPattern = null;
        Relist();
    }

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

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.Pattern))
        {
            Relist();
            if (!Equals(e.Before, e.After))
                VBControl.Raise(this, VBEvents.PatternChange);
        }
        else if (ReferenceEquals(e.Property, P.Hidden) || ReferenceEquals(e.Property, P.SystemFiles))
            Relist();
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    private string Folder(string value) => FolderPaths.Resolve(fileSystem, path, value);

    private void SetPath(string folder)
    {
        if (!fileSystem.Directory.Exists(folder))
            throw new FormErrorException(FormError.PathNotFound);
        if (string.Equals(folder, path, StringComparison.Ordinal))
            return;
        path = folder;
        Refresh();
        VBControl.Raise(this, VBEvents.PathChange);
    }

    // A file of the folder, by its name as it is on disk: the one of that very name, or else the first that differs only in case.
    private string? FileIn(string folder, string name)
    {
        var names = fileSystem.Directory.EnumerateFiles(folder).Select(fileSystem.Path.GetFileName).ToList();
        return names.FirstOrDefault(n => string.Equals(n, name, StringComparison.Ordinal))
            ?? names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
    }

    private void Relist()
    {
        var pattern = instance.Get(P.Pattern);
        var hidden = instance.Get(P.Hidden);
        var system = instance.Get(P.SystemFiles);
        if (pattern == listedPattern && hidden == listedHidden && system == listedSystem)
            return;
        (listedPattern, listedHidden, listedSystem) = (pattern, hidden, system);
        var patterns = pattern.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Glob).ToList();
        var names = new List<string>();
        try
        {
            foreach (var file in fileSystem.Directory.EnumerateFiles(path))
            {
                var attributes = fileSystem.File.GetAttributes(file);
                if ((attributes.HasFlag(FileAttributes.Hidden) && !hidden) || (attributes.HasFlag(FileAttributes.System) && !system))
                    continue;
                var name = fileSystem.Path.GetFileName(file);
                if (patterns.Any(p => p.IsMatch(name)))
                    names.Add(name);
            }
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            // A folder removed or closed since it was set lists nothing.
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        Items.Clear();
        foreach (var name in names)
            Items.Add(name);
    }

    // A pattern's expression: * any characters, ? one, and a final .* that also lets through a name with no extension.
    private static Regex Glob(string pattern)
    {
        var anyExtension = pattern.EndsWith(".*", StringComparison.Ordinal);
        var stem = Regex.Escape(anyExtension ? pattern[..^2] : pattern).Replace(@"\*", ".*").Replace(@"\?", ".");
        return new Regex(anyExtension ? $@"^{stem}(\..*)?$" : $"^{stem}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
