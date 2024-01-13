using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TinyUpdate.Delta.MSDelta.Enum;

namespace TinyUpdate.Delta.MSDelta.Struct;

/// <summary>
/// Delta header information
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DeltaHeaderInfo
{
    /// <summary>
    /// Used file type set
    /// </summary>
    public FileType FileTypeSet;

    /// <summary>
    /// Source file type
    /// </summary>
    public FileType FileType;

    /// <summary>
    /// Delta flags
    /// </summary>
    public FlagType Flags;

    /// <summary>
    /// Size of target file in bytes
    /// </summary>
    public IntPtr TargetSize;

    /// <summary>
    /// Time of target file
    /// </summary>
    public FILETIME TargetFileTime;

    /// <summary>
    /// Algorithm used for hashing
    /// </summary>
    public HashAlgId TargetHashAlgId;

    /// <summary>
    /// Target hash
    /// </summary>
    [MarshalAs(UnmanagedType.Struct)]
    public DeltaHash TargetHash;
}