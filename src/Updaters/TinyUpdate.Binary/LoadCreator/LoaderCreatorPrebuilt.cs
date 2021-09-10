using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;

namespace TinyUpdate.Binary.LoadCreator
{
    public static class LoaderCreatorPrebuilt
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(LoaderCreatorSource));
        private const string Pathline = "{APPLICATIONLOCATION}";

        /// <summary>
        /// Creates the loader that will be needed for loading the application
        /// </summary>
        /// <param name="path">The relative path to the application</param>
        /// <param name="iconLocation">Where the icon is for this application</param>
        /// <param name="outputFile">Where to put the loader</param>
        /// <param name="intendedOs">What OS this loader is intended for</param>
        /// <returns>If the loader was created</returns>
        public static LoadCreateStatus CreateLoader(string path, string? iconLocation, string outputFile, OSPlatform? intendedOs)
        {
            //Check that we should create a loader (As we currently only have one for Windows) 
            if (intendedOs.HasValue && intendedOs.Value != OSPlatform.Windows)
            {
                Logger.Warning("Tried to make loader for {0} but we can only make a loader for Windows right now!", Enum.GetName(typeof(OSPlatform), intendedOs));
                return LoadCreateStatus.UnableToCreate;
            }
            if (!intendedOs.HasValue && OSHelper.ActiveOS != OSPlatform.Windows)
            {
                Logger.Warning("We are not running on Windows, not going to create loader when we are unsure if this update is for Windows");
                return LoadCreateStatus.UnableToCreate;
            }
            
            using var fileStream = LoaderCreator.Assembly.GetManifestResourceStream("TinyUpdate.Binary.LoaderTemplate.Windows.ApplicationLoader.exe");
            if (fileStream == null)
            {
                Logger.Error("Wasn't able to get file stream, can't create loader");
                return LoadCreateStatus.Failed;
            }

            //Try to find where {APPLICATIONLOCATION} is in the stream
            long start = -1;
            long end = -1;
            while (fileStream.Length != fileStream.Position)
            {
                var c = (char) fileStream.ReadByte();
                if (c != '{')
                {
                    continue;
                }

                var app = fileStream.ReadExactly(Pathline.Length - 1).Select(x => (char)x);
                if ('{' + string.Join(string.Empty, app) == Pathline)
                {
                    end = fileStream.Position;
                    start = fileStream.Position - Pathline.Length;
                    break;
                }
            }

            if (start == -1)
            {
                Logger.Error("Can't find where we need to add the path, failing...");
                return LoadCreateStatus.Failed;
            }
            
            var tmpFileStream = FileHelper.MakeFileStream(outputFile, FileMode.CreateNew, FileAccess.ReadWrite, fileStream.Length);
            using var fileEditStream = new BinaryWriter(tmpFileStream, Encoding.ASCII, false);
            fileStream.Seek(0, SeekOrigin.Begin);

            fileEditStream.Write(fileStream.ReadExactly((int)start));
            fileEditStream.Write(path);

            fileStream.Seek(end - start + 1, SeekOrigin.Current);
            fileEditStream.Write(fileStream.ReadExactly((int)(fileStream.Length - fileStream.Position)));
            
            //TODO: Try to remake this so we can do it ourselves
            //https://github.com/electron/rcedit
            return fileEditStream.BaseStream.Length == fileStream.Length + (path.Length - "{APPLICATIONLOCATION}".Length) 
                ? LoadCreateStatus.Successful : LoadCreateStatus.Failed;
        }
    }
}