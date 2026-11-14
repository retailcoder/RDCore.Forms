# File system lists

DriveListBox lists the drives, DirListBox the folders and FileListBox the files of a folder. They read them from the [`IFileSystem`](https://github.com/TestableIO/System.IO.Abstractions) a host gives [`VBFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml), [`VBFormView`](../api/RDCore.Forms.Avalonia.Rendering.VBFormView.yml) or [`VBControls.Create`](../api/RDCore.Forms.Avalonia.Controls.VBControls.yml), and start in that file system's current directory. Without one, they read the machine's file system, from the process's current directory.

```csharp
using System.IO.Abstractions;

var window = new VBFormWindow(form, new FileSystem());
```

The behaviour below was measured with VB6, except where a row says it is RDForms' own.

## DriveListBox

[`VBDriveListBox`](../api/RDCore.Forms.Avalonia.Controls.VBDriveListBox.yml), a drop-down list. It raises Change, GotFocus, LostFocus, KeyDown, KeyPress and KeyUp.

|Member|Behaviour|Error|
|---|---|---|
|Items|One per drive: its letter in lower case and a colon, then its volume label in brackets, such as `c: [Windows]`. A drive without a label shows its letter alone, and a drive without a letter, as on Linux and macOS, shows its root's path, such as `/` (RDForms' own).||
|`Drive`|The chosen drive, as it shows; at first, the drive of the current directory. It is set to a letter, alone or followed by a colon and anything, such as `c`, `c:` or `C:\Windows`; a drive without a letter is set to a path on it (RDForms' own). The same drive again raises nothing. Another drive raises Change (RDForms' own).|68 for no drive of the list.|
|`ListIndex`, `ListCount`, `GetList(index)`|The chosen drive's index, the number of drives, and a drive as it shows (RDForms' own).|380 for a `ListIndex` outside the drives.|
|`Refresh()`|Reads the drives again, keeping the chosen one.||

## DirListBox

[`VBDirListBox`](../api/RDCore.Forms.Avalonia.Controls.VBDirListBox.yml). It raises Change, Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp, and no DblClick.

|Member|Behaviour|Error|
|---|---|---|
|Items|The folders from the root down to `Path`, then the folders `Path` holds, as their names are on disk, sorted without regard to case. A hidden folder is left out.||
|`GetList(index)`|A folder's full path: 0 to `ListCount` - 1 for the folders `Path` holds, -1 for `Path` itself, -2 for the folder above it, and so on up to the root. "" for an index of no folder (RDForms' own).||
|`Path`|The folder shown, without a separator at its end, in the case it was given; the folders above it keep that case. A relative path is taken from the list's own `Path`, so `..` goes up one folder. Setting another folder makes `ListIndex` -1 and raises Change; the same folder again raises nothing.|76 for a folder that does not exist.|
|`ListIndex`|The selected folder, -1 for `Path`. Setting it raises Click and leaves `Path` as it is.|380 for no folder of the list (RDForms' own).|
|`Refresh()`|Reads the folders `Path` holds again.||

A press, a key or a typed character selects a folder and raises Click as on a [ListBox](lists.md#listbox), and a double click on a folder makes it the `Path`, which raises Change, with no Click for the second release (RDForms' own).

`Dir1.Path = Drive1.Drive` and `File1.Path = Drive1.Drive` go to the file system's current directory where it is on that drive, and to the drive's root where it is not (RDForms' own).

## FileListBox

[`VBFileListBox`](../api/RDCore.Forms.Avalonia.Controls.VBFileListBox.yml). It raises Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, PathChange and PatternChange.

|Member|Behaviour|Error|
|---|---|---|
|Items|The names of the files in `Path` that match `Pattern`, as they are on disk, sorted without regard to case. A hidden file is listed only while `Hidden` is True, and a system file only while `System` is True. `Archive`, `Normal` and `ReadOnly` leave the list as it is.||
|`Pattern`|One or more patterns separated by semicolons, such as `*.txt;*.bas`; a file that matches any of them is listed. `*` stands for any characters and `?` for one, without regard to case; `*` and `*.*` also match a name with no extension. Setting it raises PatternChange. Setting the same pattern again raises nothing (RDForms' own).||
|`Path`|The folder, read without a separator at its end. Setting another folder lists its files, selects none, and raises PathChange. A relative path is taken from the list's own `Path`, and the same folder again raises nothing (RDForms' own).|76 for a folder that does not exist.|
|`FileName`|The selected file's name, or "". A file's name becomes the `Pattern`, which lists that file alone, and selects it; it reads back as the name is on disk. A pattern, with `*` or `?`, becomes the `Pattern` and selects nothing. A name with a folder sets `Path` first, which raises PathChange, then PatternChange. No Click.|53 for a file that does not exist; 76 for a folder that does not exist.|
|`ListIndex`|The selected file, or -1 for none. Setting it raises Click when it moves the selection.|380 for a value outside -1 to `ListCount` - 1.|
|`ListCount`, `GetList(index)`|The number of files, and a file's name. `GetList` gives "" outside the list (RDForms' own).||
|`Refresh()`|Reads the folder again. A file made or removed on disk shows only then, or when `Path`, `Pattern`, `Hidden` or `System` changes.||

A press, a key or a typed character selects a file and raises Click as on a [ListBox](lists.md#listbox) (RDForms' own).

> [!NOTE]
> **Not implemented.** A FileListBox selects one file at a time: its `MultiSelect` and `Selected` are not applied.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---
