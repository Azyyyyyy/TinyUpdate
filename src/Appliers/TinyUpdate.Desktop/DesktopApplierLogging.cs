using Microsoft.Extensions.Logging;

namespace TinyUpdate.Desktop;

/// <summary>
/// Contains all the logging that <see cref="DesktopApplier"/> will output
/// </summary>
internal static partial class DesktopApplierLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "We have a file mis-match! (Hash is not what is expected - {FilePath})")]
    public static partial void FileHashMisMatch(
        this ILogger logger, string filePath);
    
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "We have a file mis-match! (Filesize is not what is expected - {FilePath})")]
    public static partial void FilesizeMisMatch(
        this ILogger logger, string filePath);
    
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "We have not been given the previous version location")]
    public static partial void NoPreviousVersion(
        this ILogger logger);
    
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "{NewLocation} has no link to it's previous location")]
    public static partial void NoPreviousLocation(
        this ILogger logger, string newLocation);
    
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Failed to apply the delta file for {NewPath}")]
    public static partial void FailedDeltaApply(
        this ILogger logger, string newPath);
    
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Was unable to hard link {PreviousPath} to {NewPath}, going to copy file")]
    public static partial void HardLinkFailed(
        this ILogger logger, string previousPath, string newPath);
}