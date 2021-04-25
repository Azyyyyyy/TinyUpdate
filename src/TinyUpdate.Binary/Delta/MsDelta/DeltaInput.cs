﻿using System;
using System.Runtime.InteropServices;

namespace TinyUpdate.Binary.Delta.MsDelta
{
    /// <summary>
    /// Type for input memory blocks
    /// </summary>
    internal struct DeltaInput
    {
        /// <summary>Memory address non-editable input buffer.</summary>
        public IntPtr Start;

        /// <summary>Size of the memory buffer in bytes.</summary>
        public IntPtr Size;

        /// <summary>
        /// Defines whether MSDelta is allowed to edit the input buffer. If you make the input editable, the buffer will
        /// be zeroed at function return. However this will cause most MSDelta functions to use less memory.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)] public bool Editable;
    }
}