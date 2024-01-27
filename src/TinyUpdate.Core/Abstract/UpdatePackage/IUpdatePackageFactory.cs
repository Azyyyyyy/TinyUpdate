namespace TinyUpdate.Core.Abstract.UpdatePackage;

public interface IUpdatePackageFactory
{
    Task<LoadUpdatePackageResult> CreateUpdatePackage(Stream? stream, string extension, ReleaseEntry releaseEntry);
}