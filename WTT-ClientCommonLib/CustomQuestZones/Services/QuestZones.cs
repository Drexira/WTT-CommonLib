using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;
using WTTClientCommonLib.CustomQuestZones.Components;
using WTTClientCommonLib.CustomQuestZones.Models;

namespace WTTClientCommonLib.CustomQuestZones.Services
{
    internal class QuestZones
    {
        public static List<CustomQuestZone> GetZones()
        {
            var request = Utils.Get<List<CustomQuestZone>>("/wttcommonlib/zones/get");
            if (request == null || request.Count == 0)
            {
                ConsoleScreen.Log("[QuestZones.GetZones] No zones data loaded.");
                return new List<CustomQuestZone>();
            }
            foreach (CustomQuestZone zone in request)
            {
                if (zone.Position.W == null) zone.Position.W = "0";
                if (zone.Rotation.W == null) zone.Rotation.W = "0";
                if (zone.Scale.W == null) zone.Scale.W = "0";
            }
#if DEBUG
            int loadedZoneCount = 0;
            if (request != null)
            {
                foreach (var zone in request)
                {
                    if (zone.ZoneLocation.ToLower() == Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower())
                    {
                        ConsoleScreen.Log("-------------------------------------");
                        ConsoleScreen.Log($"ZoneScale:");
                        ConsoleScreen.Log($"Scale Z: {zone.Scale.Z}");
                        ConsoleScreen.Log($"Scale Y: {zone.Scale.Y}");
                        ConsoleScreen.Log($"Scale X: {zone.Scale.X}");
                        ConsoleScreen.Log($"ZonePosition:");
                        ConsoleScreen.Log($"Position Z: {zone.Position.Z}");
                        ConsoleScreen.Log($"Position Y: {zone.Position.Y}");
                        ConsoleScreen.Log($"Position X: {zone.Position.X}");
                        ConsoleScreen.Log($"ZoneRotation:");
                        ConsoleScreen.Log($"Rotation Z: {zone.Rotation.Z}");
                        ConsoleScreen.Log($"Rotation Y: {zone.Rotation.Y}");
                        ConsoleScreen.Log($"Rotation X: {zone.Rotation.X}");
                        ConsoleScreen.Log($"Rotation W: {zone.Rotation.W}");
                        ConsoleScreen.Log($"ZoneType: {zone.ZoneType}");
                        if (!string.IsNullOrEmpty(zone.FlareType)) ConsoleScreen.Log($"FlareType: {zone.FlareType}");  
                        else ConsoleScreen.Log($"FlareType: N/A");
                        ConsoleScreen.Log($"ZoneLocation: {zone.ZoneLocation}");
                        ConsoleScreen.Log($"ZoneId: {zone.ZoneId}");
                        ConsoleScreen.Log($"ZoneName: {zone.ZoneName}");
                        ConsoleScreen.Log("-------------------------------------");
                        loadedZoneCount++;
                    }
                }
            }
            ConsoleScreen.Log("-------------------------------------");
            ConsoleScreen.Log($"Loaded CustomQuestZone Count: {loadedZoneCount}");
            ConsoleScreen.Log($"Player Map Location: {Singleton<GameWorld>.Instance.MainPlayer.Location}");
#endif   
            return request;
        }

        public static GameObject ZoneCreateItem(CustomQuestZone customQuestZone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y), float.Parse(customQuestZone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y), float.Parse(customQuestZone.Scale.Z));
            Quaternion rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y), float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;
            newZone.transform.rotation = rotation;

            EFT.Interactive.PlaceItemTrigger trigger = newZone.AddComponent<EFT.Interactive.PlaceItemTrigger>();
            trigger.SetId(customQuestZone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = customQuestZone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateVisit(CustomQuestZone customQuestZone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y), float.Parse(customQuestZone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y), float.Parse(customQuestZone.Scale.Z));
            Quaternion rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y), float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;
            newZone.transform.rotation = rotation;

            EFT.Interactive.ExperienceTrigger trigger = newZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            trigger.SetId(customQuestZone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = customQuestZone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateBotKillZone(CustomQuestZone customQuestZone)
        {
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            Vector3 position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y), float.Parse(customQuestZone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y), float.Parse(customQuestZone.Scale.Z));
            Quaternion rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y), float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;
            newZone.transform.rotation = rotation;

            EFT.Interactive.TriggerWithId trigger = newZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId(customQuestZone.ZoneId);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = customQuestZone.ZoneId;

            return newZone;
        }

        public static GameObject ZoneCreateFlareZone(CustomQuestZone customQuestZone)
        {
            // Thank you Groovey :)
            GameObject newZone = new GameObject();

            BoxCollider boxCollider = newZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;


            Vector3 position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y), float.Parse(customQuestZone.Position.Z));
            Vector3 scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y), float.Parse(customQuestZone.Scale.Z));
            Quaternion rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y), float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

            newZone.transform.position = position;
            newZone.transform.localScale = scale;
            newZone.transform.rotation = rotation;

            ZoneFlareTrigger flareTrigger = newZone.AddComponent<ZoneFlareTrigger>();
            flareTrigger.SetId(customQuestZone.ZoneId);

            MoveObjectsToAdditionalPhysSceneMarker moveObjectsToAdditionalPhysSceneMarker = newZone.AddComponent<MoveObjectsToAdditionalPhysSceneMarker>();

            FlareShootDetectorZone flareDetector = newZone.AddComponent<FlareShootDetectorZone>();

            Type flareDetectorType = typeof(FlareShootDetectorZone);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo zoneIDField = flareDetectorType.GetField("zoneID", bindingFlags);
            zoneIDField.SetValue(flareDetector, customQuestZone.ZoneId);

            FlareEventType flareType = (FlareEventType)Enum.Parse(typeof(FlareEventType), customQuestZone.FlareType);
            FieldInfo flareTypeForHandleField = flareDetectorType.GetField("flareTypeForHandle", bindingFlags);
            flareTypeForHandleField.SetValue(flareDetector, flareType);

            PhysicsTriggerHandler triggerHandler = newZone.AddComponent<PhysicsTriggerHandler>();
            triggerHandler.trigger = boxCollider;

            FieldInfo triggerHandlersField = flareDetectorType.GetField("_triggerHandlers", bindingFlags);
            List<PhysicsTriggerHandler> triggerHandlers = (List<PhysicsTriggerHandler>)triggerHandlersField.GetValue(flareDetector);
            triggerHandlers.Add(triggerHandler);

            newZone.layer = LayerMask.NameToLayer("Triggers");
            newZone.name = customQuestZone.ZoneId;

            return newZone;
        }


        public static void CreateZones(List<CustomQuestZone> zones)
        {
            foreach (CustomQuestZone zone in zones)
            {
                {
                    if (zone.ZoneType.ToLower() == "placeitem")
                    {
                        ZoneCreateItem(zone);
                    }

                    if (zone.ZoneType.ToLower() == "visit")
                    {
                        ZoneCreateVisit(zone);
                    }

                    if (zone.ZoneType.ToLower() == "flarezone")
                    {
                        ZoneCreateFlareZone(zone);
                    }

                    if (zone.ZoneType.ToLower() == "botkillzone")
                    {
                        ZoneCreateBotKillZone(zone);
                    }
                }
            }
        }
    }
}