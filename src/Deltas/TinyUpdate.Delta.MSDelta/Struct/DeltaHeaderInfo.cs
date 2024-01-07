using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TinyUpdate.Delta.MSDelta.Enum;

namespace TinyUpdate.Delta.MSDelta.Struct;

[StructLayout(LayoutKind.Sequential)]
internal struct DeltaHeaderInfo
{
    /** Used file type set. */
    public FileType FileTypeSet;

    /** Source file type. */
    public FileType FileType;

    /** Delta flags. */
    public FlagType Flags;

    /** Size of target file in bytes. */
    public IntPtr TargetSize;

    /** Time of target file. */
    public FILETIME TargetFileTime;

    /** Algorithm used for hashing. */
    public HashAlgId TargetHashAlgId;

    /** Target hash. */
    [MarshalAs(UnmanagedType.Struct)]
    public DeltaHash TargetHash;
}