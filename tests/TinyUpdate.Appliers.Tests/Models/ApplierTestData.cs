namespace TinyUpdate.Appliers.Tests.Models;

public class ApplierTestData
{
    private readonly string _name;
    public ApplierTestData(string name, MockUpdatePackage updatePackage, string location)
    {
        UpdatePackage = updatePackage;
        Location = location;
        _name = name;
    }
    
    public MockUpdatePackage UpdatePackage { get; }

    public string Location { get; }

    public override string ToString() => _name;
}