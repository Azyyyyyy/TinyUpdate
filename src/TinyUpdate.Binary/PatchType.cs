namespace TinyUpdate.Binary
{
    /// <summary>
    /// What kind of patch this file is
    /// </summary>
    public enum PatchType
    {
        /// <summary>
        /// The patch was used using <see cref="MsDeltaCompression"/>
        /// </summary>
        MSDiff,
        
        /// <summary>
        /// The patch was used using <see cref="BinaryPatchUtility"/>
        /// </summary>
        BSDiff,
        
        /// <summary>
        /// This is a new file and not a patch
        /// </summary>
        New
    }
}