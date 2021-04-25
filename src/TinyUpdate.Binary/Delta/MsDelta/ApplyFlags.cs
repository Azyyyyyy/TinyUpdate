using System;

namespace TinyUpdate.Binary.Delta.MsDelta
{
    /// <summary>
    /// Flags for when we are applying a <see cref="MsDiff"/> file
    /// </summary>
    [Flags]
    internal enum ApplyFlags : long
    {
        /// <summary>
        /// Indicates no special handling.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow MSDelta to apply deltas created using PatchAPI.
        /// </summary>
        AllowLegacy = 1,
    }
}