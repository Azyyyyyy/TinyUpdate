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
        
        yield return new ApplierTestData("DeltaFileRoot", deltaRootUpdatePackage, applicationPath);
        yield return new ApplierTestData("DeltaFileSubDir", deltaSubDirUpdatePackage, applicationPath);
    }
    
    private static FileEntry MakeFileEntry(string location, string sourceVersionPath, bool createInitialFile = true)
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
            Stream = targetFileStream
        };
        targetFileStream.Seek(0, SeekOrigin.Begin);

        return targetFileEntry;

        void Setup()
        {
            FileSystem.Directory.CreateDirectory(Path.Combine(sourceVersionPath, dir));

            var sourceFileStream = FileSystem.File.Create(Path.Combine(sourceVersionPath, location));
            Functions.FillStreamWithRandomData(sourceFileStream);
            sourceFileStream.Dispose();
        }
    }
}