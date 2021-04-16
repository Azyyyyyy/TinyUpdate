using System;

namespace TinyUpdate.Binary.MsDelta
{
    [Flags]
    internal enum FileTypeSet : long
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
        CLI4_I386 = 16,
        
        /// <summary>File type for AMD64 Portable Executable files with CLI4 transform</summary>
        CLI4_AMD64 = 32,
        
        /// <summary>File type for ARM Portable Executable files with CLI4 transform</summary>
        CLI4_ARM = 64,
        
        /// <summary>File type for ARM64 Portable Executable files with CLI4 transform</summary>
        CLI4_ARM64 = 128,
        
        /// <summary>File type set that distinguishes I386, IA64 and AMD64 Portable Executable file and treats others as raw</summary>
        Executables = Raw + I386 | IA64 | AMD64,

        /// <summary>File type set that distinguishes I386, IA64, ARM, and AMD64 Portable Executable file and treats others as raw</summary>
        Executables2 = Raw + CLI4_I386 | IA64 | CLI4_AMD64 | CLI4_ARM,
        
        /// <summary>File type set that distinguishes I386, IA64, ARM, ARM64, and AMD64 Portable Executable file and treats others as raw</summary>
        Executables3 = Raw + CLI4_I386 | IA64 | CLI4_AMD64 | CLI4_ARM | CLI4_ARM64,
        
        /// <summary>Uses the latest executable flag</summary>
        ExecutablesLatest  = Executables3
    }
}