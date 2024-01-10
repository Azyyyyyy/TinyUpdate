using System.Collections;
using System.Globalization;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace TinyUpdate.Core.Tests.Attributes;

//Readopted TestCaseSourceAttribute to make this
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DynamicTestCaseSourceAttribute : Attribute, ITestBuilder
{
    private readonly NUnitTestCaseBuilder _builder = new();

    public DynamicTestCaseSourceAttribute(string testName, Type sourceType, string methodName, object?[]? parameters = null)
    {
        TestName = testName;
        SourceType = sourceType;
        MethodName = methodName;
        Parameters = parameters;
    }

    public string TestName { get; }

    public Type SourceType { get; }

    public string MethodName { get; }

    public object?[]? Parameters { get; }

    public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
    {
        if (method.Name != TestName) yield break;

        var count = 0;
        foreach (var parms in GetTestCasesFor(method))
        {
            count++;
            yield return _builder.BuildTestMethod(method, suite, parms);
        }

        // If count > 0, error messages will be shown for each case
        // but if it's 0, we need to add an extra "test" to show the message.
        if (count == 0 && method.GetParameters().Length == 0)
        {
            var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
            parms.Properties.Set(PropertyNames.SkipReason, "DynamicTestCaseSource may not be used on a method without parameters");

            yield return _builder.BuildTestMethod(method, suite, parms);
        }
    }

    private IEnumerable<TestCaseParameters> GetTestCasesFor(IMethodInfo method)
    {
        var methodEnumerable = SourceType.GetMethod(MethodName)?.Invoke(null, BindingFlags.Public | BindingFlags.Static,
            null, Parameters, CultureInfo.CurrentCulture) as IEnumerable;
        if (methodEnumerable == null)
        {
            var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
            parms.Properties.Set(PropertyNames.SkipReason, "DynamicTestCaseSource can't find the method to invoke");

            yield return parms;
            yield break;
        }

        foreach (var item in methodEnumerable)
        {
            // First handle two easy cases:
            // 1. Source is null. This is really an error but if we
            //    throw an exception we simply get an invalid fixture
            //    without good info as to what caused it. Passing a
            //    single null argument will cause an error to be
            //    reported at the test level, in most cases.
            // 2. User provided an TestCaseParameters and we just use it.
            var parms = item is null
                ? new TestCaseParameters(new object?[] { null })
                : item as TestCaseParameters;

            if (parms is not null)
            {
                yield return parms;
                continue;
            }

            object?[]? args = null;

            // 3. An array was passed, it may be an object[]
            //    or possibly some other kind of array, which
            //    TestCaseSource can accept.
            if (item is Array array)
            {
                // If array has the same number of elements as parameters
                // and it does not fit exactly into single existing parameter
                // we believe that this array contains arguments, not is a bare
                // argument itself.
                var parameters = method.GetParameters();
                var argsNeeded = parameters.Length;
                if (argsNeeded > 0 && argsNeeded == array.Length && parameters[0].ParameterType != array.GetType())
                {
                    args = new object?[array.Length];
                    for (var i = 0; i < array.Length; i++)
                        args[i] = array.GetValue(i);
                }
            }

            args ??= new[] { item };
            yield return new TestCaseParameters(args);
        }
    }
}