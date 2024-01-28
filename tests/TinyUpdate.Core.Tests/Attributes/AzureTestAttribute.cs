using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace TinyUpdate.Core.Tests.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AzureTestAttribute : Attribute, IApplyToTest
{
    public void ApplyToTest(Test test)
    {
        if (Environment.GetEnvironmentVariable("AZURE_DEVOPS_EXT_PAT") == null)
        {
            test.RunState = RunState.Skipped;
            test.Properties.Set(PropertyNames.SkipReason, "Unable to run Azure Test without AZURE_DEVOPS_EXT_PAT set");
        }
    }
}