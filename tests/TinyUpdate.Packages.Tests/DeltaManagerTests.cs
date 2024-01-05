using TinyUpdate.Core.Abstract;
using TinyUpdate.Packages.Tests.Abstract;
using TinyUpdate.TUUP;

namespace TinyUpdate.Packages.Tests;

[Ignore("Not Imp'd yet")]
public class TuupDeltaManagerTests : DeltaManagerCan
{
    protected override IDeltaManager CreateDeltaCreation(IEnumerable<IDeltaApplier> appliers, IEnumerable<IDeltaCreation> creators)
    {
        return new TuupDeltaManager(appliers, creators);
    }
}