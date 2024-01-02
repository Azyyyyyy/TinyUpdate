namespace TinyUpdate.Core.Abstract;

public interface IUpdatePackage : IExtension
{
    Task Load(Stream updatePackageStream);
    
    /// <summary>
    /// Files that need to be processed as a delta file
    /// </summary>
    ICollection<FileEntry> DeltaFiles { get; }

    /// <summary>
    /// Files that should already be on the device
    /// </summary>
    ICollection<FileEntry> UnchangedFiles { get; }

    /// <summary>
    /// Files that aren't in the last update 
    /// </summary>
    ICollection<FileEntry> NewFiles { get; }
    
    /// <summary>
    /// Files that are unchanged but moved 
    /// </summary>
    ICollection<FileEntry> MovedFiles { get; }
}