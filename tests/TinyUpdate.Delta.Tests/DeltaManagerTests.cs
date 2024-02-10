using TinyUpdate.Core;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Services;
using TinyUpdate.Core.Tests;
using TinyUpdate.Delta.Tests.Abstract;

namespace TinyUpdate.Delta.Tests;

public class DeltaManagerTests : DeltaManagerCan
{
    protected override IDeltaManager CreateDeltaManager(IEnumerable<IDeltaApplier> appliers, IEnumerable<IDeltaCreation> creators) => new DeltaManager(appliers, creators, NUnitLogger<DeltaManager>.Instance);
}