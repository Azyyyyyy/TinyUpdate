using System.Collections.Generic;
using System.Linq;
using TinyUpdate.Binary.Delta.MsDelta;
using TinyUpdate.Core.Helper;
using TinyUpdate.Core.Logging;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Binary.Delta
{
    public static class DeltaUpdaters
    {
        private static readonly ILogging Logger = LoggingCreator.CreateLogger(nameof(DeltaUpdaters));
        private static readonly List<IDeltaUpdate> _updaters = new List<IDeltaUpdate>(2);

        static DeltaUpdaters()
        {
            AddDeltaUpdater(new MsDiff());
            AddDeltaUpdater(new BSDiff());
        }

        public static void AddDeltaUpdater(IDeltaUpdate deltaUpdater, bool addToTop = false)
        {
            if (deltaUpdater.IntendedOs.HasValue
                && OSHelper.ActiveOS != deltaUpdater.IntendedOs.Value)
            {
                Logger.Error("{0} can't be used as it is intended to be used on {1} but the OS we are running on is {2}", 
                    deltaUpdater.GetType().Name, deltaUpdater.IntendedOs, OSHelper.ActiveOS);
                return;
            }
            
            if (_updaters.All(x => x.Extension != deltaUpdater.Extension))
            {
                if (addToTop)
                {
                    _updaters.Insert(0, deltaUpdater);
                    return;
                }
                _updaters.Add(deltaUpdater);
            }
        }

        public static IReadOnlyList<IDeltaUpdate> Updaters => _updaters.AsReadOnly();
        
        public static IDeltaUpdate? GetUpdater(string deltaExtension) =>
            _updaters.FirstOrDefault(x => x.Extension == deltaExtension);
    }
}