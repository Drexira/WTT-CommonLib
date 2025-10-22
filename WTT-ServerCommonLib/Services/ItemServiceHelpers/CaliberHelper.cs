using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class CaliberHelper(ISptLogger<CaliberHelper> logger, DatabaseService databaseService)
{
    public void AddNewCaliberToItems(CustomItemConfig itemConfig, string newItemId)
    {
        var tables = databaseService.GetTables();
        var items = tables.Templates.Items;

        foreach (var (itemId, item) in items)
        {
            UpdateItemFilters(item, itemConfig.ItemTplToClone, newItemId, itemId);
        }

    }

    private void UpdateItemFilters(TemplateItem item, string cloneId, string newId, string itemId)
    {
        if (item.Properties?.Cartridges != null)
        {
            foreach (var cartridge in item.Properties.Cartridges)
            {
                if (cartridge.Properties?.Filters != null)
                {
                    UpdateFilters(cartridge.Properties.Filters, cloneId, newId, itemId);
                }
            }
        }

        if (item.Properties?.Slots != null)
        {
            foreach (var slot in item.Properties.Slots)
            {
                if (slot.Properties?.Filters != null)
                {
                    UpdateFilters(slot.Properties.Filters, cloneId, newId, itemId);
                }
            }
            
        }

        if (item.Properties?.Chambers != null)
        {
            foreach (var chamber in item.Properties.Chambers)
            {
                if (chamber.Properties?.Filters != null)
                {
                    UpdateFilters(chamber.Properties.Filters, cloneId, newId, itemId);
                }
            }
        }
    }

    private void UpdateFilters(IEnumerable<SlotFilter> filters, string cloneId, string newId, string itemId)
    {
        foreach (var filter in filters)
        {
            if (filter.Filter != null && filter.Filter.Contains(cloneId) && filter.Filter.Add(newId))
            {
                LogHelper.Debug(logger,$"Added {newId} to filter in {itemId}");
            }
        }
    }
}