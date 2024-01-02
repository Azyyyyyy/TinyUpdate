using System.Diagnostics.CodeAnalysis;

namespace TinyUpdate.Core.Abstract;

public interface IDeltaManager
{
    public IReadOnlyCollection<IDeltaApplier> Appliers { get; }
    
    public IReadOnlyCollection<IDeltaCreation> Creators { get; }

    public Task<DeltaCreationResult> CreateDeltaFile(Stream sourceStream, Stream targetStream);
}

public class DeltaCreationResult(IDeltaCreation? creator, Stream? deltaStream, bool successful)
{
    public IDeltaCreation? Creator { get; } = creator;

    public Stream? DeltaStream { get; } = deltaStream;

    [MemberNotNullWhen(true, nameof(Creator), nameof(DeltaStream))]
    public bool Successful { get; } = successful;
}