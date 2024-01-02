// ReSharper disable InconsistentNaming

namespace TinyUpdate.MSDelta.Enum;

[Flags]
internal enum FileType : long
{
    /// <summary>Raw file type</summary>
    Raw = 1,

    /// <summary>File type for I386 Portable Executable files</summary>
    I386 = 2,

    /// <summary>File type for for IA64 Portable Executable files</summary>
    IA64 = 4,

    /// <summary>File type for AMD64 Portable Executable files</summary>
    AMD64 = 8,

    /// <summary>File type for I386 Portable Executable files with CLI4 transform</summary>
    CLI4I386 = 16,

    /// <summary>File type for AMD64 Portable Executable files with CLI4 transform</summary>
    CLI4AMD64 = 32,

    /// <summary>File type for ARM Portable Executable files with CLI4 transform</summary>
    CLI4ARM = 64,

    /// <summary>File type for ARM64 Portable Executable files with CLI4 transform</summary>
    CLI4ARM64 = 128,

    /// <summary>File type set that distinguishes I386, IA64 and AMD64 Portable Executable file and treats others as raw</summary>
    Executables = (Raw + I386) | IA64 | AMD64,

    /// <summary>File type set that distinguishes I386, IA64, ARM, and AMD64 Portable Executable file and treats others as raw</summary>
    Executables2 = (Raw + CLI4I386) | IA64 | CLI4AMD64 | CLI4ARM,

    /// <summary>
    ///     File type set that distinguishes I386, IA64, ARM, ARM64, and AMD64 Portable Executable file and treats others
    ///     as raw
    /// </summary>
    Executables3 = (Raw + CLI4I386) | IA64 | CLI4AMD64 | CLI4ARM | CLI4ARM64,

    /// <summary>Uses the latest executable flag</summary>
    ExecutablesLatest = Executables3
}