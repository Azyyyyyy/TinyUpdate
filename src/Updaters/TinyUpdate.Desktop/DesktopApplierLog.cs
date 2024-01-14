using Microsoft.Extensions.Logging;

namespace TinyUpdate.Desktop;

internal static partial class DesktopApplierLog
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "We have a file mis-match! (Hash is not what is expected - {FilePath})")]
    public static partial void FileHashMisMatch(
        this ILogger logger, string filePath);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "We have a file mis-match! (Filesize is not what is expected - {FilePath})")]
    public static partial void FilesizeMisMatch(
        this ILogger logger, string filePath);
}