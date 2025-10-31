using SPTarkov.Server.Core.Models.Utils;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace WTTServerCommonLib.Helpers;

public static class LogHelper
{
    public static void Debug<T>(ISptLogger<T> logger, string message)
    {
#if DEBUG
        logger.Info(message);
#endif
    }
}