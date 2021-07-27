using TinyUpdate.Core.Update;
using TinyUpdate.Create.Helper;
using SemVersion;

namespace TinyUpdate.Create
{
    public static class CreateUpdate
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(CreateUpdate));

        public static bool CreateDeltaUpdate(IUpdateCreator updateCreator)
        {
            if (Global.OldVersionLocation == null)
            {
                Logger.Error("We don't have the old version of the application, can't create update");
                return false;
            }

            Logger.WriteLine("Creating Delta update");
            var progressBar = new ProgressBar();
            var wasUpdateCreated =
                updateCreator.CreateDeltaPackage(
                    Global.ApplicationMetadata,
                    Global.NewVersionLocation,
                    Global.ApplicationNewVersion,
                    Global.OldVersionLocation,
                    Global.ApplicationOldVersion!,
                    Global.OutputLocation,
                    Program.GetOutputLocation(true, updateCreator.Extension),
                    Global.IntendedOs,
                    progress => progressBar.Report(progress));
            progressBar.Dispose();

            ConsoleHelper.ShowSuccess(wasUpdateCreated);
            return wasUpdateCreated;
        }

        public static bool CreateFullUpdate(IUpdateCreator updateCreator)
        {
            Logger.WriteLine("Creating Full update");
            var progressBar = new ProgressBar();
            var wasUpdateCreated =
                updateCreator.CreateFullPackage(
                    Global.ApplicationMetadata,
                    Global.NewVersionLocation,
                    Global.ApplicationNewVersion,
                    Program.GetOutputLocation(false, updateCreator.Extension),
                    Global.IntendedOs,
                    progress => progressBar.Report(progress));
            progressBar.Dispose();

            ConsoleHelper.ShowSuccess(wasUpdateCreated);
            return wasUpdateCreated;
        }
    }
}