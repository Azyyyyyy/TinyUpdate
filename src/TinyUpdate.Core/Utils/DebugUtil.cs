using System;
using System.Reflection;

namespace TinyUpdate.Core.Utils;

/// <summary>
/// Functions to assist in debugging
/// </summary>
public static class DebugUtil
{
    static DebugUtil()
    {
        try
        {
            IsInUnitTest = Assembly.Load("nunit.framework") != null;
        }
        catch (Exception)
        {
            //We don't mind that we errored out, likely that we
            //aren't in a Unit Test
        }
    }

    /// <summary>
    /// If we are currently running in a unit test
    /// </summary>
    public static bool IsInUnitTest { get; }
}