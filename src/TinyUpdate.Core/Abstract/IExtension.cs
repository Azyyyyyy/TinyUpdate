namespace TinyUpdate.Core.Abstract;

/// <summary>
/// Declares that an extension is used
/// </summary>
public interface IExtension
{
    /// <summary>
    /// Extension to detect
    /// </summary>
    public string Extension { get; }
}