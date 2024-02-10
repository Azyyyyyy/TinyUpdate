using TinyUpdate.Core.Abstract;
using TinyUpdate.Core.Abstract.UpdatePackage;
using TinyUpdate.Core.Model;

namespace TinyUpdate.Core.Services;

public class UpdatePackageFactory : IUpdatePackageFactory
{
    private readonly Dictionary<string, Type> _updatePackageTypes = new();
    
    public void AddUpdatePackageType<T>()
        where T : IUpdatePackage
    {
        var updatePackage = Activator.CreateInstance<T>();
        _updatePackageTypes.Add(updatePackage.Extension, typeof(T));
    }
    
    public async Task<LoadUpdatePackageResult> CreateUpdatePackage(Stream? stream, string extension, ReleaseEntry releaseEntry)
    {
        if (stream is not { CanSeek: true, Length: >0 })
        {
            return LoadUpdatePackageResult.Failed("Stream is not usable");
        }
        
        if (!_updatePackageTypes.TryGetValue(extension, out var updatePackageType))
        {
            return LoadUpdatePackageResult.Failed($"We don't have any update package which handles {extension}");
        }

        var updatePackage = Activator.CreateInstance(updatePackageType) as IUpdatePackage;
        if (updatePackage == null)
        {
            return LoadUpdatePackageResult.Failed($"Failed to create an instance of {updatePackageType.Name}, does it contain a parameterless constructor?");
        }

        var loadResult = await updatePackage.Load(stream, releaseEntry);

        return loadResult.Successful 
            ? LoadUpdatePackageResult.Success(updatePackage) 
            : LoadUpdatePackageResult.Failed(loadResult.Message);
    }
}