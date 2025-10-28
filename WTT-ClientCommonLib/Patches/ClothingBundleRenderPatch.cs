﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Diz.Jobs;
using SPT.Reflection.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib.Patches;

internal class ClothingBundleRendererPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass925).GetMethod("RenderModel",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    [PatchPrefix]
    private static bool PatchPrefix(
        GClass925 __instance,
        GClass3677 clothing,
        GClass924<GClass3677, GClass3685>.SpriteFactory spriteFactory,
        ref Task<GClass924<GClass3677, GClass3685>.RenderModelResult> __result)
    {
        try
        {
            // Get the asset and all SkinnedMeshRenderers
            var asset = __instance.IEasyAssets.GetAsset<GameObject>(clothing.Prefab.path, clothing.Prefab.rcid);
            var allRenderers = asset?.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // If no renderers or only one renderer, let the original method handle it
            if (allRenderers == null || allRenderers.Length <= 1) return true;

            // Count total unique materials across all renderers
            var uniqueMaterials = new HashSet<Material>();
            foreach (var renderer in allRenderers)
                if (renderer.sharedMaterials != null)
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat != null)
                            uniqueMaterials.Add(mat);

            // Only apply patch if there are multiple materials (custom clothing)
            // Skip if 1 or fewer materials found
            if (uniqueMaterials.Count <= 1)
            {
                Logger.LogInfo($"Skipping patch: Only {uniqueMaterials.Count} unique materials found");
                return true;
            }

            Logger.LogInfo(
                $"Applying multi-mesh patch: {allRenderers.Length} renderers, {uniqueMaterials.Count} unique materials on {clothing.Name.Localized()}");

            // Create a parent object to hold all mesh objects
            var parentObject = new GameObject("ClothingBundleParent");
            parentObject.layer = LayerMaskClass.WeaponPreview;
            parentObject.transform.SetPositionAndRotation(
                __instance.GameObject_0.transform.position,
                __instance.GameObject_0.transform.rotation
            );

            // Create a GameObject for each SkinnedMeshRenderer
            foreach (var skinnedRenderer in allRenderers)
                if (skinnedRenderer.sharedMesh != null && skinnedRenderer.enabled)
                {
                    var meshObject = new GameObject(skinnedRenderer.name + "_Preview");
                    meshObject.layer = parentObject.layer;
                    meshObject.transform.SetParent(parentObject.transform, false);

                    var mf = meshObject.AddComponent<MeshFilter>();
                    var mr = meshObject.AddComponent<MeshRenderer>();

                    mf.sharedMesh = skinnedRenderer.sharedMesh;
                    mr.sharedMaterials = skinnedRenderer.sharedMaterials;

                    // Copy transform from original skinned renderer
                    meshObject.transform.localPosition = skinnedRenderer.transform.localPosition;
                    meshObject.transform.localRotation = skinnedRenderer.transform.localRotation;
                    meshObject.transform.localScale = skinnedRenderer.transform.localScale;
                }

            // Use the parent object for rendering and skip the original method
            __result = CompleteRendering(__instance, spriteFactory, parentObject, asset);
            return false;
        }
        catch (Exception e)
        {
            Logger.LogError($"Error in ClothingBundleRendererPatch: {e}");
            return true; // Fall back to original method
        }
    }

    private static async Task<GClass924<GClass3677, GClass3685>.RenderModelResult> CompleteRendering(
        GClass925 instance,
        GClass924<GClass3677, GClass3685>.SpriteFactory spriteFactory,
        GameObject parentObject,
        GameObject asset)
    {
        var originalGameObject = instance.GameObject_0;
        var originalMeshFilter = instance.MeshFilter_0;
        var originalMeshRenderer = instance.MeshRenderer_0;

        try
        {
            // Wait if already rendering
            while (instance.Bool_2) await JobScheduler.Yield();

            instance.Bool_2 = true;

            // Ensure the parent object is active
            if (!parentObject.activeSelf) parentObject.SetActive(true);

            // Rotate all child meshes
            foreach (Transform child in parentObject.transform) child.Rotate(90f, 0f, 0f, Space.Self);

            // NOW calculate combined bounds from all child meshes (with rotation applied)
            var combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
            var boundsInitialized = false;
            var validMeshCount = 0;

            foreach (Transform child in parentObject.transform)
            {
                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var childBounds = mf.sharedMesh.bounds;

                    // Validate the bounds
                    if (childBounds.size.magnitude < 0.001f)
                    {
                        Logger.LogWarning($"Skipping mesh with invalid bounds: {child.name}");
                        continue;
                    }

                    // Calculate world-space bounds for this mesh
                    // Get all corners of the mesh bounds
                    var corners = new Vector3[8];
                    var center = childBounds.center;
                    var extents = childBounds.extents;

                    corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
                    corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
                    corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
                    corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
                    corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
                    corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
                    corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
                    corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

                    // Transform corners to parent space
                    for (var i = 0; i < corners.Length; i++)
                    {
                        corners[i] = child.TransformPoint(corners[i]);
                        corners[i] = parentObject.transform.InverseTransformPoint(corners[i]);
                    }

                    // Create bounds from transformed corners
                    var worldBounds = new Bounds(corners[0], Vector3.zero);
                    for (var i = 1; i < corners.Length; i++) worldBounds.Encapsulate(corners[i]);

                    if (!boundsInitialized)
                    {
                        combinedBounds = worldBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(worldBounds);
                    }

                    validMeshCount++;
                }
            }

            Logger.LogDebug($"Rendering with {validMeshCount} valid meshes. Bounds size: {combinedBounds.size}");


            if (validMeshCount == 0)
            {
                Logger.LogWarning("No valid meshes found for rendering");
                instance.Bool_2 = false;
                return default;
            }

            Logger.LogInfo($"Rendering with {validMeshCount} valid meshes. Bounds size: {combinedBounds.size}");

            // Temporarily replace the instance's GameObject with our multi-mesh object
            instance.GameObject_0 = parentObject;

            // Create a dummy mesh with the combined bounds
            var boundsMesh = new Mesh();
            boundsMesh.bounds = combinedBounds;

            var parentMeshFilter = parentObject.AddComponent<MeshFilter>();
            parentMeshFilter.sharedMesh = boundsMesh;
            instance.MeshFilter_0 = parentMeshFilter;

            // Add a MeshRenderer to the parent
            var parentMeshRenderer = parentObject.AddComponent<MeshRenderer>();
            instance.MeshRenderer_0 = parentMeshRenderer;

            // Get the PreviewPivot component
            var pivot = asset?.GetComponent<PreviewPivot>();

            // Use the sprite factory with our multi-mesh object
            var result = await spriteFactory(parentObject, pivot);

            instance.Bool_2 = false;

            // Restore the original GameObject and components
            instance.GameObject_0 = originalGameObject;
            instance.MeshFilter_0 = originalMeshFilter;
            instance.MeshRenderer_0 = originalMeshRenderer;

            // Clean up the temporary objects
            if (parentObject != null) Object.Destroy(parentObject);
            if (boundsMesh != null) Object.Destroy(boundsMesh);

            return new GClass924<GClass3677, GClass3685>.RenderModelResult
            {
                sprite = result,
                zeroMipWasLoaded = true
            };
        }
        catch (Exception e)
        {
            instance.Bool_2 = false;
            instance.GameObject_0 = originalGameObject;
            instance.MeshFilter_0 = originalMeshFilter;
            instance.MeshRenderer_0 = originalMeshRenderer;

            Logger.LogError($"Error in CompleteRendering: {e}");

            if (parentObject != null) Object.Destroy(parentObject);
            return default;
        }
    }
}