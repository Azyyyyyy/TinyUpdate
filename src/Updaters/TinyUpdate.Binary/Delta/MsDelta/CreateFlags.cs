using System;

namespace TinyUpdate.Binary.Delta.MsDelta
{
    [Flags]
    internal enum CreateFlags : long
    {
        /// <summary>Indicates no special handling.</summary>
        None = 0,

        /// <summary>Transform E8 pieces (relative calls in x86) of target file</summary>
        E8 = 1,

        /// <summary>Mark non-executable parts of source PE</summary>
        Mark = 2,

        /// <summary>Transform imports of source PE</summary>
        Imports = 4,

        /// <summary>Transform exports of source PE</summary>
        Exports = 8,

        /// <summary>Transform resources of source PE</summary>
        Resources = 16,

        /// <summary>Transform relocations of source PE</summary>
        Relocs = 32,

        /// <summary>Smash lock prefixes of source PE</summary>
        I386SmashLock = 64,

        /// <summary>Transform relative jumps of source I386 (x86) PE</summary>
        I386Jmps = 128,

        /// <summary>Transform relative calls of source I386 (x86) PE</summary>
        I386Calls = 256,

        /// <summary>Transform instructions of source AMD64 (x86-64) PE</summary>
        Amd64Disasm = 512,

        /// <summary>Transform pdata of source AMD64 (x86-64) PE</summary>
        Amd64PData = 1024,

        /// <summary>Transform instructions of source IA64 (Itanium) PE</summary>
        IA64Disasm = 2048,

        /// <summary>Transform pdata of source IA64 (Itanium) PE</summary>
        IA64PData = 4096,

        /// <summary>Unbind source PE</summary>
        Unbind = 8192,

        /// <summary>Transform CLI instructions of source PE</summary>
        CliDisasm = 16384,

        /// <summary>Transform CLI Metadata of source PE</summary>
        CliMetadata = 32768,

        /// <summary>Transform headers of source PE</summary>
        Headers = 65536,

        /// <summary>Allow the source, target and delta files to exceed the default size limit.</summary>
        IgnoreFileSizeLimit = 131072,

        /// <summary>Allow options buffer or file to exceed its default size limit</summary>
        IgnoreOptionsSizeLimit = 262144,

        /// <summary>Transform instructions of source ARM PE</summary>
        ArmDisasm = 524288,

        /// <summary>Transform pdata of source ARM PE</summary>
        ArmPData = 1048576,

        /// <summary>Transform CLI4 Metadata of source PE</summary>
        Cli4Metadata = 2097152,

        /// <summary>Transform CLI4 instructions of source PE</summary>
        Cli4Disasm = 4194304,

        /// <summary>Transform instructions of source ARM PE</summary>
        Arm64Disasm = 8388608,

        /// <summary>Transform pdata of source ARM PE</summary>
        Arm64PData = 16777216,

        I386 =
            Mark | Imports | Exports
            | Resources | Relocs
            | I386SmashLock | I386Jmps
            | I386Calls | Unbind
            | CliDisasm | CliMetadata,

        IA64 =
            Mark | Imports | Exports
            | Resources | Relocs
            | IA64Disasm | IA64PData | Unbind
            | CliDisasm | CliMetadata,

        Amd64 =
            Mark | Imports | Exports
            | Resources | Relocs
            | Amd64Disasm | Amd64PData | Unbind
            | CliDisasm | CliMetadata,

        Cli4I386 =
            Mark | Imports | Exports
            | Resources | Relocs
            | I386SmashLock | I386Jmps
            | I386Calls | Unbind
            | Cli4Disasm | Cli4Metadata,

        Cli4Amd64 =
            Mark | Imports | Exports
            | Resources | Relocs
            | Amd64Disasm | Amd64PData | Unbind
            | Cli4Disasm | Cli4Metadata,

        Cli4Arm =
            Mark | Imports | Exports
            | Resources | Relocs
            | ArmDisasm | ArmPData | Unbind
            | Cli4Disasm | Cli4Metadata,

        Cli4Arm64 =
            Mark | Imports | Exports
            | Resources | Relocs
            | Arm64Disasm | Arm64PData | Unbind
            | Cli4Disasm | Cli4Metadata,
    }
}