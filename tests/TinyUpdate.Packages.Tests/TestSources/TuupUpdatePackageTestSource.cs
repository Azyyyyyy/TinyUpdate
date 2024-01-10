using SemVersion;
using TinyUpdate.Packages.Tests.Model;

namespace TinyUpdate.Packages.Tests.TestSources;

public static class TuupUpdatePackageTestSource
{
    public static IEnumerable<FullUpdatePackageTestData> GetFullTests()
    {
        yield return new FullUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithNewFileInRoot",
            Version = new SemanticVersion(2, 0, 0),
            ApplicationName = "new-application",
            SourceFolder = "NewRootFiles",
            ExpectedFilename = "newFileRoot"
        };
        
        yield return new FullUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithNewFileInSubDir",
            Version = new SemanticVersion(2, 0, 0),
            ApplicationName = "new-application",
            SourceFolder = "NewSubdirFiles",
            ExpectedFilename = "newFileSubdir"
        };
    }

    public static IEnumerable<DeltaUpdatePackageTestData> GetDeltaTests()
    {
        //NewFileTests
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithNewFileInRoot",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "EmptyFolder",
            TargetFolder = "NewRootFiles",
            ExpectedFilename = "newFileRoot"
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithNewFileInSubdir",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "EmptyFolder",
            TargetFolder = "NewSubdirFiles",
            ExpectedFilename = "newFileSubdir"
        };
        
        //UnchangedFileTests
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithUnchangedFileInRoot",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewRootFiles",
            TargetFolder = "NewRootFiles",
            ExpectedFilename = "unchangedFileRoot"
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithUnchangedFileInSubDir",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewSubdirFiles",
            TargetFolder = "NewSubdirFiles",
            ExpectedFilename = "unchangedFileSubdir",
        };

        //DeltaFileTests
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithDeltaFileInRoot",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewRootFiles",
            TargetFolder = "DeltaRootFiles",
            ExpectedFilename = "deltaFileRoot",
            NeedsFixedCreatorSize = true
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithDeltaFileInSubDir",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewSubdirFiles",
            TargetFolder = "DeltaSubdirFiles",
            ExpectedFilename = "deltaFileSubdir",
            NeedsFixedCreatorSize = true
        };
        
        //MovedFileTests
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithMovedFileInRoot",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewRootFiles",
            TargetFolder = "MovedRootFiles",
            ExpectedFilename = "movedFileRoot"
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithMovedFileRootToSubDir",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewRootFiles",
            TargetFolder = "MovedRootToSubdirFiles",
            ExpectedFilename = "movedFileRootToSubdir"
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithMovedFileSubDirToRoot",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewSubdirFiles",
            TargetFolder = "MovedSubdirToRootFiles",
            ExpectedFilename = "movedFileSubdirToRoot"
        };
        yield return new DeltaUpdatePackageTestData
        {
            Name = "CreateUpdatePackageWithMovedFileSubDirToSubDir",
            NewVersion = new SemanticVersion(1, 0, 1),
            ApplicationName = "new-application",
            SourceFolder = "NewSubdirFiles",
            TargetFolder = "MovedSubdirToSubdirFiles",
            ExpectedFilename = "movedFileSubdirToSubdir"
        };
    }
}