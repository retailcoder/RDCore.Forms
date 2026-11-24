using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Avalonia;
using Avalonia.Input;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The folder of the VB6 measurements, its files and their attributes, on a file system of the test's own.
[TestClass]
public sealed class FileListControlTests
{
    private static readonly string Root = OperatingSystem.IsWindows() ? @"C:\tree" : "/tree";

    private static string In(params string[] parts) => Path.Combine([Root, .. parts]);

    // The tree, on a file system whose drives are as this OS's own are named.
    private static IFileSystem Tree() => OnThisOs(Mock());

    private static IFileSystem OnThisOs(MockFileSystem files) => OperatingSystem.IsWindows() ? files : new RootDrives(files);

    private static MockFileSystem Mock() => new(new Dictionary<string, MockFileData>
    {
        [In("b.bas")] = new(""),
        [In("a.txt")] = new(""),
        [In("C.TXT")] = new(""),
        [In("Zeta.Txt")] = new(""),
        [In("noext")] = new(""),
        [In("hid.txt")] = new("") { Attributes = FileAttributes.Hidden },
        [In("sys.txt")] = new("") { Attributes = FileAttributes.System },
        [In("ro.txt")] = new("") { Attributes = FileAttributes.ReadOnly },
        [In("SubOne", "x.bas")] = new(""),
        [In("SubOne", "deep")] = new MockDirectoryData(),
        [In("sub2")] = new MockDirectoryData(),
        [In("HiddenDir")] = new MockDirectoryData { Attributes = FileAttributes.Directory | FileAttributes.Hidden },
    }, Root);

    private static readonly string DriveRoot = Path.GetPathRoot(Root)!;

    private static ComponentInstance File() => new(ComponentClasses.FileListBox, "File1");

    private static string[] Items(RDFormsFileListBox list) => [.. Enumerable.Range(0, list.ListCount).Select(list.GetList)];

    [TestMethod]
    public void AFileList_ListsTheCurrentDirectorysFiles_SortedIgnoringCase_WithoutHiddenOrSystemFiles()
    {
        var (path, items, fileName, index) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            return (list.Path, Items(list), list.FileName, list.ListIndex);
        });

        Assert.AreEqual(Root, path);
        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "noext", "ro.txt", "Zeta.Txt" }, items);
        Assert.AreEqual("", fileName);
        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    [DataRow("*.txt", "a.txt|C.TXT|ro.txt|Zeta.Txt")]
    [DataRow("*.txt;*.bas", "a.txt|b.bas|C.TXT|ro.txt|Zeta.Txt")]
    [DataRow("*.*", "a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt")]
    [DataRow("*", "a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt")]
    [DataRow("?.txt", "a.txt|C.TXT")]
    [DataRow("NOEXT", "noext")]
    [DataRow("z*", "Zeta.Txt")]
    public void APattern_ListsTheFilesThatMatchAnyOfItsParts_WithoutRegardToCase(string pattern, string expected)
    {
        var items = UI.Run(() => Items(new RDFormsFileListBox(File().Set(P.Pattern, pattern), Tree())));

        Assert.AreEqual(expected, string.Join("|", items));
    }

    [TestMethod]
    public void SettingThePattern_ListsItsFiles_AndRaisesPatternChange_OnlyWhereItChanges()
    {
        var (items, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.Pattern = "*.bas";
            list.Pattern = "*.bas";
            window.Close();
            return (Items(list), sink.Raised);
        });

        Expect.Lines(new[] { "b.bas" }, items);
        Expect.Lines(new[] { "File1.PatternChange" }, raised);
    }

    [TestMethod]
    public void HiddenAndSystem_ListTheirFiles_WhileTrue()
    {
        var (shown, hidden) = UI.Run(() =>
        {
            var instance = File();
            var list = new RDFormsFileListBox(instance, Tree());
            var (window, _) = ControlHost.Show(list);
            instance.Set(P.Hidden, true).Set(P.SystemFiles, true);
            var shown = string.Join("|", Items(list));
            instance.Set(P.Hidden, false).Set(P.SystemFiles, false);
            window.Close();
            return (shown, string.Join("|", Items(list)));
        });

        Assert.AreEqual("a.txt|b.bas|C.TXT|hid.txt|noext|ro.txt|sys.txt|Zeta.Txt", shown);
        Assert.AreEqual("a.txt|b.bas|C.TXT|noext|ro.txt|Zeta.Txt", hidden);
    }

    [TestMethod]
    public void ReadOnlyArchiveAndNormal_LeaveTheListAsItIs()
    {
        var items = UI.Run(() =>
        {
            var instance = File();
            var list = new RDFormsFileListBox(instance, Tree());
            var (window, _) = ControlHost.Show(list);
            instance.Set(P.ReadOnly, false).Set(P.Archive, false).Set(P.Normal, false);
            window.Close();
            return Items(list);
        });

        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "noext", "ro.txt", "Zeta.Txt" }, items);
    }

    [TestMethod]
    public void TheListIndex_SelectsAFile_AndRaisesClick_AndOutsideTheFilesIs381()
    {
        var (fileName, raised, past, below) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 1;
            list.ListIndex = 1;
            var past = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = 6).Error.Number;
            var below = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = -2).Error.Number;
            window.Close();
            return (list.FileName, sink.Raised, past, below);
        });

        Assert.AreEqual("b.bas", fileName);
        Expect.Lines(new[] { "File1.Click" }, raised);
        Assert.AreEqual(381, past);
        Assert.AreEqual(381, below);
    }

    [TestMethod]
    public void AFileName_BecomesThePattern_SelectsTheFile_AsItsNameIsOnDisk_AndRaisesDblClick_OnlyWhereItChanges()
    {
        var (pattern, fileName, index, items, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.FileName = "c.txt";
            list.FileName = "c.txt";
            window.Close();
            return (list.Pattern, list.FileName, list.ListIndex, Items(list), sink.Raised);
        });

        Assert.AreEqual("c.txt", pattern);
        Assert.AreEqual("C.TXT", fileName);
        Assert.AreEqual(0, index);
        Expect.Lines(new[] { "C.TXT" }, items);
        Expect.Lines(new[] { "File1.PatternChange", "File1.DblClick(Boolean False)" }, raised);
    }

    [TestMethod]
    public void AFileNameOfNoFile_Is53_AndChangesNothing()
    {
        var (error, pattern, count, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.FileName = "nothere.txt").Error.Number;
            window.Close();
            return (error, list.Pattern, list.ListCount, sink.Raised);
        });

        Assert.AreEqual(53, error);
        Assert.AreEqual("*.*", pattern);
        Assert.AreEqual(6, count);
        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void AFileNameThatIsAPattern_BecomesThePattern_AndSelectsNothing()
    {
        var (pattern, fileName, index, items) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            list.ListIndex = 0;
            list.FileName = "*.bas";
            return (list.Pattern, list.FileName, list.ListIndex, Items(list));
        });

        Assert.AreEqual("*.bas", pattern);
        Assert.AreEqual("", fileName);
        Assert.AreEqual(-1, index);
        Expect.Lines(new[] { "b.bas" }, items);
    }

    [TestMethod]
    public void AFileNameWithAFolder_SetsThePath_ThenThePattern()
    {
        var (path, fileName, index, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.FileName = In("SubOne", "x.bas");
            window.Close();
            return (list.Path, list.FileName, list.ListIndex, sink.Raised);
        });

        Assert.AreEqual(In("SubOne"), path);
        Assert.AreEqual("x.bas", fileName);
        Assert.AreEqual(0, index);
        Expect.Lines(new[] { "File1.PathChange", "File1.PatternChange", "File1.DblClick(Boolean False)" }, raised);
    }

    [TestMethod]
    public void ASingleSelectFileListsSelected_MovesTheSelection_AndClearingTheSelectedFileLeavesNone()
    {
        var (log, raised, read, set) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            var log = new List<string>();
            void Step(string name, Action act)
            {
                act();
                log.Add($"{name}: {sink.Raised.Count(e => e == "File1.Click")} {list.ListIndex} {list.FileName} {list.GetSelected(1)}");
                sink.Raised.Clear();
            }
            Step("Selected(1) = True", () => list.SetSelected(1, true));
            Step("Selected(3) = True", () => list.SetSelected(3, true));
            Step("Selected(1) = True", () => list.SetSelected(1, true));
            Step("Selected(3) = False", () => list.SetSelected(3, false));
            Step("Selected(1) = False", () => list.SetSelected(1, false));
            var read = Assert.ThrowsExactly<FormErrorException>(() => list.GetSelected(6)).Error.Number;
            var set = Assert.ThrowsExactly<FormErrorException>(() => list.SetSelected(-1, true)).Error.Number;
            window.Close();
            return (log, sink.Raised, read, set);
        });

        Expect.Lines(["Selected(1) = True: 1 1 b.bas True", "Selected(3) = True: 1 3 noext False", "Selected(1) = True: 1 1 b.bas True",
            "Selected(3) = False: 0 1 b.bas True", "Selected(1) = False: 1 -1  False"], log);
        Assert.IsEmpty(raised);
        Assert.AreEqual(381, read);
        Assert.AreEqual(381, set);
    }

    [TestMethod]
    [DataRow(ListBoxMultiSelect.Simple)]
    [DataRow(ListBoxMultiSelect.Extended)]
    public void AMultiSelectFileListsListIndex_IsTheFocusedFile_TheFirstEachTimeItLists_AndTakesOnlyMinus1AndItself(ListBoxMultiSelect mode)
    {
        var (log, error) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File().Set(P.MultiSelect, mode), Tree());
            var (window, sink) = ControlHost.Show(list);
            var log = new List<string>();
            void Step(string name, Action act)
            {
                act();
                var selected = Enumerable.Range(0, list.ListCount).Where(list.GetSelected);
                log.Add($"{name}: {string.Join(" ", sink.Raised.Select(e => e[6..]))} [{string.Join(",", selected)}] {list.ListIndex} {list.FileName}");
                sink.Raised.Clear();
            }
            Step("shown", () => { });
            Step("Selected(1) = True", () => list.SetSelected(1, true));
            Step("Selected(3) = True", () => list.SetSelected(3, true));
            Step("Selected(3) = True", () => list.SetSelected(3, true));
            Step("Selected(3) = False", () => list.SetSelected(3, false));
            Step("ListIndex = 3", () => list.ListIndex = 3);
            Step("ListIndex = -1", () => list.ListIndex = -1);
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = 2).Error.Number;
            Step("Refresh", list.Refresh);
            Step("Pattern = *.none", () => list.Pattern = "*.none");
            Step("ListIndex = 0", () => list.ListIndex = 0);
            Step("ListIndex = -1", () => list.ListIndex = -1);
            window.Close();
            return (log, error);
        });

        Expect.Lines(["shown:  [] 0 a.txt", "Selected(1) = True: Click [1] 1 b.bas", "Selected(3) = True: Click [1,3] 3 noext",
            "Selected(3) = True:  [1,3] 3 noext", "Selected(3) = False: Click [1] 3 noext", "ListIndex = 3:  [1] 3 noext",
            "ListIndex = -1: Click [1] 3 noext", "Refresh:  [] 0 a.txt", "Pattern = *.none: PatternChange [] 0 ",
            "ListIndex = 0:  [] 0 ", "ListIndex = -1: Click [] 0 "], log);
        Assert.AreEqual(381, error);
    }

    [TestMethod]
    public void AMultiSelectFileListsFileName_GivesTheFileTheFocus_AndSelectsNothing()
    {
        var (index, fileName, selected, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File().Set(P.MultiSelect, ListBoxMultiSelect.Extended), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.SetSelected(2, true);
            sink.Raised.Clear();
            list.FileName = "noext";
            window.Close();
            return (list.ListIndex, list.FileName, list.GetSelected(0), sink.Raised);
        });

        Assert.AreEqual(0, index);
        Assert.AreEqual("noext", fileName);
        Assert.IsFalse(selected);
        Expect.Lines(new[] { "File1.PatternChange", "File1.DblClick(Boolean False)" }, raised);
    }

    [TestMethod]
    public void APath_IsReadWithoutItsFinalSeparator_AndRaisesPathChange_OnlyWhereItChanges()
    {
        var (path, fileName, raised) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 0;
            list.Path = In("SubOne") + Path.DirectorySeparatorChar;
            list.Path = In("SubOne");
            window.Close();
            return (list.Path, list.FileName, sink.Raised);
        });

        Assert.AreEqual(In("SubOne"), path);
        Assert.AreEqual("", fileName);
        Expect.Lines(new[] { "File1.Click", "File1.PathChange" }, raised);
    }

    [TestMethod]
    public void ARelativePath_IsTakenFromTheListsOwnPath()
    {
        var (down, up) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            list.Path = "SubOne";
            var down = list.Path;
            list.Path = "..";
            return (down, list.Path);
        });

        Assert.AreEqual(In("SubOne"), down);
        Assert.AreEqual(Root, up);
    }

    [TestMethod]
    public void APathThatDoesNotExist_Is76_AndChangesNothing()
    {
        var (error, fileNameError, path, count) = UI.Run(() =>
        {
            var list = new RDFormsFileListBox(File(), Tree());
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.Path = In("nothere")).Error.Number;
            var fileNameError = Assert.ThrowsExactly<FormErrorException>(() => list.FileName = In("nothere", "a.txt")).Error.Number;
            return (error, fileNameError, list.Path, list.ListCount);
        });

        Assert.AreEqual(76, error);
        Assert.AreEqual(76, fileNameError);
        Assert.AreEqual(Root, path);
        Assert.AreEqual(6, count);
    }

    [TestMethod]
    public void AFileMadeOnDisk_IsListedOnlyAfterARefresh()
    {
        var (before, after) = UI.Run(() =>
        {
            var files = Mock();
            var list = new RDFormsFileListBox(File(), OnThisOs(files));
            files.AddFile(In("new.txt"), new MockFileData(""));
            var before = list.ListCount;
            list.Refresh();
            return (before, Items(list));
        });

        Assert.AreEqual(6, before);
        Expect.Lines(new[] { "a.txt", "b.bas", "C.TXT", "new.txt", "noext", "ro.txt", "Zeta.Txt" }, after);
    }

    [TestMethod]
    public void AFormsFileLists_ShowTheFileSystemItsViewIsGiven()
    {
        var path = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1");
            var file = new ComponentInstance(ComponentClasses.FileListBox, "File1");
            file.SetContainer(root);
            var view = new RDFormsFormView(new FormDefinition(root), Tree());
            return ((RDFormsFileListBox)view.ControlOf(file)!).Path;
        });

        Assert.AreEqual(Root, path);
    }

    private static ComponentInstance Dir() => new(ComponentClasses.DirListBox, "Dir1");

    // The folders from -n, the root, to the last one Path holds.
    private static string[] Folders(RDFormsDirListBox list)
    {
        var top = -1;
        while (list.GetList(top - 1) != "")
            top--;
        return [.. Enumerable.Range(top, list.ListCount - top).Select(list.GetList)];
    }

    [TestMethod]
    public void ADirList_ShowsThePathUnderTheFoldersAboveIt_ThenTheFoldersItHolds_SortedIgnoringCase_WithoutHiddenOnes()
    {
        var (path, index, count, folders) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            return (list.Path, list.ListIndex, list.ListCount, Folders(list));
        });

        Assert.AreEqual(Root, path);
        Assert.AreEqual(-1, index);
        Assert.AreEqual(2, count);
        Expect.Lines(new[] { DriveRoot, Root, In("sub2"), In("SubOne") }, folders);
    }

    [TestMethod]
    public void ADirListsPath_RaisesChange_OnlyWhereItChanges()
    {
        var (path, above, count, index, raised) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 0;
            list.Path = In("SubOne", "deep");
            list.Path = In("SubOne", "deep");
            window.Close();
            return (list.Path, list.GetList(-2), list.ListCount, list.ListIndex, sink.Raised);
        });

        Assert.AreEqual(In("SubOne", "deep"), path);
        Assert.AreEqual(In("SubOne"), above);
        Assert.AreEqual(0, count);
        Assert.AreEqual(-1, index);
        Expect.Lines(new[] { "Dir1.Click", "Dir1.Change" }, raised);
    }

    // A file system that ignores case, as Windows does, finds a folder named in another case; the list keeps the case given.
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void ADirListsPath_KeepsTheCaseItIsGiven_WhereTheFileSystemIgnoresCase()
    {
        var (path, above) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            list.Path = In("subone", "deep");
            return (list.Path, list.GetList(-2));
        });

        Assert.AreEqual(In("subone", "deep"), path);
        Assert.AreEqual(In("subone"), above);
    }

    // A file system that compares case, as Linux's does, has no folder of that name in another case.
    [TestMethod]
    [OSCondition(OperatingSystems.Linux)]
    public void ADirListsPathInAnotherCase_Is76_WhereTheFileSystemComparesCase()
    {
        var (error, path) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            return (Assert.ThrowsExactly<FormErrorException>(() => list.Path = In("subone", "deep")).Error.Number, list.Path);
        });

        Assert.AreEqual(76, error);
        Assert.AreEqual(Root, path);
    }

    [TestMethod]
    public void ADirListsRelativePath_IsTakenFromItsOwnPath_AndAMissingFolderIs76()
    {
        var (path, error) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            list.Path = list.GetList(1);
            list.Path = "..";
            return (list.Path, Assert.ThrowsExactly<FormErrorException>(() => list.Path = "nothere").Error.Number);
        });

        Assert.AreEqual(Root, path);
        Assert.AreEqual(76, error);
    }

    [TestMethod]
    public void ADirListsListIndex_SelectsAFolderAboveOrBelow_AndRaisesClick_WithoutChangingThePath()
    {
        var (path, index, raised, below, past) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            var (window, sink) = ControlHost.Show(list);
            list.ListIndex = 0;
            list.ListIndex = -2;
            var below = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = -3).Error.Number;
            var past = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = 2).Error.Number;
            window.Close();
            return (list.Path, list.ListIndex, sink.Raised, below, past);
        });

        Assert.AreEqual(Root, path);
        Assert.AreEqual(-2, index);
        Expect.Lines(new[] { "Dir1.Click", "Dir1.Click" }, raised);
        Assert.AreEqual(381, below);
        Assert.AreEqual(381, past);
    }

    [TestMethod]
    public void ADoubleClickOnAFolder_MakesItThePath()
    {
        var (path, raised) = UI.Run(() =>
        {
            var list = new RDFormsDirListBox(Dir(), Tree());
            var (window, sink) = ControlHost.Show((list, new Rect(10, 5, 160, 120)));
            var item = list.ContainerFromIndex(3)!;
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
            var point = item.TranslatePoint(new Point(4, 4), window)!.Value;
            foreach (var clickCount in new[] { 1, 2 })
            {
                item.RaiseEvent(new PointerPressedEventArgs(item, pointer, window, point, 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, clickCount));
                item.RaiseEvent(new PointerReleasedEventArgs(item, pointer, window, point, 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            }
            window.Close();
            return (list.Path, sink.WithoutMoves.Where(e => e is "Dir1.Change" or "Dir1.Click").ToArray());
        });

        Assert.AreEqual(In("SubOne"), path);
        Expect.Lines(new[] { "Dir1.Click", "Dir1.Change" }, raised);
    }

    [TestMethod]
    public void ADirListsPathAndAFileListsPath_TakeADriveAsTheDriveListReadsIt_AsTheCurrentDirectoryOnIt()
    {
        var (dir, file) = UI.Run(() =>
        {
            var files = Tree();
            var drive = new RDFormsDriveListBox(new ComponentInstance(ComponentClasses.DriveListBox, "Drive1"), files);
            var dirList = new RDFormsDirListBox(Dir(), files);
            var fileList = new RDFormsFileListBox(File(), files);
            dirList.Path = In("SubOne");
            fileList.Path = In("SubOne");
            dirList.Path = drive.Drive;
            fileList.Path = drive.Drive;
            return (dirList.Path, fileList.Path);
        });

        // A drive without a letter reads as its root's path, which is a path like any other.
        Assert.AreEqual(OperatingSystem.IsWindows() ? Root : DriveRoot, dir);
        Assert.AreEqual(OperatingSystem.IsWindows() ? Root : DriveRoot, file);
    }

    private static ComponentInstance Drive() => new(ComponentClasses.DriveListBox, "Drive1");

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void ADriveList_ShowsEachDrivesLetterAndLabel_AndChoosesTheCurrentDirectorysDrive()
    {
        var (items, drive, index) = UI.Run(() =>
        {
            var files = Mock();
            files.AddDrive(@"D:\", new MockDriveData { VolumeLabel = "Data" });
            files.AddDrive(@"B:\", new MockDriveData { VolumeLabel = "" });
            var list = new RDFormsDriveListBox(Drive(), files);
            return (Enumerable.Range(0, list.ListCount).Select(list.GetList).ToArray(), list.Drive, list.ListIndex);
        });

        CollectionAssert.Contains(items, "d: [Data]");
        CollectionAssert.Contains(items, "b:");
        Assert.AreEqual("c:", drive[..2]);
        Assert.AreEqual(drive, items[index]);
    }

    [TestMethod]
    public void ADriveListsListIndexPastTheDrives_Is381_AndChangesNothing()
    {
        var (error, index, raised) = UI.Run(() =>
        {
            var list = new RDFormsDriveListBox(Drive(), Tree());
            var (window, sink) = ControlHost.Show(list);
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.ListIndex = list.ListCount).Error.Number;
            window.Close();
            return (error, list.ListIndex, sink.Raised);
        });

        Assert.AreEqual(381, error);
        Assert.AreEqual(0, index);
        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void ADriveListsListIndexOfMinus1_ChoosesNoDrive_WithChangeOnce_AndRefreshChoosesTheFirstWithChange()
    {
        var log = UI.Run(() =>
        {
            var list = new RDFormsDriveListBox(Drive(), Tree());
            var (window, sink) = ControlHost.Show(list);
            var first = list.GetList(0);
            var log = new List<string>();
            void Step(string name, Action act)
            {
                sink.Raised.Clear();
                var error = 0;
                try
                {
                    act();
                }
                catch (FormErrorException e)
                {
                    error = e.Error.Number;
                }
                var drive = list.Drive == "" ? "none" : list.Drive == first ? "first" : list.Drive;
                log.Add($"{name}: {error} {string.Join(" ", sink.Raised)} {list.ListIndex} {drive}");
            }
            Step("ListIndex = -1", () => list.ListIndex = -1);
            Step("ListIndex = -1", () => list.ListIndex = -1);
            Step("ListIndex = -2", () => list.ListIndex = -2);
            Step("Drive = \"\"", () => list.Drive = "");
            Step("Refresh", list.Refresh);
            Step("Refresh", list.Refresh);
            Step("Drive = \"\"", () => list.Drive = "");
            window.Close();
            return log;
        });

        Expect.Lines(new[]
        {
            "ListIndex = -1: 0 Drive1.Change -1 none", "ListIndex = -1: 0  -1 none", "ListIndex = -2: 381  -1 none", "Drive = \"\": 380  -1 none",
            "Refresh: 0 Drive1.Change 0 first", "Refresh: 0  0 first", "Drive = \"\": 380  0 first",
        }, log);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void ADriveListsDrive_TakesALetterOrAPathOnTheDrive_AndRaisesChange_OnlyForAnotherDrive()
    {
        var (drive, raised, error) = UI.Run(() =>
        {
            var files = Mock();
            files.AddDrive(@"D:\", new MockDriveData { VolumeLabel = "Data" });
            var list = new RDFormsDriveListBox(Drive(), files);
            var (window, sink) = ControlHost.Show(list);
            list.Drive = "c";
            list.Drive = @"C:\Windows";
            list.Drive = @"d:\x";
            list.Drive = "d: [Data]";
            var error = Assert.ThrowsExactly<FormErrorException>(() => list.Drive = "z:").Error.Number;
            window.Close();
            return (list.Drive, sink.Raised, error);
        });

        Assert.AreEqual("d: [Data]", drive);
        Expect.Lines(new[] { "Drive1.Change" }, raised);
        Assert.AreEqual(68, error);
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Linux | OperatingSystems.OSX)]
    public void ADriveWithoutALetter_ShowsAsItsRootsPath_AndIsChosenByAPathOnIt()
    {
        var (items, drive) = UI.Run(() =>
        {
            var list = new RDFormsDriveListBox(Drive(), Tree());
            list.Drive = In("SubOne");
            return (Enumerable.Range(0, list.ListCount).Select(list.GetList).ToArray(), list.Drive);
        });

        CollectionAssert.Contains(items, "/");
        Assert.AreEqual("/", drive);
    }
}
