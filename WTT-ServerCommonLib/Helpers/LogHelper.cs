using SPTarkov.Server.Core.Models.Utils;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace WTTServerCommonLib.Helpers;

public static class LogHelper
{
    public static void Debug<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Debug)) logger.Debug(message);
    }

    public static void Info<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Info)) logger.Info(message);
    }

    public static void Warning<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Warn)) logger.Warning(message);
    }

    public static void Error<T>(ISptLogger<T> logger, string message)
    {
        if (logger.IsLogEnabled(LogLevel.Error)) logger.Error(message);
    }
}