namespace TinyUpdate.Core.Abstract;

public interface IDeltaManager
{
    public IReadOnlyCollection<IDeltaApplier> Appliers { get; }
}