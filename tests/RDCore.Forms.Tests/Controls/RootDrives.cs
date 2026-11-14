using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RDCore.Forms.Tests.Controls;

// A mock file system whose drives are those .NET reports on Linux and macOS: here the one root, "/", with no letter and no label.
// TestableIO's mock names its drives as Windows does, a letter and a colon, on every OS, and its drive off Windows cannot be read.
internal sealed class RootDrives(MockFileSystem files) : IFileSystem
{
    public IFile File => files.File;
    public IDirectory Directory => files.Directory;
    public IFileInfoFactory FileInfo => files.FileInfo;
    public IFileStreamFactory FileStream => files.FileStream;
    public IPath Path => files.Path;
    public IDirectoryInfoFactory DirectoryInfo => files.DirectoryInfo;
    public IDriveInfoFactory DriveInfo => new Drives(this);
    public IFileSystemWatcherFactory FileSystemWatcher => files.FileSystemWatcher;
    public IFileVersionInfoFactory FileVersionInfo => files.FileVersionInfo;

    private sealed class Drives(RootDrives fileSystem) : IDriveInfoFactory
    {
        public IFileSystem FileSystem => fileSystem;

        public IDriveInfo[] GetDrives() => [new Root(fileSystem)];

        public IDriveInfo New(string driveName) =>
            driveName == "/" ? new Root(fileSystem) : throw new ArgumentException($"No drive {driveName}.", nameof(driveName));

        public IDriveInfo? Wrap(DriveInfo? driveInfo) => throw new NotSupportedException();
    }

    private sealed class Root(RootDrives fileSystem) : IDriveInfo
    {
        public IFileSystem FileSystem => fileSystem;
        public long AvailableFreeSpace => 0;
        public string DriveFormat => "";
        public DriveType DriveType => DriveType.Fixed;
        public bool IsReady => true;
        public string Name => "/";
        public IDirectoryInfo RootDirectory => fileSystem.DirectoryInfo.New("/");
        public long TotalFreeSpace => 0;
        public long TotalSize => 0;

        // Oblivious, as the interface's getter and setter differ in what they take for null.
#nullable disable annotations
        public string VolumeLabel
        {
            get => "";
            set => throw new NotSupportedException();
        }
#nullable restore annotations
    }
}
