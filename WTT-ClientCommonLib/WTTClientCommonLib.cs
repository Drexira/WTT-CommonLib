using BepInEx;
using System;
using Comfort.Common;
using EFT;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;
using WTTClientCommonLib.CustomQuestZones.Configuration;
using WTTClientCommonLib.CustomStaticSpawnSystem;
using WTTClientCommonLib.Patches;

namespace WTTClientCommonLib
{
    [BepInPlugin("com.WTT.ClientCommonLib", "WTT-ClientCommonLib", "1.0.0")]
    public class WTTClientCommonLib : BaseUnityPlugin
    {
        public static WTTClientCommonLib Instance { get; private set; }
        
        private GameObject _updaterObject;
        public static CommandProcessor CommandProcessor;
        public AssetLoader AssetLoader;
        public SpawnCommands SpawnCommands;
        public static GameWorld GameWorld;
        public PlayerWorldStats PlayerWorldStats;
        public static Player Player;
        private ResourceLoader _resourceLoader;
        private void Awake()
        {
            Instance = this; 
            try
            {
                AssetLoader      = new AssetLoader(Logger);
                SpawnCommands    = new SpawnCommands(Logger, AssetLoader);
                PlayerWorldStats = new PlayerWorldStats(Logger);

                ZoneConfigManager.Initialize(Config);
                StaticSpawnSystemConfigManager.Initialize(Config);
                new OnGameStarted().Enable();

                var resourceLoader = new ResourceLoader(Logger, AssetLoader);
                resourceLoader.LoadAllResourcesFromServer();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize WTT-ClientCommonLib: {ex}");
            }
        }
        internal void Start()
        {
            Init();
        }
        private void Update()
        {
            if (Singleton<GameWorld>.Instantiated && (GameWorld == null || Player == null))
            {
                GameWorld = Singleton<GameWorld>.Instance;
                Player = GameWorld.MainPlayer;
            }
        }

        internal void Init()
        {
            if (CommandProcessor == null)
            {
                CommandProcessor = new CommandProcessor(PlayerWorldStats, SpawnCommands);
                CommandProcessor.RegisterCommandProcessor();
            }
            
            if (_updaterObject == null)
            {
                _updaterObject = new GameObject("SpawnSystemUpdater");
                _updaterObject.AddComponent<SpawnSystemUpdater>();
                DontDestroyOnLoad(_updaterObject);
            }
        }
    }
}