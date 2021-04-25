using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TinyUpdate.Core.Helper
{
    /// <summary>
    /// Helper to run tasks based on the OS that is currently running
    /// </summary>
    public static class TaskHelper
    {
        /// <summary>
        /// Runs a task based on what OS we are currently running on
        /// </summary>
        /// <param name="windowsTask">Task to run when on <see cref="OSPlatform.Windows"/></param>
        /// <param name="linuxTask">Task to run when on <see cref="OSPlatform.Linux"/></param>
        /// <param name="macOSTask">Task to run when on <see cref="OSPlatform.OSX"/></param>
        /// <typeparam name="T">What type to return from the task</typeparam>
        /// <exception cref="PlatformNotSupportedException">We are unable to find the OS you are on or we currently don't support the OS</exception>
        public static T RunTaskBasedOnOS<T>(Func<T> windowsTask, Func<T> linuxTask, Func<T> macOSTask)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return windowsTask.Invoke();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return linuxTask.Invoke();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return macOSTask.Invoke();
            }

            throw new PlatformNotSupportedException("We currently don't support the OS we are currently running on");
        }

        /// <inheritdoc cref="RunTaskBasedOnOS{T}"/>
        public static Task<T> RunTaskBasedOnOSAsync<T>(Task<T> windowsTask, Task<T> linuxTask, Task<T> macOSTask)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return windowsTask;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return linuxTask;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return macOSTask;
            }

            throw new Exception("We currently don't support the OS we are currently running on");
        }
    }
}