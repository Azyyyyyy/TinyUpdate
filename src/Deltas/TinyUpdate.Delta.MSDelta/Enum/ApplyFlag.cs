namespace TinyUpdate.Delta.MSDelta.Enum;

/// <summary>
/// Apply Flag to allow PatchAPI deltas to be applied 
/// </summary>
[Flags]
internal enum ApplyFlag : long
{
    /// <summary>
    ///     Indicates no special handling
    /// </summary>
    None = 0,

    /// <summary>
    ///     Allow MSDelta to apply deltas created using PatchAPI
    /// </summary>
    AllowLegacy = 1
}