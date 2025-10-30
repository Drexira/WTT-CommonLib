using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class WeaponPresetHelper(ISptLogger<WeaponPresetHelper> logger, DatabaseService databaseService)
{
    public void ProcessWeaponPresets(CustomItemConfig itemConfig, string itemId)
    {
        var itemPresets = databaseService.GetGlobals().ItemPresets;

        if (itemConfig.WeaponPresets == null || itemConfig.WeaponPresets.Count == 0)
        {
            logger.Warning($"WeaponPresets list is null or empty when trying {itemId}. Skipping.");
            return;
        }

        foreach (var preset in itemConfig.WeaponPresets)
        {
            if (preset.Items.Count == 0)
            {
                logger.Warning($"Preset {preset.Id} has no items defined. Skipping.");
                continue;
            }

            itemPresets[preset.Id] = preset;
        }
    }
}