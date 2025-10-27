using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace WTTServerCommonLib.Helpers;

[Injectable]
public class ConfigHelper(ISptLogger<ConfigHelper> logger, JsonUtil jsonUtil)
{
    public async Task<T?> LoadJsonFile<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
        {
            logger.Warning($"File not found: {filePath}");
            return null;
        }

        try
        {
            var data = await jsonUtil.DeserializeFromFileAsync<T>(filePath);
        
            if (data != null)
                LogHelper.Debug(logger, $"Loaded file: {filePath}");
            else
                logger.Warning($"Failed to deserialize {filePath}");
            
            return data;
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading file {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<T>> LoadAllJsonFiles<T>(string directoryPath)
    {
        var result = new List<T>();

        if (!Directory.Exists(directoryPath)) return result;

        var jsonFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
            try
            {
                var jsonData = await jsonUtil.DeserializeFromFileAsync<T>(filePath);
                if (jsonData != null)
                {
                    result.Add(jsonData);
                    LogHelper.Debug(logger, $"Loaded file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading file {filePath}: {ex.Message}");
            }

        return result;
    }


    public async Task<Dictionary<string, Dictionary<string, string>>> LoadLocalesFromDirectory(string directoryPath)
    {
        var locales = new Dictionary<string, Dictionary<string, string>>();

        var jsonFiles = Directory.GetFiles(directoryPath, "*.json")
            .Concat(Directory.GetFiles(directoryPath, "*.jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
        {
            var localeCode = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                var data = await jsonUtil.DeserializeFromFileAsync<Dictionary<string, string>>(filePath);

                if (data != null)
                {
                    locales[localeCode] = data;
                    LogHelper.Debug(logger, $"Loaded locale file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Warning($"Failed to parse {filePath}: {ex.Message}");
            }
        }

        return locales;
    }
}