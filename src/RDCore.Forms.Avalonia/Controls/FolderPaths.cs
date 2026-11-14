using System.IO.Abstractions;

namespace RDCore.Forms.Avalonia.Controls;

// The paths of the file system lists.
internal static class FolderPaths
{
    // A folder as a list's Path holds it: full, taken from the list's own path where it is relative, and without a separator at its
    // end. A drive as a DriveListBox's Drive reads it, "c:" alone or followed by a space and its label, is the file system's current
    // directory where that is on the drive, and the drive's root where it is not.
    public static string Resolve(IFileSystem fileSystem, string from, string value)
    {
        if (value.Length >= 2 && value[1] == ':' && (value.Length == 2 || value[2] == ' ') && Drive(fileSystem, value[0]) is { } drive)
        {
            var current = fileSystem.Directory.GetCurrentDirectory();
            value = current.StartsWith(drive.Name, StringComparison.OrdinalIgnoreCase) ? current : drive.Name;
        }
        return fileSystem.Path.TrimEndingDirectorySeparator(fileSystem.Path.GetFullPath(fileSystem.Path.Combine(from, value)));
    }

    // The drive of a letter, such as C:\ for 'c', or none.
    public static IDriveInfo? Drive(IFileSystem fileSystem, char letter) =>
        char.IsAsciiLetter(letter)
            ? fileSystem.DriveInfo.GetDrives().FirstOrDefault(d => IsLettered(d.Name) && char.ToUpperInvariant(d.Name[0]) == char.ToUpperInvariant(letter))
            : null;

    // Whether a drive's name is a letter and a colon, as on Windows.
    public static bool IsLettered(string name) => name.Length >= 2 && char.IsAsciiLetter(name[0]) && name[1] == ':';
}
