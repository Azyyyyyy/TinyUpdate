using System;
using System.Runtime.InteropServices;

namespace TinyUpdate.Create
{
    public static class Global
    {
        public static OSPlatform? IntendedOS { get; set; }
        public static bool CreateDeltaUpdate { get; set; }
        public static string NewVersionLocation { get; set; } = null!;

        public static bool CreateFullUpdate { get; set; }
        public static string? OldVersionLocation { get; set; }

        public static string OutputLocation { get; set; } = null!;
        public static Version ApplicationNewVersion { get; set; } = null!;
        public static Version? ApplicationOldVersion { get; set; }
        public static string MainApplicationName { get; set; } = null!;

        public static bool SkipVerify { get; set; }
        public static bool AskIfUserWantsToVerify { get; set; } = true;
    }
}