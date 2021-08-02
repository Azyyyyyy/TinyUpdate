using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;

namespace TinyUpdate.Create
{
    public class Commands
    {
        public Commands(bool delta, bool full, DirectoryInfo? outputLocation, DirectoryInfo? newVersionLocation, DirectoryInfo? oldVersionLocation, string? applicationFile,
            bool skipVerifying, bool verify, string? applierType, string? creatorType, string? intendedOs, int? stagingPercentage)
        {
            Delta = delta;
            Full = full;
            OutputLocation = outputLocation;
            NewVersionLocation = newVersionLocation;
            OldVersionLocation = oldVersionLocation;
            ApplicationFile = applicationFile!;
            ShouldVerify = verify;
            ApplierType = applierType;
            CreatorType = creatorType;
            StagingPercentage = stagingPercentage;
            SkipVerify = skipVerifying;
            IntendedOs = !string.IsNullOrWhiteSpace(intendedOs)
                ? OSPlatform.Create(intendedOs)
                : null;
        }
        
        public bool Delta { get; }

        public bool Full { get; }

        public DirectoryInfo? OutputLocation { get; }

        public DirectoryInfo? NewVersionLocation { get; }

        public DirectoryInfo? OldVersionLocation { get; }

        public string ApplicationFile { get; }

        public bool SkipVerify { get; }

        public bool ShouldVerify { get; }

        public OSPlatform? IntendedOs { get; }

        public int? StagingPercentage { get; }

        public string? ApplierType { get; }

        public string? CreatorType { get; }

        public static RootCommand GetRootCommand()
        {
            return new RootCommand
            {
                new Option<bool>(
                    new[] {"-d", "--delta"},
                    "Create a delta update"),
                new Option<bool>(
                    new[] {"-f", "--full"},
                    "Create a full update"),
                new Option<DirectoryInfo?>(
                    new[] {"-o", "--output-location"},
                    "Where any files created should be stored"),
                new Option<DirectoryInfo?>(
                    new[] {"--nl", "--new-version-location"},
                    "Where the new version of the application is stored"),
                new Option<DirectoryInfo?>(
                    new[] {"--ol", "--old-version-location"},
                    "Where the old version of the application is stored"),
                new Option<string>(
                    new[] {"--af", "--application-file"},
                    "What is the main application file?"),
                new Option<bool>(
                    new[] {"-s", "--skip-verifying"},
                    "Skip verifying that the update applies correctly"),
                new Option<bool>(
                    new[] {"-v", "--verify"},
                    "Verify that the update applies correctly"),
                new Option<string?>(
                    new[] {"--at", "--applier-type"},
                    "What type is used for applying the update"),
                new Option<string?>(
                    new[] {"--ct", "--creator-type"},
                    "What type is used for creating updates"),
                new Option<string?>(
                    new[] {"--os", "--intended-os"},
                    "What os the update is intended for"),
                new Option<int?>(
                    new[] {"--sp", "--staging-percentage"},
                    "Limits the amount of people who gets this update"),
            };
        }
    }
}