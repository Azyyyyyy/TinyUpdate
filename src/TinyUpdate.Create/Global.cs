using System;
using System.Runtime.InteropServices;
using TinyUpdate.Core;

namespace TinyUpdate.Create
{
    public static class Global
    {
        public static ApplicationMetadata ApplicationMetadata { get; set; } = new ApplicationMetadata();

        public static OSPlatform? IntendedOs { get; set; }
        public static bool CreateDeltaUpdate { get; set; }

        public static string MainApplicationFile { get; set; } = null!;
        public static string OutputLocation { get; set; } = null!;
        public static bool CreateFullUpdate { get; set; }

        public static Version? ApplicationOldVersion { get; set; }
        public static string? OldVersionLocation { get; set; }

        public static Version ApplicationNewVersion { get; set; } = null!;
        public static string NewVersionLocation { get; set; } = null!;
        
        public static bool SkipVerify { get; set; }
        public static bool AskIfUserWantsToVerify { get; set; } = true;
    }
}