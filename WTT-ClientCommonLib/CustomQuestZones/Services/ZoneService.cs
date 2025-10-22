using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using WTTClientCommonLib.Common.Helpers;
using WTTClientCommonLib.CustomQuestZones.Configuration;
using WTTClientCommonLib.CustomQuestZones.Models;

namespace WTTClientCommonLib.CustomQuestZones.Services
{
    public static class ZoneService
    {
        private static readonly List<CustomZoneContainer> Zones = new();
        private static int _currentSelectIndex = -1;
        private static string _selectedZoneName = "No CustomQuestZone Selected";
        

        public static void AddExistingZones()
        {
            ZoneConfigManager.ExistingQuestZones.ForEach(questZone =>
            {
                GameObject cube = Utils.CreateNewZoneCube(questZone.ZoneName);
                if (cube == null) return;

                Vector3 position = new Vector3(float.Parse(questZone.Position.X), float.Parse(questZone.Position.Y), float.Parse(questZone.Position.Z));
                Vector3 scale = new Vector3(float.Parse(questZone.Scale.X), float.Parse(questZone.Scale.Y), float.Parse(questZone.Scale.Z));
                Quaternion rotation = new Quaternion(float.Parse(questZone.Rotation.X), float.Parse(questZone.Rotation.Y), float.Parse(questZone.Rotation.Z), float.Parse(questZone.Rotation.W));

                cube.transform.position = position;
                cube.transform.rotation = rotation;
                cube.transform.localScale = scale;

                CustomZoneContainer customZoneContainer = new CustomZoneContainer(cube, questZone.ZoneType, questZone.FlareType);
                Zones.Add(customZoneContainer);
            });
            ZoneConfigManager.ExistingQuestZones.Clear();
        }

        public static void CreateNewZone()
        {
            var name = ZoneConfigManager.NewZoneName.Value;
            if (string.IsNullOrWhiteSpace(name) || Zones.Exists(z => z.GameObject.name == name))
                return;

            var type = ZoneConfigManager.NewZoneType.Value;
            var flare = string.IsNullOrEmpty(ZoneConfigManager.FlareZoneType.Value) ? "" : ZoneConfigManager.FlareZoneType.Value;
            var obj = Utils.CreateNewZoneCube(name);
            if (obj == null) return;

            Zones.Add(new CustomZoneContainer(obj, type, flare));
            Console.WriteLine($"Created new zone: {name}, total zones: {Zones.Count}");
        }

        public static void NextZone()
        {
            if (Zones.Count < 1) return;
            
            if (_currentSelectIndex >= 0) 
                Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ZoneConfigManager.ColorZoneRed;

            if (_currentSelectIndex + 1 < Zones.Count) 
                _currentSelectIndex++;
            else 
                _currentSelectIndex = 0;

            Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ZoneConfigManager.ColorZoneGreen;
            _selectedZoneName = Zones[_currentSelectIndex].GameObject.name;
            AdjustConfigValues();
        }

        public static void PrevZone()
        {
            if (Zones.Count < 1) return;
            
            if (_currentSelectIndex >= 0) 
                Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ZoneConfigManager.ColorZoneRed;

            if (_currentSelectIndex - 1 < 0) 
                _currentSelectIndex = Zones.Count - 1;
            else 
                _currentSelectIndex--;

            Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color = ZoneConfigManager.ColorZoneGreen;
            _selectedZoneName = Zones[_currentSelectIndex].GameObject.name;
            AdjustConfigValues();
        }

        public static string GetCurrentZoneName()
        {
            return _selectedZoneName;
        }

        public static void AdjustPosition()
        {
            if (Zones.Count < 1 || _currentSelectIndex < 0) return;
            
            GameObject currentZone = Zones[_currentSelectIndex].GameObject;
            Vector3 newPosition = new Vector3(ZoneConfigManager.PositionConfigX.Value, ZoneConfigManager.PositionConfigY.Value, ZoneConfigManager.PositionConfigZ.Value);
            currentZone.transform.position = newPosition;
        }

        public static void AdjustScale()
        {
            if (Zones.Count < 1 || _currentSelectIndex < 0) return;
            
            GameObject currentZone = Zones[_currentSelectIndex].GameObject;
            Vector3 newScale = new Vector3(ZoneConfigManager.ScaleConfigX.Value, ZoneConfigManager.ScaleConfigY.Value, ZoneConfigManager.ScaleConfigZ.Value);
            currentZone.transform.localScale = newScale;
        }

        public static void AdjustRotation()
        {
            if (Zones.Count < 1 || _currentSelectIndex < 0) return;
            
            GameObject currentZone = Zones[_currentSelectIndex].GameObject;
            Quaternion newRotation = Quaternion.Euler(ZoneConfigManager.RotationConfigX.Value, ZoneConfigManager.RotationConfigY.Value, ZoneConfigManager.RotationConfigZ.Value);
            currentZone.transform.rotation = newRotation;
        }

        public static void OutputZones()
        {
            if (Zones.Count < 1) return;
            
            string outputDir = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\"));
            string path = Path.Combine(outputDir, $"WTT-ClientCommonLib-CustomQuestZone-Output-{DateTime.Now:yyyyMMddHHmmssffff}.json");

            using (StreamWriter streamWriter = File.CreateText(path))
            {
                List<CustomQuestZone> convertedZones = Utils.ConvertZoneFormat(Zones, Utils.GetLocationId());
                Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                serializer.Serialize(streamWriter, convertedZones);
            }

            Console.WriteLine($"WTT-ClientCommonLib: Output zones to file: {path}");
        }

        private static void AdjustConfigValues()
        {
            if (Zones.Count < 1 || _currentSelectIndex < 0) return;
            
            GameObject currentZone = Zones[_currentSelectIndex].GameObject;

            ZoneConfigManager.PositionConfigX.Value = currentZone.transform.position.x;
            ZoneConfigManager.PositionConfigY.Value = currentZone.transform.position.y;
            ZoneConfigManager.PositionConfigZ.Value = currentZone.transform.position.z;

            ZoneConfigManager.ScaleConfigX.Value = currentZone.transform.localScale.x;
            ZoneConfigManager.ScaleConfigY.Value = currentZone.transform.localScale.y;
            ZoneConfigManager.ScaleConfigZ.Value = currentZone.transform.localScale.z;

            Vector3 rotationEulerAngles = currentZone.transform.rotation.eulerAngles;
            ZoneConfigManager.RotationConfigX.Value = rotationEulerAngles.x;
            ZoneConfigManager.RotationConfigY.Value = rotationEulerAngles.y;
            ZoneConfigManager.RotationConfigZ.Value = rotationEulerAngles.z;
        }

    }
}