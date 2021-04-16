using System.Threading.Tasks;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Create
{
    public static class CreateUpdate
    {
        private static readonly CustomConsoleLogger Logger = new(nameof(CreateUpdate));

        public static async Task<bool> CreateDeltaUpdate(IUpdateCreator updateCreator)
        {
            if (Global.OldVersionLocation == null)
            {
                Logger.Error("We don't have the old version of the application, can't create update");
                return false;
            }
            
            Logger.WriteLine("Creating Delta update");
            using var progressBar = new ProgressBar();
            var wasUpdateCreated = 
                await updateCreator.CreateDeltaPackage(
                    Global.NewVersionLocation,
                    Global.OldVersionLocation,
                    Program.GetOutputLocation(true, updateCreator.Extension),
                    progress => progressBar.Report((double)progress));

            Console.ShowSuccess(wasUpdateCreated);
            return wasUpdateCreated;
        }
        
        public static async Task<bool> CreateFullUpdate(IUpdateCreator updateCreator)
        {
            Logger.WriteLine("Creating Full update");
            using var progressBar = new ProgressBar();
            var wasUpdateCreated = 
                await updateCreator.CreateFullPackage(
                    Global.NewVersionLocation,
                    Program.GetOutputLocation(false, updateCreator.Extension),
                    progress => progressBar.Report((double)progress));

            Console.ShowSuccess(wasUpdateCreated);
            return wasUpdateCreated;
        }
    }
}