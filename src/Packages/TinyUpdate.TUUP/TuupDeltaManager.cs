using System.Collections.Concurrent;
using System.Collections.Immutable;
using NeoSmart.AsyncLock;
using TinyUpdate.Core.Abstract;

namespace TinyUpdate.TUUP;

public class TuupDeltaManager : IDeltaManager
{
    protected AsyncLock _copyLock = new AsyncLock();
    public TuupDeltaManager(IEnumerable<IDeltaApplier> appliers, IEnumerable<IDeltaCreation> creators)
    {
        Appliers = appliers.ToImmutableArray();
        Creators = creators.ToImmutableArray();
    }
    
    public IReadOnlyCollection<IDeltaApplier> Appliers { get; }
    public IReadOnlyCollection<IDeltaCreation> Creators { get; }
    public async Task<DeltaCreationResult> CreateDeltaFile(Stream sourceStream, Stream targetStream)
    {
        var resultBag = new ConcurrentBag<DeltaCreationResult>();

        var sourceStreamMasterCopy = new MemoryStream();
        var targetStreamMasterCopy = new MemoryStream();

        await sourceStream.CopyToAsync(sourceStreamMasterCopy);
        await targetStream.CopyToAsync(targetStreamMasterCopy);
        
        await Parallel.ForEachAsync(Creators, async (creator, token) =>
        {
            var deltaStream = new MemoryStream();
            var sourceStreamLocalCopy = new MemoryStream();
            var targetStreamLocalCopy = new MemoryStream();

            using (await _copyLock.LockAsync(token))
            {
                sourceStreamMasterCopy.Seek(0, SeekOrigin.Begin);
                targetStreamMasterCopy.Seek(0, SeekOrigin.Begin);
                
                await sourceStreamMasterCopy.CopyToAsync(sourceStreamLocalCopy, token);
                await targetStreamMasterCopy.CopyToAsync(targetStreamLocalCopy, token);
            }
            
            sourceStreamLocalCopy.Seek(0, SeekOrigin.Begin);
            targetStreamLocalCopy.Seek(0, SeekOrigin.Begin);

            var successful = await creator.CreateDeltaFile(sourceStreamLocalCopy, targetStreamLocalCopy, deltaStream);
            resultBag.Add(new DeltaCreationResult(creator, deltaStream, successful));
        });

        DeltaCreationResult? bestCreator = null;
        foreach (var deltaCreationResult in resultBag)
        {
            if (bestCreator == null)
            {
                //Only want to add it if it was at least successful at this point
                if (deltaCreationResult.Successful)
                {
                    bestCreator = deltaCreationResult;
                }
                continue;
            }
            
            if (deltaCreationResult.Successful && bestCreator.DeltaStream!.Length < deltaCreationResult.DeltaStream.Length)
            {
                bestCreator = deltaCreationResult;
            }
        }

        return bestCreator ?? new DeltaCreationResult(null, null, false);
    }
}