using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using WTTClientCommonLib.CustomQuestZones.Configuration;
using WTTClientCommonLib.CustomQuestZones.Models;
using WTTClientCommonLib.CustomQuestZones.Services;

namespace WTTClientCommonLib.Patches
{
    internal class OnGameStarted: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            try
            {
                string currentMap = __instance.MainPlayer.Location;
                List<CustomQuestZone> questZones = QuestZones.GetZones();
                if (questZones == null || questZones.Count == 0)
                {
                    Logger.LogDebug("No zones data loaded; skipping initialization.");
                    return;
                }
                List<CustomQuestZone> validZones = questZones.Where(zone => zone.ZoneLocation.ToLower() == currentMap.ToLower()).ToList();
                ZoneConfigManager.ExistingQuestZones = validZones;
                QuestZones.CreateZones(validZones);
                
                var player = __instance.MainPlayer;
                string locationID = __instance.LocationId;
                if (player?.Profile?.QuestsData == null) return;
                if (locationID == null) return;

                var loader  = WTTClientCommonLib.Instance.AssetLoader;
                var configs = loader.SpawnConfigs;
                foreach (var config in configs)
                {
                    loader.ProcessSpawnConfig(__instance.MainPlayer, config, __instance.LocationId);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}