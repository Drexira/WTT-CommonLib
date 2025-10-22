using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace WTTServerCommonLib.Helpers;

[Injectable]
public class ConfigHelper(ISptLogger<ConfigHelper> logger, JsonUtil jsonUtil)
{
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string LoadJsonFile(string fileName, string directory)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var jsoncPath = Path.Combine(directory, $"{baseName}.jsonc");
        var jsonPath = Path.Combine(directory, $"{baseName}.json");
    
        return File.Exists(jsoncPath) ? jsoncPath : jsonPath;
    }

    public List<T> LoadAllJsonFiles<T>(string directoryPath)
    {
        var result = new List<T>();

        if (!Directory.Exists(directoryPath)) return result;

        var jsonFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
            .ToArray();

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var jsonData = jsonUtil.DeserializeFromFile<T>(filePath);
                if (jsonData != null)
                {
                    result.Add(jsonData);
                    LogHelper.Debug(logger,$"Loaded file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading file {filePath}: {ex.Message}");
            }
        }

        return result;
    }

    
    public Dictionary<string, Dictionary<string, string>> LoadLocalesFromDirectory(string directoryPath)
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
                var data = jsonUtil.DeserializeFromFile<Dictionary<string, string>>(filePath);

                if (data != null)
                {
                    locales[localeCode] = data;
                    LogHelper.Debug(logger,$"Loaded locale file: {filePath}");
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