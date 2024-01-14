namespace TinyUpdate.Desktop.Abstract;

public interface INative
{
    /// <summary>
    /// Creates a hard link for a file
    /// </summary>
    /// <param name="sourcePath">Where the file that is going to be linked is</param>
    /// <param name="targetPath">where we want the link to be</param>
    /// <returns>If we was able to make the hard link</returns>
    bool CreateHardLink(string sourcePath, string targetPath);
}