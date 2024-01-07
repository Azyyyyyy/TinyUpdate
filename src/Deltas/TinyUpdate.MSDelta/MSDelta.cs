using System.Runtime.InteropServices;
using TinyUpdate.Core.Abstract;
using TinyUpdate.MSDelta.Enum;
using TinyUpdate.MSDelta.Struct;

namespace TinyUpdate.MSDelta;

public partial class MSDelta : IDeltaApplier, IDeltaCreation
{
    public string Extension => ".diff";

    public unsafe bool SupportedStream(Stream deltaStream)
    {
        var deltaBytes = new byte[deltaStream.Length];
        deltaStream.ReadExactly(deltaBytes, 0, deltaBytes.Length);
        
        fixed (byte* deltaBuf = deltaBytes)
        {
            var deltaDeltaInput = new DeltaInput(deltaBuf, deltaBytes.Length, true);
            var supported = GetDeltaInfoB(deltaDeltaInput, out var info);
            return supported;
        }
    }

    public unsafe long TargetStreamSize(Stream deltaStream)
    {
        var deltaBytes = new byte[deltaStream.Length];
        deltaStream.ReadExactly(deltaBytes, 0, deltaBytes.Length);
        
        fixed (byte* deltaBuf = deltaBytes)
        {
            var deltaDeltaInput = new DeltaInput(deltaBuf, deltaBytes.Length, true);
            if (GetDeltaInfoB(deltaDeltaInput, out var info))
            {
                return info.TargetSize;
            }
        }

        return -1;
    }

    public unsafe Task<bool> ApplyDeltaFile(Stream sourceFileStream, Stream deltaFileStream,
        Stream targetFileStream,
        IProgress<double>? progress = null)
    {
        var sourceBytes = new byte[sourceFileStream.Length];
        sourceFileStream.ReadExactly(sourceBytes, 0, sourceBytes.Length);

        var deltaBytes = new byte[deltaFileStream.Length];
        deltaFileStream.ReadExactly(deltaBytes, 0, deltaBytes.Length);

        fixed (byte* sourceBuf = sourceBytes)
        fixed (byte* deltaBuf = deltaBytes)
        {
            var sourceDeltaInput = new DeltaInput(sourceBuf, sourceBytes.Length, true);
            var deltaDeltaInput = new DeltaInput(deltaBuf, deltaBytes.Length, true);

            var cleared = true;
            var success = ApplyDeltaB(ApplyFlag.None, sourceDeltaInput, deltaDeltaInput, out var output);
            if (success)
            {
                var deltaStream = new UnmanagedMemoryStream((byte*)output.Start, output.Size);
                deltaStream.CopyTo(targetFileStream);
                deltaStream.Dispose();
                cleared = DeltaFree(output.Start);
            }

            return Task.FromResult(success && cleared);
        }
    }

    public unsafe Task<bool> CreateDeltaFile(Stream sourceFileStream, Stream targetFileStream,
        Stream deltaFileStream,
        IProgress<double>? progress = null)
    {
        var sourceBytes = new byte[sourceFileStream.Length];
        sourceFileStream.ReadExactly(sourceBytes, 0, sourceBytes.Length);

        var targetBytes = new byte[targetFileStream.Length];
        targetFileStream.ReadExactly(targetBytes, 0, targetBytes.Length);
        
        fixed (byte* sourceBuf = sourceBytes)
        fixed (byte* targetBuf = targetBytes)
        {
            var sourceDeltaInput = new DeltaInput(sourceBuf, sourceBytes.Length, false);
            var targetDeltaInput = new DeltaInput(targetBuf, targetBytes.Length, false);

            var cleared = true;
            var success = CreateDeltaB(FileType.ExecutablesLatest, GetCreateFlags(), FlagType.None, sourceDeltaInput,
                targetDeltaInput, DeltaInput.Empty, DeltaInput.Empty, DeltaInput.Empty, IntPtr.Zero, HashAlgId.Crc32,
                out var deltaBuffer);

            if (success)
            {
                using var deltaStream = new UnmanagedMemoryStream((byte*)deltaBuffer.Start, deltaBuffer.Size);
                deltaStream.CopyTo(deltaFileStream);
                cleared = DeltaFree(deltaBuffer.Start);
            }

            Array.Clear(targetBytes);

            return Task.FromResult(success && cleared);
        }
    }
}

//Ms Imp
public partial class MSDelta
{
    /// <summary>
    ///     The ApplyDelta function use the specified delta and source files to create a new copy of the target file.
    /// </summary>
    /// <param name="applyFlag">Either <see cref="ApplyFlag.None" /> or <see cref="ApplyFlag.AllowLegacy" />.</param>
    /// <param name="source">The name of the source file to which the delta is to be applied.</param>
    /// <param name="delta">The name of the delta to be applied to the source file.</param>
    /// <param name="target">The name of the target file that is to be created.</param>
    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("msdelta.dll", SetLastError = true)]
    private static partial bool ApplyDeltaB(
        ApplyFlag applyFlag,
        DeltaInput source,
        DeltaInput delta,
        out DeltaOutput target);

    /// <summary>
    ///     The CreateDelta function creates a delta from the specified source and target files and write the output delta to
    ///     the designated file name.
    /// </summary>
    /// <param name="fileType">The file type set used for Create.</param>
    /// <param name="setFlags">The file type set used for Create.</param>
    /// <param name="resetFlags">The file type set used for Create.</param>
    /// <param name="source">The file type set used for Create.</param>
    /// <param name="target">The name of the target against which the source is compared.</param>
    /// <param name="sourceOptions">Reserved. Pass NULL.</param>
    /// <param name="targetOptions">Reserved. Pass NULL.</param>
    /// <param name="globalOptions">Reserved. Pass a DELTA_INPUT structure with lpStart set to NULL and uSize set to 0.</param>
    /// <param name="targetFileTime">
    ///     The time stamp set on the target file after delta Apply. If NULL, the timestamp of the
    ///     target file during delta Create will be used.
    /// </param>
    /// <param name="hashAlgId">ALG_ID of the algorithm to be used to generate the target signature.</param>
    /// <param name="deltaOutput">The name of the delta file to be created.</param>
    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("msdelta.dll", SetLastError = true)]
    private static partial bool CreateDeltaB(
        FileType fileType, // File type set.
        FlagType setFlags, // Set these flags.
        FlagType resetFlags, // Reset (suppress) these flags.
        DeltaInput source, // Source memory block.
        DeltaInput target, // Target memory block.
        DeltaInput sourceOptions, // Memory block with source-specific options.
        DeltaInput targetOptions, // Memory block with target-specific options.
        DeltaInput globalOptions, // Memory block with global options.
        IntPtr targetFileTime, // Target file time to use, null to use current time.
        HashAlgId hashAlgId,
        out DeltaOutput deltaOutput);


    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("msdelta.dll", SetLastError = true)]
    private static partial bool DeltaFree(IntPtr memory);

    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("msdelta.dll", SetLastError = true)]
    internal static partial bool GetDeltaInfoB(
        DeltaInput delta,
        out DeltaHeaderInfo target);
    
    private static FlagType GetCreateFlags()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm => FlagType.Cli4Arm | FlagType.IgnoreFileSizeLimit,
            Architecture.Arm64 => FlagType.Cli4Arm64 | FlagType.IgnoreFileSizeLimit,
            Architecture.X64 => FlagType.Cli4Amd64 | FlagType.IgnoreFileSizeLimit,
            Architecture.X86 => FlagType.Cli4I386 | FlagType.IgnoreFileSizeLimit,
            _ => FlagType.IgnoreFileSizeLimit
        };
    }
}