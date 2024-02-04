using System.Reflection;

namespace TinyUpdate.Core;

public static class Testing
{
    public static bool InTestRunner
    {
        get
        {
            //First see if the assembly references nunit (used for all our testing)
            var nunitAssemblyName = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .FirstOrDefault(x => x.Name == "nunit.framework");
            if (nunitAssemblyName == null)
            {
                return false;
            }
        
            //Get the current context, this *should* always return something
            var contextType = Assembly.Load(nunitAssemblyName).GetType("NUnit.Framework.Internal.TestExecutionContext");
            var currentContextProperty = contextType?.GetProperty("CurrentContext", BindingFlags.Public | BindingFlags.Static);
            var currentContext = currentContextProperty?.GetValue(null);

            //Get StartTicks, this will contain a value (over 0) if we're actually within a test runner and running a test.
            var startTicksProperty = currentContext?.GetType().GetProperty("StartTicks", BindingFlags.Public | BindingFlags.Instance);
            var startTicksObj = startTicksProperty?.GetValue(currentContext);

            if (startTicksObj is long startTicks)
            {
                return startTicks > 0;
            }

            return false;
        }
    }
}