using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class WTTCustomLootspawnService(
    DatabaseService databaseService,
    ConfigHelper configHelper,
    ModHelper modHelper
    )
{
    private const double Epsilon = 0.0001;

    public void CreateCustomLootSpawns(Assembly assembly, string? relativePath = null)
    {
        string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        string baseDir = Path.Combine(assemblyLocation, relativePath ?? Path.Combine("db", "CustomLootspawns"));

        string spawnDir = Path.Combine(baseDir, "CustomSpawnpoints");
        string forcedDir = Path.Combine(baseDir, "CustomSpawnpointsForced");

        var locations = databaseService.GetLocations().GetDictionary();

        ProcessSpawnDirectory(spawnDir, locations, forced: false);
        ProcessSpawnDirectory(forcedDir, locations, forced: true);
    }

    private void ProcessSpawnDirectory(string directory, Dictionary<string, Location> locations, bool forced)
    {
        if (!Directory.Exists(directory))
            return;

        var spawnDicts = configHelper.LoadAllJsonFiles<Dictionary<string, List<Spawnpoint>>>(directory);

        foreach (var spawns in spawnDicts)
        {
            foreach (var (mapName, spawnList) in spawns)
            {
                string locationId = databaseService.GetLocations().GetMappedKey(mapName);
                if (!locations.TryGetValue(locationId, out var location)) continue;
                if (location.LooseLoot == null) continue;

                location.LooseLoot.AddTransformer(looseLoot =>
                {
                    if (looseLoot == null) return looseLoot;
                    if (forced)
                        looseLoot.SpawnpointsForced = MergeForced(looseLoot.SpawnpointsForced, spawnList);
                    else
                        looseLoot.Spawnpoints = MergeGeneral(looseLoot.Spawnpoints, spawnList);
                    return looseLoot;
                });
            }
        }
    }

    private static List<Spawnpoint> MergeForced(IEnumerable<Spawnpoint>? existingForced, List<Spawnpoint> newSpawns)
    {
        var existing = existingForced?.ToList() ?? new();
        foreach (var newSpawn in newSpawns)
        {
            if (existing.All(sp => sp.LocationId != newSpawn.LocationId))
                existing.Add(newSpawn);
        }
        return existing;
    }

    private static List<Spawnpoint> MergeGeneral(IEnumerable<Spawnpoint>? existingPoints, List<Spawnpoint> newSpawns)
    {
        var existing = existingPoints?.ToList() ?? new();
        foreach (var custom in newSpawns)
        {
            var match = existing.FirstOrDefault(sp => sp.LocationId == custom.LocationId);
            if (match == null)
            {
                existing.Add(custom);
                continue;
            }

            match.Probability = custom.Probability;
            MergeSpawnpoint(match, custom);
        }
        return existing;
    }

    private static void MergeSpawnpoint(Spawnpoint existing, Spawnpoint custom)
    {
        if (custom.Template != null)
        {
            existing.Template ??= new SpawnpointTemplate();
            existing.Template.IsContainer = custom.Template.IsContainer;
            existing.Template.UseGravity = custom.Template.UseGravity;
            existing.Template.RandomRotation = custom.Template.RandomRotation;

            if (custom.Template.Items != null)
            {
                var items = existing.Template.Items?.ToList() ?? new List<SptLootItem>();
                foreach (var item in custom.Template.Items)
                    if (items.All(i => i.Id != item.Id))
                        items.Add(item);
                existing.Template.Items = items;
            }

            if (custom.Template.GroupPositions != null)
            {
                var groups = existing.Template.GroupPositions?.ToList() ?? new List<GroupPosition>();
                foreach (var group in custom.Template.GroupPositions)
                {
                    bool exists = groups.Any(g =>
                        AreEqual(g.Position?.X, group.Position?.X) &&
                        AreEqual(g.Position?.Y, group.Position?.Y) &&
                        AreEqual(g.Position?.Z, group.Position?.Z));

                    if (!exists) groups.Add(group);
                }
                existing.Template.GroupPositions = groups;
            }
        }

        if (custom.ItemDistribution == null) return;

        var dists = existing.ItemDistribution?.ToList() ?? new List<LooseLootItemDistribution>();
        foreach (var dist in custom.ItemDistribution)
            if (dists.All(d => d.ComposedKey?.Key != dist.ComposedKey?.Key))
                dists.Add(dist);

        existing.ItemDistribution = dists;
    }

    private static bool AreEqual(double? a, double? b)
    {
        if (a == null || b == null) return Equals(a, b);
        return Math.Abs(a.Value - b.Value) < Epsilon;
    }
}
