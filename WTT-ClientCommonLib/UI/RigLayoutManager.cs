using System;
using System.Collections.Generic;
using System.IO;
using EFT.UI.DragAndDrop;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;

namespace WTTClientCommonLib.UI
{
    public class RigLayoutManager
    {
        private readonly List<string> _registeredDirectories = new();
        private readonly Dictionary<string, ContainedGridsView> _rigEntries = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Registers a directory containing rig layout bundles and loads them immediately.
        /// </summary>
        public void RegisterDirectory(string path)
        {
            lock (_lockObject)
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    Console.WriteLine($"[WTT-ClientCommonLib] Invalid or missing rig layout path: {path}");
                    return;
                }

                if (_registeredDirectories.Contains(path))
                    return;

                _registeredDirectories.Add(path);
                LoadFromDirectory(path);
            }
        }

        /// <summary>
        /// Loads all .bundle files from a directory.
        /// </summary>
        private void LoadFromDirectory(string directory)
        {
            foreach (var bundlePath in Directory.GetFiles(directory, "*.bundle"))
            {
                LoadBundle(bundlePath);
            }
        }

        /// <summary>
        /// Loads a single rig layout bundle.
        /// </summary>
        private void LoadBundle(string bundlePath)
        {
            string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null)
            {
                Console.WriteLine($"[WTT-ClientCommonLib] Failed to load rig layout bundle: {bundleName}");
                return;
            }

            foreach (var prefab in bundle.LoadAllAssets<GameObject>())
            {
                var gridView = prefab?.GetComponent<ContainedGridsView>();
                if (gridView == null)
                {
                    Console.WriteLine($"[WTT-ClientCommonLib] Prefab {prefab?.name ?? "null"} missing ContainedGridsView.");
                    continue;
                }

                if (!_rigEntries.ContainsKey(prefab.name))
                {
                    _rigEntries[prefab.name] = gridView;
                    ResourceHelper.AddEntry($"UI/Rig Layouts/{prefab.name}", gridView);
#if DEBUG
                    Console.WriteLine($"[WTT-ClientCommonLib] Added rig layout: {prefab.name}");
#endif
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"[WTT-ClientCommonLib] Skipped duplicate rig layout: {prefab.name}");
#endif
                }
            }

            bundle.Unload(false);
        }
    }
}
