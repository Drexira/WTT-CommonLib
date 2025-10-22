using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;
using WTTClientCommonLib.CustomStaticSpawnSystem.Models;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib.CustomStaticSpawnSystem
{
    public class SpawnCommands(ManualLogSource logger, AssetLoader assetLoader)
    {
        private GameObject _lastSpawnedObject;
        private List<SpawnedObjectInfo> _spawnedObjects = new List<SpawnedObjectInfo>();
        private int _currentSpawnedIndex = -1;
        private bool _isEditing = false;
        private const float MoveSpeed = 1f; 
        private const float RotateSpeed = 15f;
        private bool _shiftPressed = false;
        private bool _altPressed = false;
        public GameObject LastSpawnedObject => 
            _currentSpawnedIndex >= 0 ? _spawnedObjects[_currentSpawnedIndex].Object : null;

        private SpawnedObjectInfo CurrentSpawnedInfo => 
            _currentSpawnedIndex >= 0 ? _spawnedObjects[_currentSpawnedIndex] : null;
        private GameObject _editIndicator;
        public bool IsEditing => _isEditing;
        private GamePlayerOwner _gamePlayerOwner;

        public void SpawnObject(string bundleName, string prefabName, string[] args = null)
        {
            // Get player position
            var player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player == null)
            {
                logger.LogDebug("Player not found. Are you in a raid?");
                return;
            }

            // Calculate spawn position 2 meters in front of player
            Vector3 spawnPosition = player.Transform.position + player.Transform.forward * 3f;
            Quaternion spawnRotation = Quaternion.identity;

            // Parse coordinates if provided
            if (args != null && args.Length >= 3)
            {
                if (float.TryParse(args[0], out float x) &&
                    float.TryParse(args[1], out float y) &&
                    float.TryParse(args[2], out float z))
                {
                    spawnPosition = new Vector3(x, y, z);
                }

                // Parse rotation if provided
                if (args.Length >= 6)
                {
                    if (float.TryParse(args[3], out float rx) &&
                        float.TryParse(args[4], out float ry) &&
                        float.TryParse(args[5], out float rz))
                    {
                        spawnRotation = Quaternion.Euler(rx, ry, rz);
                    }
                }
            }

            // Load and spawn the prefab
            var prefab = assetLoader.LoadPrefabFromBundle(bundleName, prefabName);
            if (prefab == null)
            {
                logger.LogDebug($"Failed to load prefab: {prefabName} from bundle: {bundleName}");
                return;
            }

            var newObject = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
            _lastSpawnedObject = newObject;
    
            // Store metadata with the object
            _spawnedObjects.Add(new SpawnedObjectInfo {
                Object = newObject,
                BundleName = bundleName,
                PrefabName = prefabName
            });
            _currentSpawnedIndex = _spawnedObjects.Count - 1;
            logger.LogDebug($"Spawned {prefabName} at {spawnPosition}");
        }


        public void StartEditMode()
        {
            if (_lastSpawnedObject == null)
            {
                logger.LogDebug("No object to edit. Spawn an object first.");
                return;
            }

            _isEditing = true;
            _gamePlayerOwner = WTTClientCommonLib.Player.GetComponentInChildren<GamePlayerOwner>();
            _gamePlayerOwner.enabled = false;
            
            // In StartEditMode():
            if (_editIndicator == null)
            {
                _editIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _editIndicator.transform.localScale = Vector3.one * 0.3f;
                _editIndicator.GetComponent<Renderer>().material.color = Color.red;
                _editIndicator.GetComponent<Collider>().enabled = false;
    
                // Add visible rotation indicator
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.transform.SetParent(_editIndicator.transform);
                arrow.transform.localPosition = new Vector3(0, 0, 0.5f);
                arrow.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            }
    
            logger.LogDebug("Entering edit mode. Use WASD to move, Arrow Keys to rotate. Press Enter to confirm.");
            logger.LogDebug("Current position: " + _lastSpawnedObject.transform.position);
        }

        public void ExitEditMode()
        {
            logger.LogDebug("Exiting edit mode.");
            _isEditing = false;
            _gamePlayerOwner = WTTClientCommonLib.Player.GetComponentInChildren<GamePlayerOwner>();
            _gamePlayerOwner.enabled = true;
    
            // Clean up visual indicator
            if (_editIndicator != null)
            {
                Object.Destroy(_editIndicator);
                _editIndicator = null;
            }
        }
    public void UpdateEditMode()
    {
        if (!_isEditing || _lastSpawnedObject == null)
        {
            if (_isEditing)
                logger.LogError("EditMode active but no object!");
            return;
        }

        Transform cameraTransform = Camera.main?.transform;
        if (cameraTransform == null) return;

        // Handle object cycling and deletion
        if (StaticSpawnSystemConfigManager.CycleSpawnedObjects.Value.BetterIsDown())
        {
            CycleSpawnedObjects();
        }
        else if (StaticSpawnSystemConfigManager.CyclePreviousSpawnedObject.Value.BetterIsDown())
        {
            CyclePreviousSpawnedObject();
        }
        else if (StaticSpawnSystemConfigManager.DeleteSelectedObject.Value.BetterIsDown())
        {
            DeleteSelectedObject();
            // Skip movement if we just deleted the object
            if (_lastSpawnedObject == null) return;
        }
        _shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);


        // Modifier key-based speed multiplier
        float speedMultiplier = 1f;
        if (_shiftPressed)
        {
            speedMultiplier = 2f;
            if (_altPressed)
            {
                speedMultiplier = 4f;
            }
        }

        // Get input states
        bool forwardPressed = StaticSpawnSystemConfigManager.MoveForwardKey.Value.BetterIsPressed();
        bool backwardPressed = StaticSpawnSystemConfigManager.MoveBackwardKey.Value.BetterIsPressed();
        bool leftPressed = StaticSpawnSystemConfigManager.MoveLeftKey.Value.BetterIsPressed();
        bool rightPressed = StaticSpawnSystemConfigManager.MoveRightKey.Value.BetterIsPressed();
        bool upPressed = StaticSpawnSystemConfigManager.MoveUpKey.Value.BetterIsPressed();
        bool downPressed = StaticSpawnSystemConfigManager.MoveDownKey.Value.BetterIsPressed();

        // Camera-relative movement vectors
        Vector3 moveDirection = Vector3.zero;
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        Vector3 up = Vector3.up; // Use world up for vertical movement

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        if (forwardPressed) moveDirection += forward;
        if (backwardPressed) moveDirection -= forward;
        if (leftPressed) moveDirection -= right;
        if (rightPressed) moveDirection += right;
        if (upPressed) moveDirection += up;
        if (downPressed) moveDirection -= up;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        _lastSpawnedObject.transform.position += moveDirection * MoveSpeed * speedMultiplier * Time.deltaTime;

        // ROTATION SYSTEM - CORRECTED DIRECTIONS
        Vector3 rotationInput = Vector3.zero;

        // Primary directions
        bool pitchUpPressed = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchUpKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchDownPressed = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchDownKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool yawLeftPressed = Input.GetKey(StaticSpawnSystemConfigManager.RotateYawLeftKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool yawRightPressed = Input.GetKey(StaticSpawnSystemConfigManager.RotateYawRightKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        
        if (pitchUpPressed)       // 8 - Pitch up (forward)
            rotationInput.x += 1;
        if (pitchDownPressed)     // 2 - Pitch down (backward)
            rotationInput.x = 1;
        if (yawLeftPressed)       // 4 - Yaw left (counter-clockwise)
            rotationInput.y -= 1;
        if (yawRightPressed)      // 6 - Yaw right (clockwise)
            rotationInput.y += 1;

        // Diagonal directions
        bool pitchRollLeftKey = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchRollLeftKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollRightKey = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchRollRightKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollLeftInvertKey = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchRollLeftInvertKey.Value.MainKey) && !_shiftPressed && !_altPressed;
        bool pitchRollRightInvertKey = Input.GetKey(StaticSpawnSystemConfigManager.RotatePitchRollRightInvertKey.Value.MainKey) && !_shiftPressed && !_altPressed;

        
        if (pitchRollLeftKey)         // 7 - Forward left
        {
            rotationInput.x -= 1;  // Pitch up
            rotationInput.z += 1;  // Roll left
        }
        if (pitchRollRightKey)        // 9 - Forward right
        {
            rotationInput.x -= 1;  // Pitch up
            rotationInput.z -= 1;  // Roll right
        }
        if (pitchRollLeftInvertKey)   // 1 - Backward left
        {
            rotationInput.x += 1;  // Pitch down
            rotationInput.z += 1;  // Roll left
        }
        if (pitchRollRightInvertKey)  // 3 - Backward right
        {
            rotationInput.x += 1;  // Pitch down
            rotationInput.z -= 1;  // Roll right
        }

        if (rotationInput != Vector3.zero)
        {
            // Apply rotation in world space
            _lastSpawnedObject.transform.Rotate(
                rotationInput.x * RotateSpeed * speedMultiplier * Time.deltaTime,
                rotationInput.y * RotateSpeed * speedMultiplier * Time.deltaTime,
                rotationInput.z * RotateSpeed * speedMultiplier * Time.deltaTime,
                Space.World
            );
        }


        if (StaticSpawnSystemConfigManager.ConfirmPositionKey.Value.BetterIsDown())
        {
            ExitEditMode();
            logger.LogDebug("Position confirmed!");
            logger.LogDebug($"Position: {_lastSpawnedObject.transform.position}");
            logger.LogDebug($"Rotation: {_lastSpawnedObject.transform.rotation.eulerAngles}");
        }

        if (_editIndicator != null)
        {
            _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            _editIndicator.transform.rotation = Quaternion.identity; // Keep indicator upright
        }
    }

        public void DeleteSelectedObject()
        {
            if (_spawnedObjects.Count == 0 || _currentSpawnedIndex < 0)
            {
                logger.LogDebug("No object to delete.");
                return;
            }

            var currentInfo = _spawnedObjects[_currentSpawnedIndex];
            GameObject.Destroy(currentInfo.Object);
            _spawnedObjects.RemoveAt(_currentSpawnedIndex);
    
            logger.LogDebug($"Deleted object: {currentInfo.PrefabName}");

            // Update selection
            if (_spawnedObjects.Count > 0)
            {
                _currentSpawnedIndex = Mathf.Clamp(_currentSpawnedIndex, 0, _spawnedObjects.Count - 1);
                _lastSpawnedObject = _spawnedObjects[_currentSpawnedIndex].Object;
            }
            else
            {
                _currentSpawnedIndex = -1;
                _lastSpawnedObject = null;
        
                // Exit edit mode if no objects left
                if (_isEditing)
                {
                    ExitEditMode();
                }
            }
        }
        public void CycleSpawnedObjects()
        {
            if (_spawnedObjects.Count == 0)
            {
                logger.LogDebug("No spawned objects to cycle.");
                return;
            }

            _currentSpawnedIndex = (_currentSpawnedIndex + 1) % _spawnedObjects.Count;
            SpawnedObjectInfo currentInfo = _spawnedObjects[_currentSpawnedIndex];
            _lastSpawnedObject = currentInfo.Object;
    
            logger.LogDebug($"Switched to object #{_currentSpawnedIndex}: " +
                               $"{currentInfo.PrefabName} [Bundle: {currentInfo.BundleName}]");

            if (_isEditing && _editIndicator != null)
            {
                _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            }
        }

        public void CyclePreviousSpawnedObject()
        {
            if (_spawnedObjects.Count == 0)
            {
                logger.LogDebug("No spawned objects to cycle.");
                return;
            }

            _currentSpawnedIndex = (_currentSpawnedIndex - 1 + _spawnedObjects.Count) % _spawnedObjects.Count;
            SpawnedObjectInfo currentInfo = _spawnedObjects[_currentSpawnedIndex];
            _lastSpawnedObject = currentInfo.Object;
    
            logger.LogDebug($"Switched to object #{_currentSpawnedIndex}: " +
                               $"{currentInfo.PrefabName} [Bundle: {currentInfo.BundleName}]");

            if (_isEditing && _editIndicator != null)
            {
                _editIndicator.transform.position = _lastSpawnedObject.transform.position;
            }
        }


        public void ExportSpawnedObjectsLocations()
        {
            if (_spawnedObjects == null || _spawnedObjects.Count == 0)
            {
                logger.LogDebug("No spawned objects to export.");
                return;
            }

            List<CustomSpawnConfig> exportList = new List<CustomSpawnConfig>();

            foreach (var info in _spawnedObjects)
            {
                CustomSpawnConfig config = new CustomSpawnConfig
                {
                    QuestId = null,
                    LocationID = WTTClientCommonLib.Player.Location ?? "unknown_location",
                    BundleName = info.BundleName,  // Directly use stored name
                    PrefabName = info.PrefabName,  // Directly use stored name
                    Position = info.Object.transform.position,
                    Rotation = info.Object.transform.rotation.eulerAngles,
                    RequiredQuestStatuses = new List<string>(),
                    ExcludedQuestStatuses = new List<string>(),
                    QuestMustExist = null,
                    LinkedQuestId = null,
                    LinkedRequiredStatuses = new List<string>(),
                    LinkedExcludedStatuses = new List<string>(),
                    LinkedQuestMustExist = null,
                    RequiredItemInInventory = null,
                    RequiredLevel = null,
                    RequiredFaction = null,
                    RequiredBossSpawned = null
                };

                exportList.Add(config);
            }

            string outputDir = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\"));
            string path = Path.Combine(outputDir, $"WTT-ClientCommonLib-CustomStaticSpawnConfig-Output-{DateTime.Now:yyyyMMddHHmmssffff}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(exportList, Formatting.Indented));
    
            logger.LogDebug($"Exported {_spawnedObjects.Count} spawned objects to {path}");
        }

    }
}