using System.Collections.Immutable;
using System.IO.Abstractions.TestingHelpers;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.UpdatePackage;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Appliers.Tests.Models;

public class MockUpdatePackage : IUpdatePackage
{
    private readonly string _baseLocation;
    private readonly MockFileSystem _mockFileSystem;

    public MockUpdatePackage(string baseLocation, MockFileSystem mockFileSystem)
    {
        _baseLocation = baseLocation;
        _mockFileSystem = mockFileSystem;
    }

    public string Extension { get; }
    public Task<LoadResult> Load(Stream updatePackageStream, ReleaseEntry releaseEntry) => throw new NotImplementedException();

    public required ReleaseEntry ReleaseEntry { get; init; }
    public IReadOnlyCollection<FileEntry> DeltaFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> UnchangedFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> NewFiles { get; init; } = [];
    public IReadOnlyCollection<FileEntry> MovedFiles { get; init; } = [];
    public IReadOnlyCollection<string> Directories => this.GetAllEntries().Select(x => x.Location).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToImmutableList();
    public long FileCount => DeltaFiles.Count + UnchangedFiles.Count + NewFiles.Count + MovedFiles.Count;

    public void Setup()
    {
        if (_mockFileSystem.Directory.Exists(_baseLocation))
        {
            _mockFileSystem.Directory.Delete(_baseLocation, true);
        }
        
        foreach (MockFileEntry fileEntry in this.GetAllEntries())
        {
            fileEntry.Setup();
        }
    }
}