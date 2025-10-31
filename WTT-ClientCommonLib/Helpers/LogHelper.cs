using System;

namespace WTTClientCommonLib.Helpers;

public static class LogHelper
{
    public static BepInEx.Logging.ManualLogSource Logger;

    public static void SetLogger(BepInEx.Logging.ManualLogSource logger)
    {
        Logger = logger;
    }

    public static void LogDebug(string message)
    {
#if DEBUG
        Logger?.LogDebug(message);
#endif
    }

    public static void LogInfo(string message)
    {
        Logger?.LogInfo(message);
    }

    public static void LogError(string message)
    {
        Logger?.LogError(message);
    }
}
