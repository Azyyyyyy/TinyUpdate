using System.Diagnostics.CodeAnalysis;

namespace TinyUpdate.Core.Abstract.UpdatePackage;

public record LoadResult
{
    public static LoadResult Failed(string message) => new() { Successful = false, Message = message };

    public static readonly LoadResult Success = new() { Successful = true };

    [MemberNotNullWhen(false, nameof(Message))]
    public bool Successful { get; protected init; }

    public string? Message { get; protected init; }
}