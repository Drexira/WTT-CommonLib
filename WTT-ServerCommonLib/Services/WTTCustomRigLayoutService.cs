using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Logger;

namespace WTTServerCommonLib.Services
{
    [Injectable]
    public class WTTCustomRigLayoutService
    {
        private readonly ModHelper modHelper;
        private readonly SptLogger<WTTCustomRigLayoutService> logger;
        private readonly Dictionary<string, Dictionary<string, string>> _modBundles = new();

        public WTTCustomRigLayoutService(ModHelper modHelper, SptLogger<WTTCustomRigLayoutService> logger)
        {
            this.modHelper = modHelper;
            this.logger = logger;
        }

        public void LoadRigLayouts(Assembly assembly, string? relativePath = null)
        {
            string modKey = assembly.GetName().Name;
            string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            string defaultDir = Path.Combine("db", "CustomRigLayouts");
            string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Info($"No CustomRigLayouts directory at {finalDir} for mod {modKey}");
                return;
            }

            if (!_modBundles.ContainsKey(modKey))
                _modBundles[modKey] = new Dictionary<string, string>();

            foreach (var bundlePath in Directory.GetFiles(finalDir, "*.bundle"))
            {
                string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
                _modBundles[modKey][bundleName] = bundlePath;
                logger.Info($"Registered rig layout: {bundleName} for mod {modKey}");
            }
        }

        public List<string> GetLayoutManifest()
        {
            var allBundles = new List<string>();
            foreach (var modBundles in _modBundles.Values)
            {
                allBundles.AddRange(modBundles.Keys);
            }
            return allBundles;
        }

        public byte[]? GetBundleData(string bundleName)
        {
            foreach (var modBundles in _modBundles.Values)
            {
                if (modBundles.TryGetValue(bundleName, out var path) && File.Exists(path))
                {
                    logger.Info($"Serving bundle {bundleName} from {path}");
                    return File.ReadAllBytes(path);
                }
            }
            logger.Warning($"Bundle {bundleName} not found in any registered mod");
            return null;
        }
    }
}
