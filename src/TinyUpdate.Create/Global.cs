using System;

namespace TinyUpdate.Create
{
    public static class Global
    {
        public static bool CreateDeltaUpdate { get; set; }
        public static string NewVersionLocation { get; set; } = "";
        
        public static bool CreateFullUpdate { get; set; }
        public static string? OldVersionLocation { get; set; }

        public static string OutputLocation { get; set; } = "";
        public static Version ApplicationNewVersion { get; set; } = null!;
        public static Version? ApplicationOldVersion { get; set; }
        public static string MainApplicationName { get; set; } = "";
    }
}