using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomLocaleService(
    ISptLogger<WTTCustomLocaleService> logger,
    DatabaseServer databaseServer,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    private DatabaseTables? _database;

    public async Task CreateCustomLocales(Assembly assembly, string? relativePath = null)
    {
        _database = databaseServer.GetTables();
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomLocales");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            logger.Warning($"Locale directory not found: {finalDir}");
            return;
        }

        var customLocales = await configHelper.LoadLocalesFromDirectory(finalDir);

        if (customLocales.Count == 0)
        {
            logger.Warning("No custom locale files found or loaded");
            return;
        }

        var fallback = customLocales.TryGetValue("en", out var locale) ? locale : customLocales.Values.FirstOrDefault();

        if (fallback == null)
        {
            logger.Warning("No valid fallback locale found");
            return;
        }

        foreach (var (localeCode, lazyLocale) in _database.Locales.Global)
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData is null) return localeData;

                var customLocale = customLocales.GetValueOrDefault(localeCode, fallback);

                foreach (var (key, value) in customLocale) localeData[key] = value;

                return localeData;
            });

        LogHelper.Debug(logger,
            $"WTTCustomLocaleService: Registered transformers for {customLocales.Count} locale files");
    }
}