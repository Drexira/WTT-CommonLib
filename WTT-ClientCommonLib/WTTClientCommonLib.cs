using BepInEx;
using System;
using WTTClientCommonLib.CustomQuestZones.Configuration;
using WTTClientCommonLib.Patches;

namespace WTTClientCommonLib
{
    [BepInPlugin("com.WTT.ClientCommonLib", "WTT-ClientCommonLib", "1.0.0")]
    public class WTTClientCommonLib : BaseUnityPlugin
    {
        private ResourceLoader _resourceLoader;

        private void Awake()
        {
            try
            {
                ZoneConfigManager.Initialize(Config);
                new OnGameStarted().Enable();

                _resourceLoader = new ResourceLoader(Logger);
                _resourceLoader.LoadAllResourcesFromServer();

            }
            catch (Exception ex) 
            {
                Logger.LogError($"Failed to initialize WTT-ClientCommonLib: {ex}");
            }
        }
    }
}