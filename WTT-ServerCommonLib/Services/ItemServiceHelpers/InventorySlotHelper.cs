﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class InventorySlotHelper(ISptLogger<InventorySlotHelper> logger, DatabaseService databaseService)
{
    public void ProcessInventorySlots(CustomItemConfig itemConfig, string itemId)
    {
        if (itemConfig.AddToInventorySlots == null)
            return;

        const string pmcInventoryTemplateId = "55d7217a4bdc2d86028b456d";
        
        var items = databaseService.GetItems();
        var defaultInventorySlots = items[pmcInventoryTemplateId].Properties?.Slots;
        if (defaultInventorySlots == null)
            return;

        var allowedSlots = itemConfig.AddToInventorySlots
            .Select(slot => slot.ToLower())
            .ToList();

        foreach (var slot in defaultInventorySlots)
        {
            var filtersList = slot.Properties?.Filters?.ToList();
            if (filtersList == null || filtersList.Count == 0)
                continue;

            var slotNameLower = slot.Name?.ToLower();
            if (slotNameLower == null)
                continue;

            if (allowedSlots.Contains(slotNameLower))
            {
                var firstFilter = filtersList.FirstOrDefault();
                if (firstFilter?.Filter == null)
                    continue;

                if (firstFilter.Filter.Add(itemId))
                {
                    logger.Info($"[InventorySlots] Added {itemId} to inventory slot '{slot.Name}'");
                }
            }
        }
    }
}