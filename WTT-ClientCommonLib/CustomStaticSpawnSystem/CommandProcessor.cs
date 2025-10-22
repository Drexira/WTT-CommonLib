using EFT.Console.Core;
using EFT.UI;
using WTTClientCommonLib.Common.Helpers;

namespace WTTClientCommonLib.CustomStaticSpawnSystem;

public class CommandProcessor(PlayerWorldStats playerWorldStats, SpawnCommands spawnCommands)
{
    public void RegisterCommandProcessor()
    {
        ConsoleScreen.Processor.RegisterCommand("clear", delegate ()
        {
            MonoBehaviourSingleton<PreloaderUI>.Instance.Console.Clear();
        });
        if (StaticSpawnSystemConfigManager.DebugMode.Value)
        {
            ConsoleScreen.Processor.RegisterCommand("GetPlayerWorldStats",
                delegate() { playerWorldStats.GetPlayerWorldStats(); });
            ConsoleScreen.Processor.RegisterCommand("EnterEditMode",
                delegate() { spawnCommands.StartEditMode(); });
            ConsoleScreen.Processor.RegisterCommand("ExitEditMode",
                delegate() { spawnCommands.ExitEditMode(); });
            ConsoleScreen.Processor.RegisterCommand("ExportSpawnedObjectInfo",
                delegate() { spawnCommands.ExportSpawnedObjectsLocations();
                });

            ConsoleScreen.Processor.RegisterCommandGroup<AdvancedConsoleCommands>();
        }
        
    }
    
}

public class AdvancedConsoleCommands(SpawnCommands spawnCommands)
{
    [ConsoleCommand("SpawnObject",
        "Spawn Static Object using bundle name, prefab name",
        "<String>, <String>", "", new string[] { })]
    public void SpawnObject(string bundleName, string prefabName)
    {
        spawnCommands.SpawnObject(bundleName, prefabName);
    }
}