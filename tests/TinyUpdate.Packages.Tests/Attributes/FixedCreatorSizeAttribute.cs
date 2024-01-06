namespace TinyUpdate.Packages.Tests.Attributes;

public class FixedCreatorSizeAttribute() : PropertyAttribute(PropName, "true")
{
    public const string PropName = "NeedFixedCreatorSize";
}