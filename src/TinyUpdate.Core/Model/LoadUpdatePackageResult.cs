using System.Diagnostics.CodeAnalysis;
using TinyUpdate.Core.Abstract.UpdatePackage;

namespace TinyUpdate.Core.Model;

public record LoadUpdatePackageResult
{
    public LoadUpdatePackageResult(bool successful, string? message, IUpdatePackage? updatePackage)
    {
        Successful = successful;
        Message = message;
        UpdatePackage = updatePackage;
    }
    
    public static LoadUpdatePackageResult Failed(string message) => new(false, message, null);

    public static LoadUpdatePackageResult Success(IUpdatePackage updatePackage) => new(true, null, updatePackage);

    [MemberNotNullWhen(true, nameof(UpdatePackage))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool Successful { get; }

    public string? Message { get; }
    
    public IUpdatePackage? UpdatePackage { get; }
}