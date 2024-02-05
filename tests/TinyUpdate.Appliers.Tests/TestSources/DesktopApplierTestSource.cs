using System.IO.Abstractions.TestingHelpers;
using TinyUpdate.Appliers.Tests.Models;
using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Model;
using TinyUpdate.Core.Tests;
using TinyUpdate.Desktop;

namespace TinyUpdate.Appliers.Tests.TestSources;

public class DesktopApplierTestSource
{
    private static readonly MockFileSystem FileSystem = Functions.SetupMockFileSystem();
    private static readonly IHasher Hasher = SHA256.Instance;
    
    public static IEnumerable<ApplierTestData> TestS()
    {
        var applicationPath = Path.Combine("Assets", nameof(DesktopApplier));
        var appV1 = Path.Combine(applicationPath, "1.0.0");
        
        //Delta update
        var deltaRootUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            DeltaFiles = [MakeFileEntry("testApplication.exe", appV1)]
        };
        var deltaSubDirUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            DeltaFiles = [MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1)]
        };
        
        var unchangedRootUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            UnchangedFiles = [MakeFileEntry("testApplication.exe", appV1, unchanged: true, createInitialFile: true)]
        };
        var unchangedSubDirUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            UnchangedFiles = [MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1, unchanged: true, createInitialFile: true)]
        };
        
        var newRootUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            NewFiles = [MakeFileEntry("testApplication.exe", appV1, false)]
        };
        var newSubDirUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            NewFiles = [MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1, false)]
        };
        
        var movedRootUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            MovedFiles = [MakeFileEntry("testApplication.exe", appV1, unchanged: true, lastLocation: "testing.exe")]
        };
        var movedSubDirUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            MovedFiles = [MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1, unchanged: true, lastLocation: Path.Combine("testsub", "testing.exe"))]
        };
        var movedSubDirToSubUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            MovedFiles = [MakeFileEntry(Path.Combine("newtestsub", "testApplication.exe"), appV1, unchanged: true, lastLocation: Path.Combine("testsub", "testing.exe"))]
        };
        var movedRootToSubUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            MovedFiles = [MakeFileEntry("testApplication.exe", appV1, unchanged: true, lastLocation: Path.Combine("testsub", "testing.exe"))]
        };
        var movedSubDirToRootUpdatePackage = new MockUpdatePackage(applicationPath, FileSystem)
        {
            ReleaseEntry = new MockReleaseEntry("1.0.0", "1.0.1", true),
            MovedFiles = [MakeFileEntry(Path.Combine("testsub", "testApplication.exe"), appV1, unchanged: true, lastLocation: "testing.exe")]
        };

        yield return new ApplierTestData("DeltaFileRoot", deltaRootUpdatePackage, applicationPath);
        yield return new ApplierTestData("DeltaFileSubDir", deltaSubDirUpdatePackage, applicationPath);
        yield return new ApplierTestData("UnchangedFileRoot", unchangedRootUpdatePackage, applicationPath);
        yield return new ApplierTestData("UnchangedFileSubDir", unchangedSubDirUpdatePackage, applicationPath);
        yield return new ApplierTestData("NewFileRoot", newRootUpdatePackage, applicationPath);
        yield return new ApplierTestData("NewFileSubDir", newSubDirUpdatePackage, applicationPath);
        yield return new ApplierTestData("MovedFileRoot", movedRootUpdatePackage, applicationPath);
        yield return new ApplierTestData("MovedFileSubDir", movedSubDirUpdatePackage, applicationPath);
        yield return new ApplierTestData("MovedFileSubDirToSubDir", movedSubDirToSubUpdatePackage, applicationPath);
        yield return new ApplierTestData("MovedFileRootToSubDir", movedRootToSubUpdatePackage, applicationPath);
        yield return new ApplierTestData("MovedFileSubDirToRoot", movedSubDirToRootUpdatePackage, applicationPath);
    }
    
    private static FileEntry MakeFileEntry(string location, string sourceVersionPath, bool createInitialFile = true, bool unchanged = false, string? lastLocation = null)
    {
        var dir = Path.GetDirectoryName(location) ?? "";
        var targetFileStream = new MemoryStream();

        Functions.FillStreamWithRandomData(targetFileStream, 1024 * 10);
        targetFileStream.Seek(0, SeekOrigin.Begin);

        var targetFileEntry = new MockFileEntry(Path.GetFileName(location), dir, Setup)
        {
            Hash = Hasher.HashData(targetFileStream),
            Filesize = targetFileStream.Length,
            Extension = ".diffing",
            Stream = unchanged ? null : targetFileStream,
            PreviousLocation = lastLocation
        };
        targetFileStream.Seek(0, SeekOrigin.Begin);

        return targetFileEntry;

        void Setup()
        {
            FileSystem.Directory.CreateDirectory(Path.Combine(sourceVersionPath, Path.GetDirectoryName(lastLocation) ?? dir));

            if (createInitialFile) {
                using var sourceFileStream = FileSystem.File.Create(Path.Combine(sourceVersionPath, lastLocation ?? location));
                if (!unchanged)
                {
                    Functions.FillStreamWithRandomData(sourceFileStream);
                }
                else
                {
                    targetFileStream.CopyToAsync(sourceFileStream);
                    targetFileStream.Seek(0, SeekOrigin.Begin);
                }
            }
        }
    }
}