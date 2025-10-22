using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Logger;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers
{
    [Injectable]
    public class BotLootHelper(DatabaseService databaseService, SptLogger<BotLootHelper> logger)
    {
        public void AddToBotLoot(CustomItemConfig itemConfig, string newItemId)
        {

            string cloneItemId = itemConfig.ItemTplToClone;
            var bots = databaseService.GetBots();

            foreach (var (_, bot) in bots.Types)
            {
                var items = bot?.BotInventory.Items;
                if (items == null) continue;

                var containers = new[]
                {
                    items.Backpack,
                    items.Pockets,
                    items.SecuredContainer,
                    items.SpecialLoot,
                    items.TacticalVest
                };

                foreach (var container in containers)
                {
                    foreach (var (existingItem, chance) in container)
                    {
                        if (existingItem.ToString() == cloneItemId)
                        {
                            container[new MongoId(newItemId)] = chance;
                            LogHelper.Debug(logger,$"Added {newItemId} to {container[new MongoId(newItemId)]}");
                            break;
                        }
                    }
                }
            }
        }
    }
}