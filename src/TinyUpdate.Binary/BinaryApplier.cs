using System;
using System.Threading.Tasks;
using TinyUpdate.Core;

namespace TinyUpdate.Binary
{
    public class BinaryApplier : IUpdateApplier
    {
        public Task<bool> ApplyUpdate(ReleaseEntry entry, Action<decimal>? progress)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ApplyUpdate(UpdateInfo updateInfo, Action<decimal>? progress)
        {
            throw new NotImplementedException();
        }
    }
}