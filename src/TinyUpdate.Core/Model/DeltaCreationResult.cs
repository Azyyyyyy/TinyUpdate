﻿using System.Diagnostics.CodeAnalysis;
using TinyUpdate.Core.Abstract.Delta;

namespace TinyUpdate.Core.Model;

/// <summary>
/// Details of creating a new delta update
/// </summary>
public class DeltaCreationResult(IDeltaCreation? creator, Stream? deltaStream, bool successful)
{
    /// <summary>
    /// Failed to create a delta update
    /// </summary>
    public static readonly DeltaCreationResult Failed = new DeltaCreationResult(null, null, false);

    /// <summary>
    /// The <see cref="IDeltaCreation"/> that created the update
    /// </summary>
    public IDeltaCreation? Creator { get; } = creator;

    /// <summary>
    /// The contents of the delta update
    /// </summary>
    public Stream? DeltaStream { get; } = deltaStream;

    /// <summary>
    /// If we was successful in creating a delta update
    /// </summary>
    [MemberNotNullWhen(true, nameof(Creator), nameof(DeltaStream))]
    public bool Successful { get; } = successful;
}