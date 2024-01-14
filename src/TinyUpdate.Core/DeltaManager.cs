using System.Collections.Concurrent;
using System.Collections.Immutable;
using NeoSmart.AsyncLock;
using TinyUpdate.Core.Abstract.Delta;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Core;

/// <summary>
/// Default <see cref="IDeltaManager"/> implementation
/// </summary>
public class DeltaManager(IEnumerable<IDeltaApplier> appliers, IEnumerable<IDeltaCreation> creators)
    : IDeltaManager
{
    private readonly AsyncLock _copyLock = new AsyncLock();

    public IReadOnlyCollection<IDeltaApplier> Appliers { get; } = appliers.ToImmutableArray();
    public IReadOnlyCollection<IDeltaCreation> Creators { get; } = creators.ToImmutableArray();

    public async Task<DeltaCreationResult> CreateDeltaUpdate(Stream sourceStream, Stream targetStream)
    {
        /*As we'll be using multiple creators at the same time, we want to copy the streams here
         and then within the for each below, this is so we're not consistently hitting IO*/
        //TODO: Add some checks for the stream type passed?
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

            //Copy the master copy into this local copy, otherwise multiple creators would clash when using the same stream
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
            var deltaStreamLength = deltaCreationResult.DeltaStream!.Length;
            if (bestCreator == null)
            {
                //Only want to add it if it was at least successful and smaller then the target source at this point
                if (deltaCreationResult.Successful && targetStream.Length > deltaStreamLength)
                {
                    bestCreator = deltaCreationResult;
                }
                continue;
            }
            
            if (deltaCreationResult.Successful && bestCreator.DeltaStream!.Length > deltaStreamLength)
            {
                bestCreator = deltaCreationResult;
            }
        }

        return bestCreator ?? DeltaCreationResult.Failed;
    }
}