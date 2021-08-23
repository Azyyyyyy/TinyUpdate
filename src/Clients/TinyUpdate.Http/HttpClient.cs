using System;
using System.Threading.Tasks;
using TinyUpdate.Core;
using TinyUpdate.Core.Update;

namespace TinyUpdate.Http
{
    public class HttpClient : UpdateClient
    {
        public HttpClient(IUpdateApplier updateApplier) : base(updateApplier)
        {
        }

        public override Task<UpdateInfo?> CheckForUpdate(bool grabDeltaUpdates = true)
        {
            throw new NotImplementedException();
        }

        public override Task<ReleaseNote?> GetChangelog(ReleaseEntry entry)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DownloadUpdate(ReleaseEntry releaseEntry, Action<double>? progress)
        {
            throw new NotImplementedException();
        }
    }
}