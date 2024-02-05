namespace TinyUpdate.Appliers.Tests.Models;

public class ApplierTestData(string name, MockUpdatePackage updatePackage, string location)
{
    public MockUpdatePackage UpdatePackage { get; } = updatePackage;

    public string Location { get; } = location;

    public override string ToString() => name;
}