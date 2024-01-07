namespace TinyUpdate.Delta.MSDelta.Enum;

/// <summary>
///     Flags for when we are applying a <see cref="MSDelta" /> file
/// </summary>
[Flags]
internal enum ApplyFlag : long
{
    /// <summary>
    ///     Indicates no special handling.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Allow MSDelta to apply deltas created using PatchAPI.
    /// </summary>
    AllowLegacy = 1
}