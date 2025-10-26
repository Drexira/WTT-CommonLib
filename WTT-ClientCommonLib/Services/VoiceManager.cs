using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WTTClientCommonLib.Services;

public class VoiceManager
{
    private readonly object _lockObject = new();
    private readonly List<string> _registeredDirectories = new();
    private readonly Dictionary<string, string> _voiceEntries = new();

    /// <summary>
    ///     Registers a new directory and immediately loads its JSON voice entries.
    /// </summary>
    public void RegisterDirectory(string path)
    {
        lock (_lockObject)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Invalid or missing voice path: {path}");
                return;
            }

            if (_registeredDirectories.Contains(path))
                return;

            _registeredDirectories.Add(path);
            LoadFromDirectory(path);
        }
    }

    /// <summary>
    ///     Loads all .json voice mappings from a directory.
    /// </summary>
    private void LoadFromDirectory(string directory)
    {
        foreach (var jsonFile in Directory.GetFiles(directory, "*.json"))
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jsonFile));
                foreach (var kvp in dict)
                {
                    if (!_voiceEntries.ContainsKey(kvp.Key))
                    {
                        _voiceEntries[kvp.Key] = kvp.Value;
                        AddToResources(kvp.Key, kvp.Value);
                    }
#if DEBUG
                    else
                    {
                        Console.WriteLine($"[WTT-ClientCommonLib] Skipped duplicate voice key: {kvp.Key}");
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Error processing {jsonFile}: {ex.Message}");
            }
    }

    /// <summary>
    ///     Adds a single key/value to EFT's resource key manager dictionary.
    /// </summary>
    private void AddToResources(string key, string value)
    {
        if (!ResourceKeyManagerAbstractClass.Dictionary_0.ContainsKey(key))
        {
            ResourceKeyManagerAbstractClass.Dictionary_0[key] = value;
#if DEBUG
            Console.WriteLine($"[WTT-ClientCommonLib] Added voice key: {key}");
#endif
        }
    }
}