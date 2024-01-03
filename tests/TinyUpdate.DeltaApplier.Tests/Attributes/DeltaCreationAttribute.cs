using TinyUpdate.Core.Abstract;

namespace TinyUpdate.DeltaApplier.Tests.Attributes;

/// <summary>
/// The test(s) are testing an <seealso cref="IDeltaCreation"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeltaCreationAttribute() : CategoryAttribute("Delta Creation");