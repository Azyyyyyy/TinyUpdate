using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace TinyUpdate.Core.Tests.Attributes;

/// <summary>
/// This allows us to skip tests if the current operating system is *not* Windows
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class NonWindowsIgnore : Attribute, IApplyToTest
{
    public void ApplyToTest(Test test)
    {
        if (!OperatingSystem.IsWindows() && test.RunState != RunState.NotRunnable)
        {
            test.RunState = RunState.Ignored;
            test.Properties.Set(PropertyNames.SkipReason, "Test can only run on Windows");
        }
    }
}