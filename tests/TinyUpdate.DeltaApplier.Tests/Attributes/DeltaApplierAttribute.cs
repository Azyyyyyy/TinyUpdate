﻿using TinyUpdate.Core.Abstract;

namespace TinyUpdate.DeltaApplier.Tests.Attributes;

/// <summary>
/// The test(s) are testing an <seealso cref="IDeltaApplier"/>
/// </summary>

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeltaApplierAttribute() : CategoryAttribute("Delta Applier");