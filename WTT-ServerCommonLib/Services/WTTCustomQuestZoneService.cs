using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Logger;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
    public class WTTCustomQuestZoneService(
        ModHelper modHelper,
        SptLogger<WTTCustomQuestZoneService> logger,
        ConfigHelper configHelper)
    {
        private readonly List<CustomQuestZone> _zones = new();
        private readonly Lock _lock = new Lock();

        public void CreateCustomQuestZones(Assembly assembly, string? relativePath = null)
        {
            string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            string defaultDir = Path.Combine("db", "CustomQuestZones");
            string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);
            
            if (!Directory.Exists(finalDir))
            {
                logger.Info($"No CustomQuestZones directory at {finalDir}");
                return;
            }

            var zones = LoadZoneFiles(finalDir);
            RegisterZones(zones);
        }

        public void RegisterZones(IEnumerable<CustomQuestZone> zones)
        {
            lock (_lock)
            {
                List<CustomQuestZone> collection = zones.ToList();
                _zones.AddRange(collection);
                logger.Info($"Registered {collection.Count()} zones. Total zones: {_zones.Count}");
            }
        }

        public void RegisterZone(CustomQuestZone zone)
        {
            lock (_lock)
            {
                _zones.Add(zone);
                logger.Info($"Registered zone: {zone.ZoneName}. Total zones: {_zones.Count}");
            }
        }

        private List<CustomQuestZone> LoadZoneFiles(string directory)
        {
            var loadedZones = new List<CustomQuestZone>();

            var zoneLists = configHelper.LoadAllJsonFiles<List<CustomQuestZone>>(directory); 

            foreach (var fileZones in zoneLists)
            {
                if (fileZones.Count > 0)
                {
                    loadedZones.AddRange(fileZones);
                    logger.Info($"Loaded {fileZones.Count} zones from a file");
                }
            }

            return loadedZones;
        }

        internal IReadOnlyList<CustomQuestZone> GetZones()
        {
            lock (_lock)
            {
                return _zones.AsReadOnly();
            }
        }
    }
}