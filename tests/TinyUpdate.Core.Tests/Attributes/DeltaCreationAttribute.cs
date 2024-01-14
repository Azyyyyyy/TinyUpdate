using TinyUpdate.Core.Abstract.Delta;

namespace TinyUpdate.Core.Tests.Attributes;

/// <summary>
/// The test(s) are testing an <seealso cref="IDeltaCreation"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeltaCreationAttribute() : CategoryAttribute("Delta Creation");