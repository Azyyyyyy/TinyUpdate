using Spectre.Console;

namespace TinyUpdate.Core.Logging.Loggers.Console;

public interface IColouredOutput
{
    /// <summary>
    /// What the colour should be if do an output for this
    /// </summary>
    public Color? Colour { get; }
}