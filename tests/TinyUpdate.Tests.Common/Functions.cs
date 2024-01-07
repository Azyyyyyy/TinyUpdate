using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace TinyUpdate.Tests.Common;

//TODO: Add Tests for SHA256
public static class Functions
{
    public static void FillStreamWithRandomData(Stream stream, long filesize = -1)
    {
        if (filesize < 0)
        {
            filesize = Random.Shared.Next(1024 * 1000);
        }
        var buffer = new byte[filesize];
        Random.Shared.NextBytes(buffer);

        stream.Write(buffer);
    }

    public static IFileSystem SetupMockFileSystem()
    {
        //return new FileSystem();
        var fileSystem = new MockFileSystem(new MockFileSystemOptions
        {
            CurrentDirectory = Environment.CurrentDirectory,
            CreateDefaultTempDir = false
        });

        //We could have empty directories, want to add them just to be safe
        var directories = Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories);
        foreach (var directory in directories)
        {
            fileSystem.AddDirectory(directory);
        }
        
        var files = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            fileSystem.AddFile(file, new MockFileData(File.ReadAllBytes(file)));
        }

        return fileSystem;
    }
}