using System;

namespace WTTClientCommonLib.Common.Helpers;

public static class ResourceHelper
{
    public static void AddEntry(string key, object value)
    {
        if (!CacheResourcesPopAbstractClass.Dictionary_0.ContainsKey(key))
        {
            CacheResourcesPopAbstractClass.Dictionary_0.Add(key, value);
#if DEBUG
            Console.WriteLine($"[WTT-ClientCommonLib] Registered {key}.");
#endif
        }
        else
        {
            Console.WriteLine($"[WTT-ClientCommonLib] Duplicate key ignored: {key}");
        }
    }
}