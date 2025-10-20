using System;
using System.Collections.Generic;
using BepInEx.Logging;
using EFT.UI.DragAndDrop;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;

namespace WTTClientCommonLib
{
    public class ResourceLoader
    {
        private readonly ManualLogSource logger;

        public ResourceLoader(ManualLogSource logger)
        {
            this.logger = logger;
        }

        public void LoadAllResourcesFromServer()
        {
            try
            {
                logger.LogInfo("Loading resources from server...");
                LoadVoicesFromServer();
                LoadSlotImagesFromServer();
                LoadRigLayoutsFromServer();
                logger.LogInfo("All resources loaded successfully from server");
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
                {
                    if (!ResourceKeyManagerAbstractClass.Dictionary_0.ContainsKey(kvp.Key))
                    {
                        ResourceKeyManagerAbstractClass.Dictionary_0[kvp.Key] = kvp.Value;
                        logger.LogInfo($"Added voice key: {kvp.Key}");
                    }
                }
                logger.LogInfo($"Loaded {voiceResponse.Count} voice mappings from server");
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
                var imageManifest = Utils.Get<List<string>>("/wttcommonlib/slotimages/get");
                if (imageManifest == null)
                {
                    logger.LogWarning("No slot images manifest received from server");
                    return;
                }

                logger.LogInfo($"Received slot image manifest with {imageManifest.Count} images");

                foreach (var imageName in imageManifest)
                {
                    try
                    {
                        var imageResponse = Utils.Get<Dictionary<string, string>>(
                            $"/wttcommonlib/slotimages/data?name={Uri.EscapeDataString(imageName)}");
                        if (imageResponse != null && imageResponse.TryGetValue("data", out var base64Data))
                        {
                            if (string.IsNullOrEmpty(base64Data))
                            {
                                logger.LogWarning($"Base64 data is empty for slot image: {imageName}");
                                continue;
                            }
                            byte[] imageData = null;
                            try { imageData = Convert.FromBase64String(base64Data); }
                            catch (Exception ex)
                            {
                                logger.LogError($"Base64 decode failed for image {imageName}: {ex}");
                                continue;
                            }
                            CreateAndRegisterSlotImage(imageData, imageName);
                        }
                        else if (imageResponse != null && imageResponse.TryGetValue("error", out var error))
                        {
                            logger.LogWarning($"Failed to load slot image {imageName}: {error}");
                        }
                        else
                        {
                            logger.LogWarning($"Unexpected response format for slot image {imageName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error loading slot image {imageName}: {ex}");
                    }
                }
                logger.LogInfo($"Loaded {imageManifest.Count} slot images from server");
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

                logger.LogInfo($"Received {bundleMap.Count} rig layouts from server");

                foreach (var kvp in bundleMap)
                {
                    var bundleName = kvp.Key;
                    var base64Data = kvp.Value;
                    if (string.IsNullOrEmpty(base64Data))
                    {
                        logger.LogWarning($"No data for rig layout: {bundleName}");
                        continue;
                    }
                    byte[] bundleData = null;
                    try { bundleData = Convert.FromBase64String(base64Data); }
                    catch (Exception ex)
                    {
                        logger.LogError($"Base64 decode failed for rig layout {bundleName}: {ex}");
                        continue;
                    }
                    if (bundleData == null || bundleData.Length == 0)
                    {
                        logger.LogWarning($"Bundle data is empty for rig layout: {bundleName}");
                        continue;
                    }
                    LoadBundleFromMemory(bundleData, bundleName);
                }

                logger.LogInfo($"Loaded {bundleMap.Count} rig layouts from server");
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
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(data))
                {
                    logger.LogWarning($"Failed to create texture for {slotID}");
                    return;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                ResourceHelper.AddEntry($"Slots/{slotID}", sprite);
                logger.LogInfo($"Added slot sprite: {slotID}");
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

                AssetBundle bundle = AssetBundle.LoadFromMemory(data);
                if (bundle == null)
                {
                    logger.LogWarning($"Failed to load rig layout bundle: {bundleName}");
                    return;
                }

                int loadedCount = 0;
                var gameObjects = bundle.LoadAllAssets<GameObject>();
                if (gameObjects == null || gameObjects.Length == 0)
                {
                    logger.LogWarning($"No GameObjects loaded from bundle: {bundleName}");
                }

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
                    logger.LogInfo($"Added rig layout: {prefab.name}");
                }

                bundle.Unload(false);
                logger.LogInfo($"Loaded {loadedCount} prefabs from bundle: {bundleName}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading bundle {bundleName}: {ex}");
            }
        }
    }
}
