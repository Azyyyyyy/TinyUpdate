using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
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
            string sha256 = null;
            var isDelta = false;
            version ??= new Version(
                Global.ApplicationVersion.Major + 1,
                Global.ApplicationVersion.Minor,
                Global.ApplicationVersion.Build >= 0 ? Global.ApplicationVersion.Build : 0,
                Global.ApplicationVersion.Revision >= 0 ? Global.ApplicationVersion.Revision : 0);

            var getRandomData = true;
            //Create some dummy content so we can make a SHA256 hash to work with
            byte[] dummyContent = null;
            while (getRandomData)
            {
                getRandomData = 
                    invalidReleaseOptions.Contains(InvalidReleaseEntry.Data) && dummyContent == null;
                dummyContent = new byte[10000];
                _rnd.NextBytes(dummyContent);
                sha256 ??= SHA256Util.CreateSHA256Hash(dummyContent, true);
            }
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
                await using var dummyReleaseFile = File.OpenWrite(dummyReleaseFileLocation);
                await dummyReleaseFile.WriteAsync(dummyContent, 0, dummyContent.Length);
            }

            //Do any data corruption if needed
            foreach (var invalidReleaseOption in invalidReleaseOptions)
            {
                switch (invalidReleaseOption)
                {
                    case InvalidReleaseEntry.SHA256:
                        sha256 = TestContext.CurrentContext.Random.GetString(64, "abcdefghijkmnopqrstuvwxyz0123456789_@\"/.,\\£$%*&)(~#¬`");
                        break;
                    case InvalidReleaseEntry.Filename:
                        filename = TestContext.CurrentContext.Random.GetString(20,
                            "abcdefghijkmnopqrstuvwxyz0123456789_" + Path.GetInvalidFileNameChars());
                        break;
                    case InvalidReleaseEntry.Filesize:
                        filesize = -1;
                        break;
                    case InvalidReleaseEntry.IsDelta:
                        break;
                    case InvalidReleaseEntry.Version:
                        version = new Version(version.Major - 2, version.Minor, version.Build);
                        break;
                    default:
                        
                        break;
                }
            }

            return new ReleaseEntry(sha256, filename, filesize, isDelta, version);
        }
    }
}