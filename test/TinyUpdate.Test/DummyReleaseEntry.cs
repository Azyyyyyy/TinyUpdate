﻿using System;
using System.IO;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Test
{
    public static class DummyReleaseEntry
    {
        private static Random _rnd = new Random();
        
        //TODO: Finish
        public static async Task<ReleaseEntry> MakeDummyReleaseEntry(bool createFile, string filename = null, params InvalidReleaseEntry[] invalidReleaseOptions)
        {
            //Create everything that is needed for having a ReleaseEntry
            filename ??= $"dummy{Global.TinyUpdateExtension}";
            long filesize;
            string sha1;
            var isDelta = false;
            var version = new Version(
                Global.ApplicationVersion.Major + 1,
                Global.ApplicationVersion.Minor,
                Global.ApplicationVersion.Build >= 0 ? Global.ApplicationVersion.Build : 0,
                Global.ApplicationVersion.Revision >= 0 ? Global.ApplicationVersion.Revision : 0);

            //Create some dummy content so we can make a sha1 hash to work with
            var dummyContent = new byte[10000];
            _rnd.NextBytes(dummyContent);
            sha1 = SHA1Util.CreateSHA1Hash(dummyContent);
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
                dummyReleaseFile.Dispose();                
            }

            //Do any data corruption if needed
            foreach (var invalidReleaseOption in invalidReleaseOptions)
            {
                switch (invalidReleaseOption)
                {
                    case InvalidReleaseEntry.SHA1:
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

            return new ReleaseEntry(sha1, filename, filesize, isDelta, version);
        }
    }
}