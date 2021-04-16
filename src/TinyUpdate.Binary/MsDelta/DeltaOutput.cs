using System;
using System.Runtime.InteropServices;

namespace TinyUpdate.Binary.MsDelta
{
    /// <summary>
    /// Type for input memory blocks
    /// </summary>
    internal struct DeltaOutput
    {
        /// <summary>Memory address</summary>
        public IntPtr Start;

        /// <summary>Size of the memory buffer in bytes.</summary>
        public IntPtr Size;
    }
}