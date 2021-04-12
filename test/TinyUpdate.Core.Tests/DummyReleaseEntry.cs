using System;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core.Tests
{
    public static class DummyReleaseEntry
    {
        private static readonly Random _rnd = new Random();
        
        //TODO: Finish
        public static async Task<ReleaseEntry> MakeDummyReleaseEntry(bool createFile, string extension, string? filename = null, Version? version = null, params InvalidReleaseEntry[] invalidReleaseOptions)
        {
            //Create everything that is needed for having a ReleaseEntry
            filename ??= $"dummy-{Guid.NewGuid()}{extension}";
            long filesize;
            string sha256;
            var isDelta = false;
            version ??= new Version(
                Global.ApplicationVersion.Major + 1,
                Global.ApplicationVersion.Minor,
                Global.ApplicationVersion.Build >= 0 ? Global.ApplicationVersion.Build : 0,
                Global.ApplicationVersion.Revision >= 0 ? Global.ApplicationVersion.Revision : 0);

            //Create some dummy content so we can make a SHA256 hash to work with
            var dummyContent = new byte[10000];
            _rnd.NextBytes(dummyContent);
            sha256 = SHA256Util.CreateSHA256Hash(dummyContent, true);
            filesize = dummyContent.Length;

            var dummyReleaseFileLocation = Path.Combine(Global.TempFolder, filename);
            if (File.Exists(dummyReleaseFileLocation))
            {
                File.Delete(dummyReleaseFileLocation);
            }
            //Put the dummy content onto the disk if we are told to
            if (createFile)
            {
                Directory.CreateDirectory(Global.TempFolder);
                var dummyReleaseFile = File.OpenWrite(dummyReleaseFileLocation);

                await dummyReleaseFile.WriteAsync(dummyContent, 0, 0);
                await dummyReleaseFile.DisposeAsync();                
            }

            //Do any data corruption if needed
            foreach (var invalidReleaseOption in invalidReleaseOptions)
            {
                switch (invalidReleaseOption)
                {
                    case InvalidReleaseEntry.SHA256:
                        break;
                    case InvalidReleaseEntry.Filename:
                        break;
                    case InvalidReleaseEntry.Filesize:
                        break;
                    case InvalidReleaseEntry.IsDelta:
                        break;
                    case InvalidReleaseEntry.Version:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new ReleaseEntry(sha256, filename, filesize, isDelta, version);
        }
    }
}