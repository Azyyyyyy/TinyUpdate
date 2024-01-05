using TinyUpdate.Core.Abstract;
using TinyUpdate.Tests.Common.Attributes;

namespace TinyUpdate.Packages.Tests.Abstract;

//TODO: Imp
public abstract class DeltaManagerCan
{
    [Test]
    [DeltaApplier]
    public void PickSmallestDeltaFile()
    {
    }
    
    /*Note that if a IDeltaCreation throws an exception, we don't want to
     handle that as the IDeltaCreation should be handling them correctly!*/
    [Test]
    [DeltaApplier]
    public void GracefullyHandleFailedDeltaCreation()
    {
    }

    [Test]
    [DeltaApplier]
    public void RunAllDeltaCreatorsOnlyOnce()
    {
        
    }

    protected abstract IDeltaManager CreateDeltaCreation(IEnumerable<IDeltaApplier> appliers,
        IEnumerable<IDeltaCreation> creators);
}