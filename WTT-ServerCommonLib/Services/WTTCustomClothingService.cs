﻿using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class WTTCustomClothingService(
    ISptLogger<WTTCustomClothingService> logger,
    DatabaseService databaseService,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    private DatabaseTables? _database;

    public void CreateCustomClothing(Assembly assembly, string? relativePath = null)
    {
        try
        {
            string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            string defaultDir = Path.Combine("db", "CustomClothing");
            string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (_database == null)
            {
                _database = databaseService.GetTables();
            }

            if (!Directory.Exists(finalDir))
            {
                logger.Warning($"Clothing directory not found at {finalDir}");
                return;
            }

            var clothingConfigsList = configHelper.LoadAllJsonFiles<List<CustomClothingConfig>>(finalDir);

            if (clothingConfigsList.Count == 0)
            {
                logger.Warning($"No valid clothing configs found in {finalDir}");
                return;
            }

            int totalClothingCreated = 0;
            foreach (var configList in clothingConfigsList)
            {
                foreach (var config in configList)
                {
                    if (ProcessClothingConfig(config))
                    {
                        totalClothingCreated++;
                    }
                }
            }

            LogHelper.Debug(logger,$"Created {totalClothingCreated} custom clothing items from {clothingConfigsList.Count} files");
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading clothing configs: {ex.Message}");
        }
    }

    private bool ProcessClothingConfig(CustomClothingConfig config)
    {
        try
        {
            if (_database == null)
            {
                logger.Error("Database not initialized");
                return false;
            }

            return config.Type?.ToLower() switch
            {
                "top" => AddTop(config),
                "bottom" => AddBottom(config),
                _ => throw new InvalidOperationException($"Unknown clothing type: {config.Type}")
            };
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to process clothing config: {ex.Message}");
            return false;
        }
    }

    private bool AddTop(CustomClothingConfig config)
    {
        try
        {
            if (_database == null) return false;

            // Create top customization item
            var topItem = new CustomizationItem
            {
                Id = config.TopId!,
                Name = $"{config.TopId}_name",
                Parent = "5cc0868e14c02e000c6bea68",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.TopId}_desc",
                    Name = $"{config.TopId}_name",
                    ShortName = $"{config.TopId}_shortName",
                    Side = config.Side ?? ["Usec"],
                    BodyPart = "Body",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab
                    {
                        Path = config.TopBundlePath!,
                        Rcid = ""
                    },
                    WatchPosition = config.WatchPosition ?? new XYZ { X = 0, Y = 0, Z = 0 },
                    WatchPrefab = config.WatchPrefab ?? new Prefab { Path = "", Rcid = "" },
                    WatchRotation = config.WatchRotation ?? new XYZ { X = 0, Y = 0, Z = 0 }
                },
                Prototype = "5cde95d97d6c8b647a3769b0"
            };

            _database.Templates.Customization[config.TopId!] = topItem;
            LogHelper.Debug(logger,$"Added top customization: {config.TopId}");

            // Create hands customization item
            var handsItem = new CustomizationItem
            {
                Id = config.HandsId!,
                Name = $"{config.HandsId}_name",
                Parent = "5cc086a314c02e000c6bea69",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.HandsId}_desc",
                    Name = $"{config.HandsId}_name",
                    ShortName = $"{config.HandsId}_shortName",
                    Side = config.Side ?? ["Usec"],
                    BodyPart = "Hands",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab
                    {
                        Path = config.HandsBundlePath!,
                        Rcid = ""
                    },
                    WatchPosition = config.WatchPosition ?? new XYZ { X = 0, Y = 0, Z = 0 },
                    WatchPrefab = config.WatchPrefab ?? new Prefab { Path = "", Rcid = "" },
                    WatchRotation = config.WatchRotation ?? new XYZ { X = 0, Y = 0, Z = 0 }
                },
                Prototype = "5cde95fa7d6c8b04737c2d13"
            };

            _database.Templates.Customization[config.HandsId!] = handsItem;
            LogHelper.Debug(logger,$"Added hands customization: {config.HandsId}");

            // Create suite
            var suite = new CustomizationItem
            {
                Id = config.SuiteId!,
                Name = $"{config.SuiteId}_name",
                Parent = "5cd944ca1388ce03a44dc2a4",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = "DefaultUsecUpperSuite",
                    Name = "DefaultUsecUpperSuite",
                    ShortName = "DefaultUsecUpperSuite",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    AvailableAsDefault = false,
                    Game = ["eft", "arena"],
                    Body = config.TopId!,
                    Hands = config.HandsId!
                },
                Prototype = "5cde9ec17d6c8b04723cf479"
            };

            _database.Templates.Customization[config.SuiteId!] = suite;
            LogHelper.Debug(logger,$"Added suite customization: {config.SuiteId}");

            HandleLocale(config, config.SuiteId!);
            AddSuiteToTrader(config);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to add top for outfitId {config.OutfitId}: {ex.Message}");
            return false;
        }
    }

    private bool AddBottom(CustomClothingConfig config)
    {
        try
        {
            if (_database == null) return false;

            // Create bottom customization item
            var bottomItem = new CustomizationItem
            {
                Id = config.BottomId!,
                Name = $"{config.BottomId}_name",
                Parent = "5cc0869814c02e000a4cad94",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.BottomId}_desc",
                    Name = $"{config.BottomId}_name",
                    ShortName = $"{config.BottomId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    BodyPart = "Feet",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab
                    {
                        Path = config.BottomBundlePath!,
                        Rcid = ""
                    },
                    WatchPosition = new XYZ { X = 0, Y = 0, Z = 0 },
                    WatchPrefab = new Prefab { Path = "", Rcid = "" },
                    WatchRotation = new XYZ { X = 0, Y = 0, Z = 0 }
                },
                Prototype = "5cdea3c47d6c8b0475341734"
            };

            _database.Templates.Customization[config.BottomId!] = bottomItem;
            LogHelper.Debug(logger,$"Added bottom customization: {config.BottomId}");

            // Create suite
            var suite = new CustomizationItem
            {
                Id = config.SuiteId!,
                Name = $"{config.SuiteId}_name",
                Parent = "5cd944d01388ce000a659df9",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.SuiteId}_desc",
                    Name = $"{config.SuiteId}_name",
                    ShortName = $"{config.SuiteId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    AvailableAsDefault = false,
                    Game = ["eft", "arena"],
                    Feet = config.BottomId!
                },
                Prototype = "5cd946231388ce000d572fe3"
            };

            _database.Templates.Customization[config.SuiteId!] = suite;
            LogHelper.Debug(logger,$"Added suite customization: {config.SuiteId}");

            HandleLocale(config, config.SuiteId!);
            AddSuiteToTrader(config);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to add bottom for outfitId {config.OutfitId}: {ex.Message}");
            return false;
        }
    }

    private void AddSuiteToTrader(CustomClothingConfig config)
    {
        if (_database == null) return;

        if (!TraderIds.TraderMap.TryGetValue(config.TraderId.ToLower(), out var traderId))
        {
            logger.Warning($"Unknown trader key '{config.TraderId}'");
            return;
        }
        
        string currencyId = ItemTplResolver.ResolveId(config.CurrencyId); 
            
        // Ensure trader is a customization seller
        _database.Traders[traderId].Base.CustomizationSeller = true;

        _database.Traders[traderId].Suits ??= [];

        var traderSuit = new Suit()
        {
            Id = config.OutfitId!,
            Tid = traderId,
            SuiteId = config.SuiteId!,
            IsActive = true,
            IsHiddenInPVE = false,
            ExternalObtain = false,
            InternalObtain = false,
            Requirements = new SuitRequirements()
            {
                LoyaltyLevel = config.LoyaltyLevel,
                ProfileLevel = config.ProfileLevel,
                Standing = config.Standing,
                SkillRequirements = [],
                QuestRequirements = [],
                AchievementRequirements = [],
                ItemRequirements =
                [
                    new ItemRequirement
                    {
                        Id = null,
                        Type = "ItemRequirement",
                        Count = config.Price,
                        Tpl = currencyId,
                        OnlyFunctional = true
                    }
                ],
                RequiredTid = traderId
            }
        };

        _database.Traders[traderId].Suits?.Add(traderSuit);
        LogHelper.Debug(logger,$"Added suite {config.SuiteId} to trader {config.TraderId}");
    }

    private void HandleLocale(CustomClothingConfig config, string clothingId)
    {
        if (_database == null || config.Locales == null) return;

        var globalLocales = _database.Locales.Global;
        string itemNameKey = $"{clothingId} Name";

        foreach (var (localeCode, lazyLocale) in globalLocales)
        {
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData == null) return localeData;

                if (config.Locales.TryGetValue(localeCode, out var localizedName))
                {
                    localeData[itemNameKey] = localizedName;
                }
                else if (config.Locales.TryGetValue("en", out var fallbackName))
                {
                    localeData[itemNameKey] = fallbackName;
                }

                return localeData;
            });
        }
    }
}
