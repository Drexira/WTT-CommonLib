﻿using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;
using WTTServerCommonLib.Constants;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services.ItemServiceHelpers;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomItemServiceExtended(
    ISptLogger<WTTCustomItemServiceExtended> logger,
    CustomItemService customItemService,
    DatabaseServer databaseServer,
    ModHelper modHelper,
    WeaponPresetHelper weaponPresetHelper,
    TraderItemHelper traderItemHelper,
    StaticLootHelper staticLootHelper,
    SpecialSlotsHelper specialSlotsHelper,
    PosterLootHelper posterLootHelper,
    ModSlotHelper modSlotHelper,
    MasteryHelper masteryHelper,
    InventorySlotHelper inventorySlotHelper,
    HideoutStatuetteHelper hideoutStatuetteHelper,
    HideoutPosterHelper hideoutPosterHelper,
    HallOfFameHelper hallOfFameHelper,
    GeneratorFuelHelper generatorFuelHelper,
    CaliberHelper caliberHelper,
    BotLootHelper botLootHelper,
    ConfigHelper configHelper,
    StaticAmmoHelper staticAmmoHelper,
    EmptyPropSlotHelper emptyPropSlotHelper
)
{
    private readonly List<(string newItemId, CustomItemConfig config)> _deferredModSlotConfigs = new();
    private DatabaseTables? _database;

    /// <summary>
    /// Loads custom item configurations from JSON/JSONC files and creates items with all associated properties.
    /// 
    /// Items are loaded from the mod's "db/CustomItems" directory (or a custom path if specified).
    /// Each item is cloned from a base template and can be configured with traders, presets, masteries, slots, loot tables, and more.
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomItems(Assembly assembly, string? relativePath = null)

    {
        if (_database == null) _database = databaseServer.GetTables();

        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomItems");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Error($"Directory not found at {finalDir}");
                return;
            }

            var itemConfigDicts = await configHelper.LoadAllJsonFiles<Dictionary<string, CustomItemConfig>>(finalDir);

            if (itemConfigDicts.Count == 0)
            {
                logger.Warning($"No valid item configs found in {finalDir}");
                return;
            }

            var totalItemsCreated = 0;

            foreach (var configDict in itemConfigDicts)
            foreach (var (itemId, configData) in configDict)
            {
                configData.Validate();
                if (CreateItemFromConfig(itemId, configData))
                    totalItemsCreated++;
            }

            LogHelper.Debug(logger, $"Created {totalItemsCreated} custom items from {itemConfigDicts.Count} files");
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading configs: {ex.Message}");
        }
    }

    private bool CreateItemFromConfig(string newItemId, CustomItemConfig config)
    {
        try
        {
            var itemDetails = new NewItemFromCloneDetails
            {
                ItemTplToClone = ItemTplResolver.ResolveId(config.ItemTplToClone),
                ParentId = NameHelper.ResolveId(config.ParentId, ItemMaps.ItemBaseClassMap),
                NewId = newItemId,
                FleaPriceRoubles = config.FleaPriceRoubles,
                HandbookPriceRoubles = config.HandbookPriceRoubles,
                HandbookParentId = NameHelper.ResolveId(config.HandbookParentId, ItemMaps.ItemHandbookCategoryMap),
                Locales = config.Locales,
                OverrideProperties = config.OverrideProperties
            };

            customItemService.CreateItemFromClone(itemDetails);
            LogHelper.Debug(logger, $"Created item {newItemId}");

            ProcessAdditionalProperties(newItemId, config);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create item {newItemId}: {ex.Message}");
            return false;
        }
    }

    private void ProcessAdditionalProperties(string newItemId, CustomItemConfig config)
    {
        if (_database == null) return;
        if (config is { AddToTraders: true, Traders: not null })
            traderItemHelper.AddItem(config, newItemId);

        if (config.AddWeaponPreset == true)
            weaponPresetHelper.ProcessWeaponPresets(config, newItemId);

        if (config is { Masteries: true, MasterySections: not null })
            masteryHelper.AddOrUpdateMasteries(config.MasterySections, newItemId);

        if (config.AddToModSlots == true)
            AddDeferredModSlot(newItemId, config);

        if (config.AddToInventorySlots != null)
            inventorySlotHelper.ProcessInventorySlots(config, newItemId);

        if (config.AddToHallOfFame == true)
            hallOfFameHelper.AddToHallOfFame(config, newItemId);

        if (config.AddToSpecialSlots == true)
            specialSlotsHelper.AddToSpecialSlots(config, newItemId);

        if (config is { AddToStaticLootContainers: true, StaticLootContainers: not null })
            staticLootHelper.ProcessStaticLootContainers(config, newItemId);

        if (config.AddToBots == true)
            botLootHelper.AddToBotLoot(config, newItemId);

        if (config.AddCaliberToAllCloneLocations == true)
            caliberHelper.AddNewCaliberToItems(config, newItemId);

        if (config is { AddToGeneratorAsFuel: true, GeneratorFuelSlotStages: not null })
            generatorFuelHelper.AddGeneratorFuel(config, newItemId);

        if (config.AddToHideoutPosterSlots == true)
            hideoutPosterHelper.AddToPosterSlot(newItemId);

        if (config is { AddPosterToMaps: true, PosterSpawnProbability: not null })
            posterLootHelper.ProcessPosterLoot(config, newItemId);

        if (config.AddToStatuetteSlots == true)
            hideoutStatuetteHelper.AddToStatuetteSlot(newItemId);

        if (config.AddToStaticAmmo == true)
            staticAmmoHelper.AddAmmoToLocationStaticAmmo(config, newItemId);
        
        if (config.AddToEmptyPropSlots == true)
            emptyPropSlotHelper.AddCustomSlots(config, newItemId);
    }

    private void AddDeferredModSlot(string newItemId, CustomItemConfig config)
    {
        if (_deferredModSlotConfigs.Any(d => d.newItemId == newItemId))
        {
            logger.Warning($"Deferred modslot for {newItemId} already exists, skipping.");
            return;
        }

        _deferredModSlotConfigs.Add((newItemId, config));
    }

    public void ProcessDeferredModSlots()
    {
        if (_deferredModSlotConfigs.Count == 0)
        {
            LogHelper.Debug(logger, "No deferred modslots to process");
            return;
        }

        LogHelper.Debug(logger, $"Processing {_deferredModSlotConfigs.Count} deferred modslots...");

        foreach (var (newItemId, config) in _deferredModSlotConfigs)
            try
            {
                if (_database == null) return;
                modSlotHelper.ProcessModSlots(config, newItemId);

                if (logger.IsLogEnabled(LogLevel.Debug)) LogHelper.Debug(logger, $"Processed modslots for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing modslots for {newItemId}", ex);
            }

        _deferredModSlotConfigs.Clear();

        LogHelper.Debug(logger, "Finished processing deferred modslots");
    }
}