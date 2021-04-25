﻿using System.Runtime.InteropServices;

namespace TinyUpdate.Core
{
    /// <summary>
    /// Interface which lets other services know what OS it runs on
    /// </summary>
    public interface ISpecificOS
    {
        /// <summary>
        /// The OS that this service is intended for (Null for any OS)
        /// </summary>
        public OSPlatform? IntendedOS { get; }
    }
}