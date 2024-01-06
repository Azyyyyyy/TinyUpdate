using TinyUpdate.Core;
using TinyUpdate.Core.Abstract;
using TinyUpdate.Delta.Tests.Abstract;

namespace TinyUpdate.Packages.Tests;

public class DeltaManagerTests : DeltaManagerCan
{
    protected override IDeltaManager CreateDeltaManager(IEnumerable<IDeltaApplier> appliers, IEnumerable<IDeltaCreation> creators) => new DeltaManager(appliers, creators);
}