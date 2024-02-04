using TinyUpdate.Core.Model;

namespace TinyUpdate.Appliers.Tests.Models;

public class MockFileEntry : FileEntry
{
    protected MockFileEntry(string filename, string path, string? previousLocation, string hash, long filesize, string extension, Action setup) : base(filename, path, previousLocation, hash, filesize, extension)
    {
        Setup = setup;
    }

    public MockFileEntry(string filename, string path, Action setup) : base(filename, path)
    {
        Setup = setup;
    }
    
    public Action Setup { get; }
}