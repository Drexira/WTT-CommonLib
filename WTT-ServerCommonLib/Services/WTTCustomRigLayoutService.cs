﻿using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomRigLayoutService(ModHelper modHelper, ISptLogger<WTTCustomRigLayoutService> logger)
{
    private readonly Dictionary<string, Dictionary<string, string>> _modBundles = [];

    public void CreateRigLayouts(Assembly assembly, string? relativePath = null)
    {
        var modKey = assembly.GetName().Name ?? string.Empty;
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomRigLayouts");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            LogHelper.Debug(logger, $"No CustomRigLayouts directory at {finalDir} for mod {modKey}");
            return;
        }

        if (!_modBundles.ContainsKey(modKey))
            _modBundles[modKey] = new Dictionary<string, string>();

        foreach (var bundlePath in Directory.GetFiles(finalDir, "*.bundle"))
        {
            var bundleName = Path.GetFileNameWithoutExtension(bundlePath);
            _modBundles[modKey][bundleName] = bundlePath;
            LogHelper.Debug(logger, $"Registered rig layout: {bundleName} for mod {modKey}");
        }
    }

    public List<string> GetLayoutManifest()
    {
        var allBundles = new List<string>();
        foreach (var modBundles in _modBundles.Values) allBundles.AddRange(modBundles.Keys);
        return allBundles;
    }

    public async Task<byte[]?> GetBundleData(string bundleName)
    {
        foreach (var bundles in _modBundles.Values)
        {
            if (!bundles.TryGetValue(bundleName, out var path))
                continue;

            if (!File.Exists(path))
                continue;

            LogHelper.Debug(logger, $"Serving bundle {bundleName} from {path}");
            return await File.ReadAllBytesAsync(path);
        }

        logger.Warning($"Bundle {bundleName} not found in any registered mod");
        return null;
    }
}