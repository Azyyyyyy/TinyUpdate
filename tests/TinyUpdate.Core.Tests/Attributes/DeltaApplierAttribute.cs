using TinyUpdate.Core.Abstract.Delta;

namespace TinyUpdate.Core.Tests.Attributes;

/// <summary>
/// The test(s) are testing an <seealso cref="IDeltaApplier"/>
/// </summary>

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeltaApplierAttribute() : CategoryAttribute("Delta Applier");