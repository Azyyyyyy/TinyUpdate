namespace TinyUpdate.TUUP;

//TODO: Add Loader option with V1 setting?
/// <summary>
/// Options to make update package creation behave differently
/// </summary>
public class TuupUpdatePackageCreatorOptions
{
    /// <summary>
    /// If the update package created should be fully compatible with V1 of Tuup format
    /// </summary>
    public bool V1Compatible { get; init; }
}