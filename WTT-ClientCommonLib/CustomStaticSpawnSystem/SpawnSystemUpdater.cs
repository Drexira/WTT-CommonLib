using System;
using UnityEngine;

namespace WTTClientCommonLib.CustomStaticSpawnSystem
{
    public class SpawnSystemUpdater : MonoBehaviour
    {
        private SpawnCommands _spawnCommands;

        public SpawnSystemUpdater()
        {
            _spawnCommands = WTTClientCommonLib.Instance.SpawnCommands;
        }
        private void Update()
        {
            try
            {
                _spawnCommands.UpdateEditMode();
                
                if (_spawnCommands.IsEditing)
                {
                    if (_spawnCommands.LastSpawnedObject != null)
                    {
                        Vector3 pos = _spawnCommands.LastSpawnedObject.transform.position;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Updater failed: {ex}");
            }
        }
    }
}