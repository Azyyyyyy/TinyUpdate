using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace TinyUpdate.Core.Tests.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ExternalTestAttribute : Attribute, ITestBuilder, IImplyFixture
{
    private static readonly NUnitTestCaseBuilder _builder = new();

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
    {
        var testCaseSource = suite?.GetCustomAttributes<DynamicTestCaseSourceAttribute>(true).FirstOrDefault(x => x.TestName == method.Name);
        if (testCaseSource == null)
        {
            var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
            parms.Properties.Set(PropertyNames.SkipReason, $"'{suite?.Name}' does not have a DynamicTestCaseSourceAttribute for this test");

            return [ _builder.BuildTestMethod(method, suite, parms) ];
        }
        
        return testCaseSource.BuildFrom(method, suite);
    }
}