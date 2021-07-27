using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SemVersion;
using TinyUpdate.Core.Utils;

namespace TinyUpdate.Core.Tests
{
    public class ReleaseFileTest
    {
        [SetUp]
        public void Setup()
        {
            if (File.Exists(ReleaseFileLocation))
            {
                File.Delete(ReleaseFileLocation);
            }
        }
        
        [Test]
        public async Task CanCreateReleaseFile_FailOnFolderNotExisting()
        {
            var dummyReleaseFileEntries = CreateReleaseFiles();
            var fileLocation = Randomizer.GetString();
            if (Directory.Exists(fileLocation))
            {
                Directory.Delete(fileLocation, true);
            }
            Assert.False(await ReleaseFile.CreateReleaseFile(dummyReleaseFileEntries, fileLocation), 
                "Creating RELEASE file passed even when we had no folder to work with");
        }

        private readonly string ReleaseFileLocation = Path.Combine(TestContext.CurrentContext.WorkDirectory, "RELEASE");

        [Test]
        public async Task CanCreateReleaseFile()
        {
            var dummyReleaseFileEntries = CreateReleaseFiles();
            Assert.True(
                await ReleaseFile.CreateReleaseFile(dummyReleaseFileEntries, TestContext.CurrentContext.WorkDirectory),
                "Wasn't able to create the release file");

            Assert.True(
                dummyReleaseFileEntries.SequenceEqual(
                ReleaseFile.ReadReleaseFile(File.ReadLines(ReleaseFileLocation))),
                "What we made and what we read from disk isn't the same");
        }

        [Test]
        public async Task CanCreateReleaseFile_FileAlreadyExists()
        {
            await CanCreateReleaseFile();

            //We shouldn't throw but just delete the file and make it with the new data
            Assert.DoesNotThrowAsync(async () => await CanCreateReleaseFile(),
                "We threw an error when we should of handled the file already existing");
        }
        
        private static (string[] lines, ReleaseFile[] releaseFiles)[] testReadData =
        {
            (new []
            {
                "3AD4CBAB73D60FB3D7E6519E63129876C8F8600AE357B4E8D5624318DED0BDAB OvVc6XjutW7tgzyNKA_z6TD33 1000",
                "48266375EFC174F113D3901C974105E2820D7FF97D02CFE60618411B42DA78B2 Vj9YyuXqq6OYtOG_ygfEdgQOg 420",
                "9CFAFA98346B577852F136BA8F2BE4913A3006F0FF780E9EB8DB90B16C5F0582 QN7qPcqEXL_dFyKeMa95MDKuR 3.8.9 1000",
                "64CA34314DE93365D718E81582A88940060087BEA3C2580F9136AED7FEE19F1D L95AMHJg84c5Krs8w80GebJBN 3.2.4 691"
            }, 
            new ReleaseFile[]
            {
                new("3AD4CBAB73D60FB3D7E6519E63129876C8F8600AE357B4E8D5624318DED0BDAB", "OvVc6XjutW7tgzyNKA_z6TD33", 1000, null),
                new("48266375EFC174F113D3901C974105E2820D7FF97D02CFE60618411B42DA78B2", "Vj9YyuXqq6OYtOG_ygfEdgQOg", 420, null),
                new("9CFAFA98346B577852F136BA8F2BE4913A3006F0FF780E9EB8DB90B16C5F0582", "QN7qPcqEXL_dFyKeMa95MDKuR", 1000, null, new SemanticVersion(3, 8, 9)),
                new("64CA34314DE93365D718E81582A88940060087BEA3C2580F9136AED7FEE19F1D", "L95AMHJg84c5Krs8w80GebJBN", 691, null, new SemanticVersion(3, 2, 4))
            })
        };

        [Test]
        [TestCaseSource(nameof(testReadData))]
        public void CanReadReleaseFile((string[], ReleaseFile[]) releaseContent)
        {
            var (lines, expectedReleaseFiles) = releaseContent;
            var releaseFiles = ReleaseFile.ReadReleaseFile(lines);
            Assert.True(expectedReleaseFiles.SequenceEqual(releaseFiles),
                "Release files created from lines isn't the same as we expect");
        }
        
        private static string[][] invalidTestReadData =
        {
            new []
            {
                "3AD4CBAB73D60FB3D7E6519E63129876C8F8600AE357B4E8D5624318DED0BDABOvVc6XjutW7tgzyNKA_z6TD33 1000",
                "48266375EFC174F113D3901C974105E2820D7FF97D02CFE60618411B42DA78B2 Vj9YyuXqq6OYtOG_ygfEdgQOg420",
                "9CFAFA98346B577852F136BA8F2BE4913A3006F0FF780E9EB8DB90B16C5F0582 QN7qPcqEXL_dFyKeMa95MDKuR 3.8.91000",
                "64CA34314DE93365D718E81582A88940060087BEA3C2580F9136AED7FEE19F1D L95AMHJg84c5Krs8w80GebJBN 6p91"
            }
        };

        [Test]
        [TestCaseSource(nameof(invalidTestReadData))]
        public void DontThrowOnInvalidReleaseFile(string[] lines)
        {
            IEnumerable<ReleaseFile> releaseFiles = ArraySegment<ReleaseFile>.Empty;
            Assert.DoesNotThrow(() => releaseFiles = ReleaseFile.ReadReleaseFile(lines),
                "We threw an error when we should of handled having invalid data");
            Assert.IsEmpty(releaseFiles,
                "We somehow got a release file from invalid data");
        }

        private ReleaseFile[] CreateReleaseFiles()
        {
            var releaseFiles = new ReleaseFile[Randomizer.Next(10)];
            for (int i = 0; i < releaseFiles.Length; i++)
            {
                var (hash, size, oldVersion) = CreateRandomFileData();
                releaseFiles[i] = new ReleaseFile(hash, Randomizer.GetString(), size, null, oldVersion);
            }

            return releaseFiles;
        }
        
        private static Randomizer Randomizer => TestContext.CurrentContext.Random;

        private (string hash, long size, SemanticVersion? oldVersion) CreateRandomFileData()
        {
            SemanticVersion? oldVersion = null;
            var data = new byte[Randomizer.Next(1000)];
            Randomizer.NextBytes(data);

            var hash = SHA256Util.CreateSHA256Hash(data);
            if (Randomizer.NextBool())
            {
                var major = Randomizer.Next(0, 10);
                var minor = Randomizer.Next(0, 10);
                var build = Randomizer.Next(0, 10);
                oldVersion = new SemanticVersion(major, minor, build);
            }
            
            return (hash, data.Length, oldVersion);
        }
    }
}