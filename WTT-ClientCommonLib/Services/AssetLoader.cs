using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Quests;
using Newtonsoft.Json;
using UnityEngine;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib.Services;

public class AssetLoader(ManualLogSource logger)
{
    private bool _initialized;
    private readonly Dictionary<string, AssetBundle> _loadedBundles = new();

    public List<CustomSpawnConfig> SpawnConfigs = null;

    public List<CustomSpawnConfig> FetchSpawnConfigs(string route)
    {
        try
        {
            return Utils.Get<List<CustomSpawnConfig>>(route)
                   ?? new List<CustomSpawnConfig>();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error fetching configs: {ex}");
            return new List<CustomSpawnConfig>();
        }
    }

    public void ProcessSpawnConfig(Player player, CustomSpawnConfig config, string locationID)
    {
        try
        {
            if (string.IsNullOrEmpty(config.PrefabName) ||
                string.IsNullOrEmpty(config.BundleName) ||
                string.IsNullOrEmpty(config.LocationID))
            {
                LogHelper.LogDebug($"[WTT-Armory] Invalid config: {JsonConvert.SerializeObject(config)}");
                return;
            }

            // Location check
            if (!locationID.Equals(config.LocationID, StringComparison.OrdinalIgnoreCase))
                return;

            // Get quest reference
            QuestDataClass quest = null;
            if (!string.IsNullOrEmpty(config.QuestId))
                quest = player.Profile.QuestsData.FirstOrDefault(q => q.Id == config.QuestId);

            // Evaluate conditions (all must pass)
            if (!EvaluateConditions(player, quest, config))
            {
                LogHelper.LogDebug($"[WTT-Armory] Conditions not met for {config.PrefabName}");
                return;
            }

            // Load and spawn prefab
            var prefab = LoadPrefabFromBundle(config.BundleName, config.PrefabName);
            if (prefab == null) return;

            var rotation = Quaternion.Euler(config.Rotation);
            SpawnPrefab(prefab, config.Position, rotation);
            LogHelper.LogDebug($"[WTT-Armory] Spawned {config.PrefabName}");
        }
        catch (Exception ex)
        {
            logger.LogError($"[WTT-Armory] Config processing failed: {ex}");
        }
    }

    private bool EvaluateConditions(Player player, QuestDataClass quest, CustomSpawnConfig config)
    {
        // Quest existence check
        if (config.QuestMustExist.HasValue)
        {
            var exists = quest != null;
            if (exists != config.QuestMustExist.Value)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Quest existence check failed. Expected: {config.QuestMustExist}, Actual: {exists}");
                return false;
            }
        }

        // Required quest statuses (multiple)
        if (config.RequiredQuestStatuses is { Count: > 0 })
        {
            if (quest == null)
            {
                LogHelper.LogDebug("[CONDITION] Required statuses but quest doesn't exist");
                return false;
            }

            var anyMatch = false;
            var validStatuses = new List<string>();

            foreach (var statusStr in config.RequiredQuestStatuses)
                if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                {
                    validStatuses.Add(statusStr);
                    if (quest.Status == requiredStatus) anyMatch = true;
                }

            if (!anyMatch)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] None of required statuses matched: {string.Join(", ", validStatuses)}. Actual: {quest.Status}");
                return false;
            }
        }

        // Excluded quest statuses (multiple)
        if (config.ExcludedQuestStatuses is { Count: > 0 })
            if (quest != null)
                foreach (var statusStr in config.ExcludedQuestStatuses)
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                        if (quest.Status == excludedStatus)
                        {
                            LogHelper.LogDebug($"[CONDITION] Excluded status matched: {excludedStatus}");
                            return false;
                        }

        // Required item in inventory
        if (!string.IsNullOrEmpty(config.RequiredItemInInventory))
        {
            var hasItem = player.Profile.Inventory.AllRealPlayerItems
                .Any(item => item.TemplateId == config.RequiredItemInInventory);

            if (!hasItem)
            {
                LogHelper.LogDebug($"[CONDITION] Missing required item: {config.RequiredItemInInventory}");
                return false;
            }
        }

        // Required level
        if (config.RequiredLevel.HasValue)
            if (player.Profile.Info.Level < config.RequiredLevel.Value)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Level too low. Required: {config.RequiredLevel}, Actual: {player.Profile.Info.Level}");
                return false;
            }

        // Required faction
        if (!string.IsNullOrEmpty(config.RequiredFaction))
        {
            var playerFaction = player.Profile.Side.ToString();
            if (!playerFaction.Equals(config.RequiredFaction, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Wrong faction. Required: {config.RequiredFaction}, Actual: {playerFaction}");
                return false;
            }
        }

        // Enhanced linked quest condition
        if (!string.IsNullOrEmpty(config.LinkedQuestId))
        {
            var linkedQuest = player.Profile.QuestsData.FirstOrDefault(q => q.Id == config.LinkedQuestId);

            // Existence check
            if (config.LinkedQuestMustExist.HasValue)
            {
                var linkedExists = linkedQuest != null;
                if (linkedExists != config.LinkedQuestMustExist.Value)
                {
                    LogHelper.LogDebug(
                        $"[CONDITION] Linked quest existence check failed. Expected: {config.LinkedQuestMustExist}, Actual: {linkedExists}");
                    return false;
                }
            }

            // Linked required statuses (multiple)
            if (config.LinkedRequiredStatuses is { Count: > 0 })
            {
                if (linkedQuest == null)
                {
                    LogHelper.LogDebug("[CONDITION] Required linked statuses but quest doesn't exist");
                    return false;
                }

                var anyMatch = false;
                var validStatuses = new List<string>();

                foreach (var statusStr in config.LinkedRequiredStatuses)
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                    {
                        validStatuses.Add(statusStr);
                        if (linkedQuest.Status == requiredStatus) anyMatch = true;
                    }

                if (!anyMatch)
                {
                    LogHelper.LogDebug(
                        $"[CONDITION] None of linked required statuses matched: {string.Join(", ", validStatuses)}. Actual: {linkedQuest?.Status}");
                    return false;
                }
            }

            // Linked excluded statuses (multiple)
            if (config.LinkedExcludedStatuses is { Count: > 0 })
                if (linkedQuest != null)
                    foreach (var statusStr in config.LinkedExcludedStatuses)
                        if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                            if (linkedQuest.Status == excludedStatus)
                            {
                                LogHelper.LogDebug($"[CONDITION] Linked excluded status matched: {excludedStatus}");
                                return false;
                            }
        }

        // Boss spawn detection
        if (!string.IsNullOrEmpty(config.RequiredBossSpawned))
            if (!CheckBossSpawned(config.RequiredBossSpawned))
            {
                LogHelper.LogDebug($"[CONDITION] Required boss not spawned: {config.RequiredBossSpawned}");
                return false;
            }


        // All conditions passed
        return true;
    }

    private bool CheckBossSpawned(string bossName)
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                LogHelper.LogDebug("[BOSS] GameWorld instance not found");
                return false;
            }

            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                if (player.AIData?.BotOwner?.Profile?.Info?.Settings?.Role == null)
                    continue;

                var roleName = player.AIData.BotOwner.Profile.Info.Settings.Role.ToString();

                if (roleName.Equals(bossName, StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.LogDebug($"[BOSS] Found {bossName} at {player.Transform.position}");
                    return true;
                }
            }

            LogHelper.LogDebug($"[BOSS] {bossName} not found in raid");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError($"[BOSS] Detection failed: {ex}");
            return false;
        }
    }

    public void InitializeBundles(string route)
    {
        if (_initialized) return;
        _initialized = true;

        LogHelper.LogDebug("Fetching bundles from server...");
        var map = Utils.Get<Dictionary<string, string>>(route)
                  ?? new Dictionary<string, string>();

        foreach (var kvp in map)
        {
            var rawName = kvp.Key;
            var key = rawName.ToLowerInvariant().Trim();
            var data = kvp.Value;

            if (string.IsNullOrEmpty(data))
            {
                logger.LogWarning($"Empty data for bundle '{rawName}'");
                continue;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(data);
            }
            catch (Exception ex)
            {
                logger.LogError($"Base64 decode failed for '{rawName}': {ex}");
                continue;
            }

            try
            {
                var bundle = AssetBundle.LoadFromMemory(bytes);
                if (bundle == null)
                {
                    logger.LogError($"Failed to load bundle '{rawName}' from memory");
                    continue;
                }

                _loadedBundles[key] = bundle;
                LogHelper.LogDebug($"Loaded bundle '{key}' ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading bundle '{rawName}': {ex}");
            }
        }
    }

    public GameObject LoadPrefabFromBundle(string bundleName, string assetName)
    {
        if (!_initialized) InitializeBundles("/wttcommonlib/spawnsystem/bundles/get");

        var key = bundleName.ToLowerInvariant().Trim();
        if (!_loadedBundles.TryGetValue(key, out var bundle))
        {
            logger.LogError(
                $"[ASSET LOADER] Bundle not in cache: '{bundleName}' (tried '{key}')");
            return null;
        }

        var prefab = bundle.LoadAsset<GameObject>(assetName);
        if (prefab == null) logger.LogError($"[ASSET LOADER] Prefab '{assetName}' not in bundle '{key}'");

        return prefab;
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        try
        {
            Object.Instantiate(prefab, position, rotation);
            LogHelper.LogDebug($"[SPAWNER] Created {prefab.name} at {position}");
        }
        catch (Exception ex)
        {
            logger.LogError($"[SPAWNER] Instantiation failed: {ex}");
        }
    }

    public void UnloadAllBundles()
    {
        if (_loadedBundles.Count == 0) return;
        foreach (var bundle in _loadedBundles.Values) bundle.Unload(true);
        _loadedBundles.Clear();
    }
}