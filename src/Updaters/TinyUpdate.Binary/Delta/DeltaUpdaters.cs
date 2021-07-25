using System.Collections.Generic;
using System.Linq;
using TinyUpdate.Binary.Delta.MsDelta;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Binary.Delta
{
    public static class DeltaUpdaters
    {
        private static readonly List<IDeltaUpdate> _updaters = new List<IDeltaUpdate>()
        {
            new MsDiff(),
            new BSDiff()
        };

        public static void AddDeltaUpdater(IDeltaUpdate deltaUpdater, bool addToTop)
        {
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