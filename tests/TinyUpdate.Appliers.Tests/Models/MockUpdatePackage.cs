using System.Collections.Immutable;
using System.IO.Abstractions;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.UpdatePackage;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Appliers.Tests.Models;

public class MockUpdatePackage(string baseLocation, IFileSystem mockFileSystem) : IUpdatePackage
{
    public string Extension { get; }
    public Task<LoadResult> Load(Stream updatePackageStream, ReleaseEntry releaseEntry) => throw new NotImplementedException();

    public required ReleaseEntry ReleaseEntry { get; init; }
    public IReadOnlyCollection<FileEntry> DeltaFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> UnchangedFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> NewFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> MovedFiles { get; init; } = [];
    public IReadOnlyCollection<string> Directories => this.GetAllEntries().Select(x => x.Path!).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToImmutableList();
    public long FileCount => DeltaFiles.Count + UnchangedFiles.Count + NewFiles.Count + MovedFiles.Count;

    public void Setup()
    {
        if (mockFileSystem.Directory.Exists(baseLocation))
        {
            mockFileSystem.Directory.Delete(baseLocation, true);
        }
        
        foreach (MockFileEntry fileEntry in this.GetAllEntries())
        {
            fileEntry.Setup();
        }
    }
}