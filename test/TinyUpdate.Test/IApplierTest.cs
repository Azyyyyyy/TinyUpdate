using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Test
{
    public abstract class UpdateApplierTest
    {
        private IUpdateApplier _updateApplier;
        protected UpdateApplierTest(IUpdateApplier updateApplier)
        {
            _updateApplier = updateApplier;
        }
        
        [Test]
        public async Task ApplyUpdate_ReleaseEntry()
        {
            await _updateApplier.ApplyUpdate(new ReleaseEntry("", "", 0, false, new Version(9, 9, 9, 9)));
        }

        [Test]
        public Task ApplyUpdate_UpdateInfo()
        {
            //UpdateInfo updateInfo, Action<decimal>? progress
            throw new NotImplementedException();
        }
    }
}