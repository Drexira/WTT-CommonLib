using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Services;

public class SlotImageManager
{
    private readonly object _lockObject = new();
    private readonly List<string> _registeredDirectories = new();
    private readonly Dictionary<string, Sprite> _slotEntries = new();

    /// <summary>
    ///     Registers a directory containing slot images and loads them immediately.
    ///     Uses the name of the image file as the slot name.
    /// </summary>
    public void RegisterDirectory(string path)
    {
        lock (_lockObject)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Invalid or missing slot image path: {path}");
                return;
            }

            if (_registeredDirectories.Contains(path))
                return;

            _registeredDirectories.Add(path);
            LoadFromDirectory(path);
        }
    }

    /// <summary>
    ///     Loads all supported image files from a directory.
    /// </summary>
    private void LoadFromDirectory(string directory)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp") RegisterSlotImage(file);
        }
    }

    /// <summary>
    ///     Registers a single slot image from disk.
    /// </summary>
    public void RegisterSlotImage(string imagePath, string slotID = null)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            Console.WriteLine($"[WTT-ClientCommonLib] Invalid or missing image file: {imagePath}");
            return;
        }

        try
        {
            var imageData = File.ReadAllBytes(imagePath);
            CreateAndRegister(imageData, slotID ?? Path.GetFileNameWithoutExtension(imagePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WTT-ClientCommonLib] Error loading image {imagePath}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Registers a slot image embedded in an assembly resource.
    /// </summary>
    public void RegisterSlotImageFromResource(Assembly assembly, string resourcePath, string slotID = null)
    {
        if (assembly == null || string.IsNullOrWhiteSpace(resourcePath))
        {
            Console.WriteLine("[WTT-ClientCommonLib] Invalid parameters for resource loading");
            return;
        }

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Resource {resourcePath} not found in {assembly.FullName}");
                return;
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var imageData = ms.ToArray();

            CreateAndRegister(imageData, slotID ?? Path.GetFileNameWithoutExtension(resourcePath));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WTT-ClientCommonLib] Error loading resource {resourcePath}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Creates a sprite from raw image data and registers it in EFT's resources.
    /// </summary>
    private void CreateAndRegister(byte[] data, string slotID)
    {
        try
        {
            if (_slotEntries.ContainsKey(slotID))
            {
#if DEBUG
                Console.WriteLine($"[WTT-ClientCommonLib] Skipped duplicate slot key: {slotID}");
#endif
                return;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(data))
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Failed to create texture for {slotID}");
                return;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            _slotEntries[slotID] = sprite;
            ResourceHelper.AddEntry($"Slots/{slotID}", sprite);
#if DEBUG
            Console.WriteLine($"[WTT-ClientCommonLib] Added slot sprite: {slotID}");
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WTT-ClientCommonLib] Error creating sprite: {ex.Message}");
        }
    }
}