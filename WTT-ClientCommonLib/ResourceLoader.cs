using System;
using System.Collections.Generic;
using BepInEx.Logging;
using EFT.UI.DragAndDrop;
using UnityEngine;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib;

public class ResourceLoader(ManualLogSource logger, AssetLoader assetLoader)
{
    public void LoadAllResourcesFromServer()
    {
        try
        {
            LogHelper.LogDebug("Loading resources from server...");
            LoadVoicesFromServer();
            LoadSlotImagesFromServer();
            LoadRigLayoutsFromServer();
            assetLoader.InitializeBundles("/wttcommonlib/spawnsystem/bundles/get");
            assetLoader.SpawnConfigs = assetLoader.FetchSpawnConfigs("/wttcommonlib/spawnsystem/configs/get");
            LogHelper.LogDebug($"Loaded {assetLoader.SpawnConfigs.Count} spawn configurations");
            LogHelper.LogDebug("All resources loaded successfully from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading resources from server: {ex}");
        }
    }
    
    private void LoadVoicesFromServer()
    {
        try
        {
            var voiceResponse = Utils.Get<Dictionary<string, string>>("/wttcommonlib/voices/get");
            if (voiceResponse == null)
            {
                logger.LogWarning("No voice data received from server");
                return;
            }

            foreach (var kvp in voiceResponse)
                if (!ResourceKeyManagerAbstractClass.Dictionary_0.ContainsKey(kvp.Key))
                {
                    ResourceKeyManagerAbstractClass.Dictionary_0[kvp.Key] = kvp.Value;
                    LogHelper.LogDebug($"Added voice key: {kvp.Key}");
                }

            LogHelper.LogDebug($"Loaded {voiceResponse.Count} voice mappings from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading voices: {ex}");
        }
    }

    private void LoadSlotImagesFromServer()
    {
        try
        {
            var images = Utils.Get<Dictionary<string, string>>("/wttcommonlib/slotimages/get");
            if (images == null)
            {
                logger.LogWarning("No slot images");
                return;
            }

            foreach (var kvp in images)
            {
                byte[] imageData;
                try
                {
                    imageData = Convert.FromBase64String(kvp.Value);
                }
                catch
                {
                    logger.LogWarning($"Invalid data for {kvp.Key}");
                    continue;
                }

                CreateAndRegisterSlotImage(imageData, kvp.Key);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading slot images: {ex}");
        }
    }

    private void LoadRigLayoutsFromServer()
    {
        try
        {
            var bundleMap = Utils.Get<Dictionary<string, string>>("/wttcommonlib/riglayouts/get");
            if (bundleMap == null)
            {
                logger.LogWarning("No rig layouts received from server");
                return;
            }

            LogHelper.LogDebug($"Received {bundleMap.Count} rig layouts from server");

            foreach (var kvp in bundleMap)
            {
                var bundleName = kvp.Key;
                var base64Data = kvp.Value;
                if (string.IsNullOrEmpty(base64Data))
                {
                    logger.LogWarning($"No data for rig layout: {bundleName}");
                    continue;
                }

                byte[] bundleData;
                try
                {
                    bundleData = Convert.FromBase64String(base64Data);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Base64 decode failed for rig layout {bundleName}: {ex}");
                    continue;
                }

                if (bundleData.Length == 0)
                {
                    logger.LogWarning($"Bundle data is empty for rig layout: {bundleName}");
                    continue;
                }

                LoadBundleFromMemory(bundleData, bundleName);
            }

            LogHelper.LogDebug($"Loaded {bundleMap.Count} rig layouts from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading rig layouts: {ex}");
        }
    }

    private void CreateAndRegisterSlotImage(byte[] data, string slotID)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                logger.LogWarning($"Empty data for slot image: {slotID}");
                return;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(data))
            {
                logger.LogWarning($"Failed to create texture for {slotID}");
                return;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            ResourceHelper.AddEntry($"Slots/{slotID}", sprite);
            LogHelper.LogDebug($"Added slot sprite: {slotID}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error creating slot sprite {slotID}: {ex}");
        }
    }

    private void LoadBundleFromMemory(byte[] data, string bundleName)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                logger.LogWarning($"Bundle data is null or empty for: {bundleName}");
                return;
            }

            var bundle = AssetBundle.LoadFromMemory(data);
            if (bundle == null)
            {
                logger.LogWarning($"Failed to load rig layout bundle: {bundleName}");
                return;
            }

            var loadedCount = 0;
            var gameObjects = bundle.LoadAllAssets<GameObject>();
            if (gameObjects == null || gameObjects.Length == 0)
                logger.LogWarning($"No GameObjects loaded from bundle: {bundleName}");

            if (gameObjects != null)
                foreach (var prefab in gameObjects)
                {
                    if (prefab == null)
                    {
                        logger.LogWarning("Encountered null prefab in bundle.");
                        continue;
                    }

                    var gridView = prefab.GetComponent<ContainedGridsView>();
                    if (gridView == null)
                    {
                        logger.LogWarning($"Prefab {prefab.name} missing ContainedGridsView.");
                        continue;
                    }

                    ResourceHelper.AddEntry($"UI/Rig Layouts/{prefab.name}", gridView);
                    loadedCount++;
                    LogHelper.LogDebug($"Added rig layout: {prefab.name}");
                }

            bundle.Unload(false);
            LogHelper.LogDebug($"Loaded {loadedCount} prefabs from bundle: {bundleName}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading bundle {bundleName}: {ex}");
        }
    }
}