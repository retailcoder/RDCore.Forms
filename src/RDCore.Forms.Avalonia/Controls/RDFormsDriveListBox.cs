using System.IO.Abstractions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB DriveListBox: a <see cref="ComboBox"/> that shows a <see cref="ComponentClasses.DriveListBox"/> component, the drives of a file
/// system.
/// </summary>
/// <remarks>
/// <para>
/// A drive with a letter shows as the letter in lower case and a colon, then its volume label in brackets where it has one, such as
/// <c>c: [Windows]</c>. A drive without a letter, as on Linux and macOS, shows as its root's path, such as <c>/</c>.
/// </para>
/// <para>
/// It raises Change, GotFocus, LostFocus, KeyDown, KeyPress and KeyUp, and no Click and no mouse events. Change comes whenever
/// another drive, or none, is chosen, by the user or by code.
/// </para>
/// </remarks>
public sealed class RDFormsDriveListBox : ComboBox, IRaisesKeyEvents
{
    private readonly IFileSystem fileSystem;
    private readonly List<IDriveInfo> drives = [];
    private readonly RDFormsDropDown dropDown;
    // Set while the list reads the drives, which changes no Drive.
    private bool listing;

    static RDFormsDriveListBox()
    {
        ControlKeys.Attach<RDFormsDriveListBox>();
        ControlEvents.AttachFocus<RDFormsDriveListBox>();
    }

    /// <summary>
    /// Creates a list that shows a DriveListBox.
    /// </summary>
    /// <param name="instance">The DriveListBox.</param>
    /// <param name="fileSystem">The file system it lists the drives of. The drive of its current directory is the first one chosen.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a DriveListBox.</exception>
    public RDFormsDriveListBox(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        this.fileSystem = fileSystem;
        dropDown = new RDFormsDropDown(this);
        Padding = new Thickness(2, 0);
        VerticalContentAlignment = VerticalAlignment.Center;
        ComponentBinding.Bind(this, instance!, ComponentClasses.DriveListBox, e => ComponentBinding.ApplyCommon(this, e.Property, e.After));
        Relist(DriveOf(fileSystem.Path.GetFullPath(fileSystem.Directory.GetCurrentDirectory())));
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    /// <summary>
    /// The chosen drive, as it shows, such as <c>c: [Windows]</c>, or "" where none is chosen.
    /// </summary>
    /// <exception cref="FormErrorException">Set to "": 380, Invalid property value. Set to no drive of the list: 68, Device unavailable.</exception>
    /// <remarks>
    /// It is set to a drive's letter, alone or followed by a colon and anything, such as <c>c</c>, <c>c:</c>, <c>c: [Windows]</c> or
    /// <c>C:\Windows</c>, or, for a drive without a letter, to a path on it. Setting another drive raises Change; setting the chosen one again raises nothing.
    /// </remarks>
    public string Drive
    {
        get => SelectedItem as string ?? "";
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length == 0)
                throw new FormErrorException(FormError.InvalidPropertyValue);
            var drive = DriveNamed(value) ?? throw new FormErrorException(FormError.DeviceUnavailable);
            SelectedIndex = drives.IndexOf(drive);
        }
    }

    /// <summary>
    /// The number of drives.
    /// </summary>
    public int ListCount => ItemCount;

    /// <summary>
    /// The chosen drive's index, from 0, or -1 where none is chosen.
    /// </summary>
    /// <exception cref="FormErrorException">Set outside -1 to <see cref="ListCount"/> - 1: 381, Invalid property array index.</exception>
    /// <remarks>
    /// Setting it chooses that drive, or none for -1, and raises Change where that is another.
    /// </remarks>
    public int ListIndex
    {
        get => SelectedIndex;
        set
        {
            if (value < -1 || value >= ItemCount)
                throw new FormErrorException(FormError.InvalidPropertyArrayIndex);
            SelectedIndex = value;
        }
    }

    /// <summary>
    /// Gets a drive as it shows.
    /// </summary>
    /// <param name="index">The drive's index, from 0.</param>
    /// <returns>The drive, or "" for an index outside the list.</returns>
    public string GetList(int index) => index >= 0 && index < ItemCount ? (string)Items[index]! : "";

    /// <summary>
    /// Reads the drives again, and keeps the chosen one where it is still there; otherwise chooses the first, and raises Change.
    /// </summary>
    public void Refresh()
    {
        var chosen = SelectedIndex >= 0 ? drives[SelectedIndex].Name : null;
        Relist(chosen);
        if (SelectedIndex >= 0 && drives[SelectedIndex].Name != chosen)
            RDFormsControl.Raise(this, Events.Change);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => dropDown.CreatePeer();

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        RDFormsDropDown.NameButton(e);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        dropDown.Prepare(container);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty && !listing)
            RDFormsControl.Raise(this, Events.Change);
    }

    private void Relist(string? chosen)
    {
        listing = true;
        try
        {
            drives.Clear();
            drives.AddRange(fileSystem.DriveInfo.GetDrives());
            Items.Clear();
            foreach (var drive in drives)
                Items.Add(Shown(drive));
            SelectedIndex = drives.FindIndex(d => d.Name == chosen);
            if (SelectedIndex < 0 && drives.Count > 0)
                SelectedIndex = 0;
        }
        finally
        {
            listing = false;
        }
    }

    // The drive's name the path is on: the longest drive name that starts it.
    private string? DriveOf(string path) =>
        fileSystem.DriveInfo.GetDrives()
            .Where(d => path.StartsWith(d.Name, FolderPaths.IsLettered(d.Name) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            .MaxBy(d => d.Name.Length)?.Name;

    private IDriveInfo? DriveNamed(string value)
    {
        if (value.Length >= 1 && char.IsAsciiLetter(value[0]) && (value.Length == 1 || value[1] == ':'))
            return drives.FirstOrDefault(d => FolderPaths.IsLettered(d.Name) && char.ToUpperInvariant(d.Name[0]) == char.ToUpperInvariant(value[0]));
        var name = DriveOf(value);
        return drives.FirstOrDefault(d => d.Name == name);
    }

    private static string Shown(IDriveInfo drive)
    {
        if (!FolderPaths.IsLettered(drive.Name))
            return drive.Name;
        var text = $"{char.ToLowerInvariant(drive.Name[0])}:";
        try
        {
            if (drive.IsReady && !string.IsNullOrEmpty(drive.VolumeLabel))
                text += $" [{drive.VolumeLabel}]";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A drive whose label cannot be read shows its letter alone.
        }
        return text;
    }
}
