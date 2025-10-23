using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class WTTCustomQuestService(
    ISptLogger<WTTCustomQuestService> logger,
    DatabaseServer databaseServer,
    ConfigServer cfgServer,
    ImageRouter imageRouter,
    ModHelper modHelper,
    ConfigHelper configHelper,
    JsonUtil jsonUtil)
{
    private DatabaseTables? _database;
    private Dictionary<string, CustomQuestTimeWindow> _timeWindows = new();

    public async Task CreateCustomQuests(Assembly assembly, string? relativePath = null)
    {
        _database = databaseServer.GetTables();
        string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        string defaultDir = Path.Combine("db", "CustomQuests");
        string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);
        
        await ImportQuestTimeConfig(finalDir);
        await ImportQuestSideConfig(finalDir);
        await LoadAllTraderQuests(finalDir);
    }

    private async Task LoadAllTraderQuests(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            logger.Warning($"Quest base directory not found: {basePath}");
            return;
        }

        var directories = Directory.GetDirectories(basePath);

        foreach (var traderDir in directories)
        {
            string traderKey = Path.GetFileName(traderDir);
            string traderId;
            if (TraderIds.TraderMap.TryGetValue(traderKey.ToLower(), out var mappedTraderId))
            {
                traderId = mappedTraderId;
                LogHelper.Debug(logger,$"Mapped trader key '{traderKey}' to ID '{traderId}'");
            }
            else if (traderKey.IsValidMongoId())
            {
                traderId = traderKey;
                LogHelper.Debug(logger,$"Using trader key '{traderKey}' as direct ID");
            }
            else
            {
                logger.Warning($"Unknown trader key '{traderKey}' and not a valid Mongo ID");
                continue;
            }

            await LoadQuestsFromDirectory(traderId, traderDir);
        }
    }

    private async Task LoadQuestsFromDirectory(string traderId, string traderDir)
    {
        LogHelper.Debug(logger,$"Loading quests for trader {traderId} from {traderDir}");

        var questFiles = await LoadQuestFiles(Path.Combine(traderDir, "Quests"));
        var assortFiles = await LoadAssortFiles(Path.Combine(traderDir, "QuestAssort"));
        var imageFiles = LoadImageFiles(Path.Combine(traderDir, "Images"));

        ImportQuestData(questFiles, traderId);
        ImportQuestAssortData(assortFiles, traderId);
        await ImportLocaleData(traderId, traderDir);
        ImportImageData(imageFiles, traderId);
    }

    private async Task<List<Dictionary<MongoId, Quest>>> LoadQuestFiles(string questsDir)
    {
        var result = new List<Dictionary<MongoId, Quest>>();

        try
        {
            var questDicts = await configHelper.LoadAllJsonFiles<Dictionary<MongoId, Quest>>(questsDir);

            foreach (var questData in questDicts)
            {
                if (questData.Any())
                {
                    result.Add(questData);
                    LogHelper.Debug(logger,$"Loaded quest data with {questData.Count} quests");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error scanning for quest files in {questsDir}: {ex.Message}");
        }

        return result;
    }
    private async Task<List<Dictionary<string, Dictionary<MongoId, MongoId>>>> LoadAssortFiles(string assortDir)
    {
        var result = new List<Dictionary<string, Dictionary<MongoId, MongoId>>>();
        if (!Directory.Exists(assortDir))
            return result;

        try
        {
            var assortDicts = await configHelper.LoadAllJsonFiles<Dictionary<string, Dictionary<MongoId, MongoId>>>(assortDir);

            foreach (var assortData in assortDicts)
            {
                if (assortData.Any())
                {
                    result.Add(assortData);
                    LogHelper.Debug(logger,$"Loaded assort data with {assortData.Count} entries");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error scanning for assort files in {assortDir}: {ex.Message}");
        }

        return result;
    }
    
    
    private readonly string[] _validImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private List<string> LoadImageFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return new List<string>();
        }

        try
        {
            var images = Directory.GetFiles(directoryPath)
                .Where(f => _validImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
            
            LogHelper.Debug(logger,$"Found {images.Count} image files in {directoryPath}");
            return images;
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading images from {directoryPath}: {ex.Message}");
            return new List<string>();
        }
    }

    private void ImportQuestData(List<Dictionary<MongoId, Quest>> questFiles, string traderId)
    {
        if (!questFiles.Any())
        {
            logger.Warning($"{traderId}: No quest files found or loaded");
            return;
        }

        int questCount = 0;
        foreach (var file in questFiles)
        {
            foreach (var (key, quest) in file)
            {
                if (!key.IsValidMongoId())
                {
                    logger.Warning($"{traderId}: Invalid quest ID '{key}', skipping");
                    continue;
                }

                if (_timeWindows.TryGetValue(key, out var window)
                    && !IsWithin(window)) continue;
                _database.Templates.Quests[key] = quest;
                questCount++;
                LogHelper.Debug(logger,$"{traderId}: Added quest {key}");

            }
        }

        LogHelper.Debug(logger,$"{traderId}: Successfully loaded {questCount} quests");
    }
    private bool IsWithin(CustomQuestTimeWindow w)
    {
        var now = DateTime.Now;
        var year = now.Year;
        var start = new DateTime(year, w.StartMonth, w.StartDay);
        var end   = new DateTime(year, w.EndMonth, w.EndDay);

        if (end < start)
        {
            // spans year-end
            if (now.Month < w.StartMonth) start = start.AddYears(-1);
            else                             end   = end.AddYears(1);
        }

        return now >= start && now <= end;
    }

    private void ImportQuestAssortData(List<Dictionary<string, Dictionary<MongoId, MongoId>>> assortFiles, string traderId)
    {
        if (assortFiles.Count == 0)
        {
            logger.Warning($"{traderId}: No quest assort files found");
            return;
        }

        var trader = _database.Traders.GetValueOrDefault(traderId);
        if (trader == null)
        {
            logger.Warning($"Trader {traderId} not found in database, cannot import quest assort");
            return;
        }

        int assortCount = 0;
        foreach (var questAssort in assortFiles)
        {
            foreach (var (stage, questAssortDict) in questAssort)
            {
                if (!trader.QuestAssort.TryGetValue(stage, out Dictionary<MongoId, MongoId>? value))
                {
                    value = new Dictionary<MongoId, MongoId>();
                    trader.QuestAssort[stage] = value;
                }

                foreach (var (questId, assortId) in questAssortDict)
                {
                    value[questId] = assortId;
                    assortCount++;
                    LogHelper.Debug(logger,$"{traderId}: Added assort for quest {questId} in stage {stage}");
                }
            }
        }

        LogHelper.Debug(logger,$"{traderId}: Loaded {assortCount} quest assort items");
    }

    private async Task ImportLocaleData(string traderId, string traderDir)
    {
        string localesPath = Path.Combine(traderDir, "Locales");
    
        try
        {
            var locales = await configHelper.LoadLocalesFromDirectory(localesPath);
    
            if (locales.Count == 0)
            {
                logger.Warning($"{traderId}: No locale files found or loaded from {localesPath}");
                return;
            }

            Dictionary<string, string>? fallback = locales.TryGetValue("en", out var englishLocales) ? englishLocales : locales.Values.FirstOrDefault();

            if (fallback == null) return;

            foreach (var (localeCode, lazyLocale) in _database.Locales.Global)
            {
                lazyLocale.AddTransformer(localeData =>
                {
                    if (localeData is null)
                    {
                        return localeData;
                    }

                    var customLocale = locales.GetValueOrDefault(localeCode, fallback);

                    foreach (var (key, value) in customLocale)
                    {
                        localeData[key] = value;
                    }

                    return localeData;
                });
            }

            LogHelper.Debug(logger,$"{traderId}: Registered transformers for {locales.Count} quest locale files");
        }
        catch (Exception ex)
        {
            logger.Error($"{traderId}: Error loading quest locales: {ex.Message}");
        }
    }

    private void ImportImageData(List<string> imageFiles, string traderId)
    {
        if (imageFiles.Count == 0)
        {
            LogHelper.Debug(logger,$"{traderId}: No images found");
            return;
        }

        foreach (var imagePath in imageFiles)
        {
            try
            {
                string imageName = Path.GetFileNameWithoutExtension(imagePath);
                imageRouter.AddRoute($"/files/quest/icon/{imageName}", imagePath);
                LogHelper.Debug(logger,$"{traderId}: Registered image route for {imageName}");
            }
            catch (Exception ex)
            {
                logger.Warning($"{traderId}: Failed to register image {imagePath}: {ex.Message}");
            }
        }
        
        LogHelper.Debug(logger,$"{traderId}: Loaded {imageFiles.Count} images");
    }
    private async Task ImportQuestTimeConfig(string basePath)
    {
        // Try both .json and .jsonc
        string[] filenames = ["QuestTimeData.json", "QuestTimeData.jsonc"];
        string? configPath = filenames
            .Select(name => Path.Combine(basePath, name))
            .FirstOrDefault(File.Exists);

        if (configPath == null)
        {
            LogHelper.Debug(logger,"No QuestTimeData.json/.jsonc found, skipping date locks");
            return;
        }

        try
        {
            _timeWindows = await jsonUtil
                               .DeserializeFromFileAsync<Dictionary<string, CustomQuestTimeWindow>>(configPath)
                           ?? [];

            LogHelper.Debug(logger,$"Loaded QuestTimeData from {Path.GetFileName(configPath)} for {_timeWindows.Count} quests");
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading {Path.GetFileName(configPath)}: {ex.Message}");
        }
    }

    private async Task ImportQuestSideConfig(string basePath)
    {
        string[] filenames = ["QuestSideData.json", "QuestSideData.jsonc"];
        string? configPath = filenames
            .Select(name => Path.Combine(basePath, name))
            .FirstOrDefault(File.Exists);

        if (configPath == null)
        {
            LogHelper.Debug(logger,"No QuestSideData.json/.jsonc found, skipping side-exclusive setup");
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CustomQuestSideConfig>(content);

            var questConfig = cfgServer.GetConfig<QuestConfig>();
            if (config == null)
            {
                logger.Warning("QuestSideData.json is empty or invalid");
                return;
            }

            int usecAdded = 0;
            int bearAdded = 0;
            
            if (config.UsecOnlyQuests.Count > 0)
            {
                foreach (var questId in config.UsecOnlyQuests)
                {
                    if (questId.IsValidMongoId())
                    {
                        questConfig.UsecOnlyQuests.Add(questId);
                        usecAdded++;
                    }
                    else
                    {
                        logger.Warning($"Invalid USEC quest ID in QuestSideData.json: {questId}");
                    }
                }
            }

            if (config.BearOnlyQuests.Count > 0)
            {
                foreach (var questId in config.BearOnlyQuests)
                {
                    if (questId.IsValidMongoId())
                    {
                        questConfig.BearOnlyQuests.Add(questId);
                        bearAdded++;
                    }
                    else
                    {
                        logger.Warning($"Invalid BEAR quest ID in QuestSideData.json: {questId}");
                    }
                }
            }

            LogHelper.Debug(logger,$"Loaded QuestSideData.json: {usecAdded} USEC quests, {bearAdded} BEAR quests");
        }
        catch (Exception ex)
        {
            logger.Critical("Error loading QuestSideData.json", ex);
        }
    }
    
    
}