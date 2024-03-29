﻿using System;
using System.IO;
using TinyUpdate.Core.Helper;

namespace TinyUpdate.Core.Temporary;

public class TemporaryFile : IDisposable
{
    public TemporaryFile(string fileLocation)
    {
        Location = fileLocation;
        File.Delete(fileLocation);
    }

    public string Location { get; }

    private FileStream? _fileStream;
    public FileStream GetStream(FileMode fileMode = FileMode.CreateNew, long preallocationSize = 0)
    {
        if (_fileStream?.SafeFileHandle?.IsClosed ?? false)
        {
            _fileStream = null;
        }
        _fileStream ??= FileHelper.MakeFileStream(Location, fileMode, FileAccess.ReadWrite, preallocationSize);
        return _fileStream;
    }

    private StreamWriter? _streamWriter;
    public StreamWriter GetTextStream()
    {
        if (!_streamWriter?.BaseStream.CanWrite ?? false)
        {
            _streamWriter = null;
        }
        _streamWriter ??= File.CreateText(Location);
        return _streamWriter;
    }
        
    public override string ToString() => Location;
        
    private bool _disposed = false;
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _fileStream?.Dispose();
        _streamWriter?.Dispose();
        File.Delete(Location);
    }
}