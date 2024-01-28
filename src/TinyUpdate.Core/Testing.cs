using System.Reflection;

namespace TinyUpdate.Core;

public static class Testing
{
    public static bool InTestRunner
    {
        get
        {
            var nunitAssemblyName = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .FirstOrDefault(x => x.Name == "nunit.framework");
            if (nunitAssemblyName == null)
            {
                return false;
            }
        
            var contextType = Assembly.Load(nunitAssemblyName).GetType("NUnit.Framework.Internal.TestExecutionContext");
            var currentContextProperty = contextType?.GetProperty("CurrentContext", BindingFlags.Public | BindingFlags.Static);
            var currentContext = currentContextProperty?.GetValue(null);

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