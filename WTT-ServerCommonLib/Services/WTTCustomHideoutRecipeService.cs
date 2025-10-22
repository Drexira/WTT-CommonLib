using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using WTTServerCommonLib.Helpers;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace WTTServerCommonLib.Services;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class WTTCustomHideoutRecipeService(
    ISptLogger<WTTCustomHideoutRecipeService> logger,
    DatabaseServer databaseServer,
    ModHelper modHelper,
    ConfigHelper configHelper
    )
{
    private DatabaseTables? _database;
    
    public void CreateHideoutRecipes(Assembly assembly, string? relativePath = null)
    {
        try
        {
            string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            string defaultDir = Path.Combine("db", "CustomHideoutRecipes");
            string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (_database == null)
            {
                _database = databaseServer.GetTables();
            }
            if (!Directory.Exists(finalDir))
            {
                logger.Error($"Directory not found at {finalDir}");
                return;
            }

            var recipes = configHelper.LoadAllJsonFiles<HideoutProduction>(finalDir);

            if (recipes.Count == 0)
            {
                logger.Warning($"No valid hideout recipes found in {finalDir}");
                return;
            }

            foreach (var recipe in recipes)
            {

                if (!MongoId.IsValidMongoId(recipe.Id))
                {
                    logger.Error($"Missing or invalid Id in recipe for end product {recipe.EndProduct}");
                    continue;
                }

                bool recipeExists = _database.Hideout.Production.Recipes != null && _database.Hideout.Production.Recipes.Any(r => r.Id == recipe.Id);
                if (recipeExists)
                {
                    if (logger.IsLogEnabled(LogLevel.Debug))
                    {
                        LogHelper.Debug(logger,$"Recipe {recipe.Id} already exists, skipping");
                    }
                    continue;
                }

                _database.Hideout.Production.Recipes?.Add(recipe);
                LogHelper.Debug(logger,$"Added hideout recipe {recipe.Id} for item {recipe.EndProduct}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading hideout recipes: {ex.Message}");
        }
    }

}