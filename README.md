# WTT-CommonLib

WTT-CommonLib Modding Guide


Overview

WTT-CommonLib is a modding library for SPT that simplifies adding custom content to Escape from Tarkov. Instead of manually editing game databases and creating complex integrations, you can    use WTT -CommonLib's services to automatically handle everything from items, quests, voices, clothing, and more!


The library handles both server-side database modifications and client-side resource loading automatically. You just need to configure your content and call the appropriate services.

Getting Started


Installation

1. Download WTT-CommonLib from GitHub/Forge

2. Install the client component to  BepInEx/plugins/

3. Install the server component to  SPT/user/mods/


Basic Mod Structure

Here's a simple example of a mod using WTT -CommonLib:

```
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;

namespace WTTTheLongLostHeadsOfYojenkz;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.GrooveypenguinX.WTT-TheLongLostHeadsOfYojenkz";
    public override string Name { get; init; } = "WTT-TheLongLostHeadsOfYojenkz";
    public override string Author { get; init; } = "GrooveypenguinX";
    public override List<string>? Contributors { get; init; } = null;
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("4.0.1");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 20)]
public class WTTTheLongLostHeadsOfYojenkz(
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{

    public Task OnLoad()
    {
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        wttCommon.CustomHeadService.CreateCustomHeads(assembly);
        wttCommon.CustomVoiceService.CreateCustomVoices(assembly);
        
        return Task.CompletedTask;
    }
}
```


- Inject WTTServerCommonLib.WTTServerCommonLib through the constructor

- Set TypePriority = OnLoadOrder.PostDBModLoader + 20 to load after the database

- Get your assembly with  Assembly.GetExecutingAssembly()

- Pass your assembly to the services you want to use


Available Services


CustomItemServiceExtended

What it does: Creates custom items (weapons, armor  , consumables, etc.) and integrates them into traders, loot tables, and bot loadouts.

How to use:

`
wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
`

or alternatively you can pass it your custom path to your item .jsons like this:

`
wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, Path.Join("db", "MyCustomItemFolder"));
`

Configuration: Create JSON files in  db/CustomItems/ in your mod folder (or use your custom path to your .jsons):

```
  "6761b213607f9a6f79017aef": {
    "itemTplToClone": "572b7adb24597762ae139821",
    "parentId": "6815465859b8c6ff13f94026",
    "handbookParentId": "5b5f6f8786f77447ed563642",
    "overrideProperties": {
      "ExaminedByDefault": true,
      "Prefab": {
        "path": "Gear_Belts/belt_fannypack.bundle",
        "rcid": ""
      },
      "Width": 2,
      "Height": 2,
      "Weight": 0.46,
      "Grids": [
        {
          "_id": "belt_fannypackgrid",
          "_name": "main",
          "_parent": "6761b213607f9a6f79017aef",
          "_props": {
            "cellsH": 3,
            "cellsV": 2,
            "filters": [
              {
                "Filter": [
                  "54009119af1c881c07000029"
                ]
              }
            ],
            "isSortingTable": false,
            "maxCount": 0,
            "maxWeight": 0,
            "minCount": 0
          },
          "_proto": "55d329c24bdc2d892f8b4567"
        }
      ]
    },
    "locales": {
      "en": {
        "name": "Fanny Pack",
        "shortName": "Fanny Pack",
        "description": "A fanny pack that can be worn at the waist and carry a moderate amount of items."
      }
    },
    "addtoInventorySlots": [
      "ArmBand"
    ],
    "addtoModSlots": false,
    "modSlot": [],
    "fleaPriceRoubles": 10900,
    "handbookPriceRoubles": 7250,
    "addtoTraders": true,
    "addtoBots": false,
    "addtoStaticLootContainers": false,
    "staticLootContainers": [
      {
        "containerName": "LOOTCONTAINER_WEAPON_BOX_4X4",
        "probability": 250
      },
      {
        "containerName": "LOOTCONTAINER_DEAD_SCAV",
        "probability": 150
      }
    ],
    "masteries": false,
    "addWeaponPreset": false,
    "weaponPresets": [],
    "traders": {
      "RAGMAN": {
        "681ce253b2fd4632d780ca88": {
          "barterSettings": {
            "loyalLevel": 1,
            "unlimitedCount": true,
            "stackObjectsCount": 99
          },
          "barters": [
            {
              "count": 26125,
              "_tpl": "MONEY_ROUBLES"
            }
          ]
        }
      }
    },
    "masterySections": []
  }
```

Key features:

- Clone existing items and modify properties

- Add to trader inventories with custom barters

- Add to bot loadouts (anywhere the item you're cloning exists, at the same chance. So if you clone bandages, have addtobots true, your item will be added anywhere bandages are at the same chance)

- Add to loot containers

- Supports weapon presets

- Supports weapon masteries

- Hall of Fame integration

- Generator Fuel integration

- Hideout Poster integration

- Hideout Statuette integration

- Supports pushing to player inventory slots (i.e. a shotgun in your pistol slot)

- Supports pushing to player special slots

- Supports adding new bullets to other similar caliber weapons/items

- Supports pushing your item to other mod slots where the item you're cloning exists (i.e. clone an ak mag, add it to "mod_magazine" slots, it will go anywhere the ak mag exists)



CustomLocaleService

What it does: Handles translations for all your custom content.

How to use:

`
wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
`

Configuration: Create locale files in  db/CustomLocales/:
en.json - English

ru.json - Russian

de.json - German

etc.

`
{
"my_custom_weapon_001 Name": "Custom Assault Rifle", 
"my_custom_weapon_001 ShortName": "CAR",
"my_custom_weapon_001 Description": "A powerful custom rifle", 
"my_custom_quest_001 name": "Custom Quest Name", 
"my_custom_quest_001 description": "Quest description here"
}
`

CustomQuestService

What it does: Adds custom quests to the database

How to use:

`
wttCommon.CustomQuestService.CreateCustomQuests(assembly);
`

Configuration: Create quest files in  db/CustomQuests/:




Key features:

Time-limited quests

Faction restrictions (BEAR/USEC only quests)

Locale support, Side Specific Quest support, and time/date locking!



CustomQuestZoneService

What it does: Manages custom quest zones for Visit, PlaceItem, and other location-based objectives.

How to use:

`
wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
`

Configuration: Place zone files in  db/CustomQuestZones/. I recommend you use the in-game zone editor to create them:

`
  {
    "ZoneId": "deadbody_1",
    "ZoneName": "deadbody_1",
    "ZoneLocation": "woods",
    "ZoneType": "placeitem",
    "FlareType": "",
    "Position": {
      "X": "91.6219",
      "Y": "16.7",
      "Z": "-845.9562",
      "W": "0"
    },
    "Rotation": {
      "X": "0",
      "Y": "0",
      "Z": "0",
      "W": "1"
    },
    "Scale": {
      "X": "1.5",
      "Y": "3.25",
      "Z": "1.75",
      "W": "0"
    }
  }
`

In-game zone editor: Press F12 in-game, navigate to WTT -ClientCommonLib settings to create and position zones visually  .



CustomVoiceService

What it does: Adds custom character voices for players and bots.

How to use:

`
wttCommon.CustomVoiceService.CreateCustomVoices(assembly);
`

Configuration: Create voice config files in  config/voices/:

`
{
    "6747aa4495b4845a0f3d9f98": {
        "locales": {
            "en": "Duke"
        },
        "name": "Duke",
        "bundlePath": "voices/Duke/Voices/duke_voice.bundle",
        "addVoiceToPlayer": true
    },
}
`

Asset requirements: Package voice audio into a Unity AssetBundle and place it in your mod's bundles/ folder and your bundles.json.



CustomHeadService

What it does: Adds custom character head models for player customization.

How to use:

`
wttCommon.CustomHeadService.CreateCustomHeads(assembly);
`

Configuration: Create head config files in  config/heads/:
`
{
    "6747aa715be2c2e443264f32":{
        "path": "heads/chrishead.bundle",
        "addHeadToPlayer": true,
        "side": ["Bear", "Usec"],
        "locales": {
            "en": "Chris Redfield"
        }
    },
}
`
Asset requirements: Package head models into Unity  AssetBundles and place them in your mod's bundles/ folder.

CustomClothingService

What it does: Adds custom clothing sets (outfits, tops, bottoms) for players.

How to use:

`
wttCommon.CustomClothingService.CreateCustomClothing(assembly);
`

Configuration: Create clothing config files in  db/CustomClothing/:

`
{
	{
		"type": "top",
		"suiteId": "6748037e298128d377dfffd0",
		"outfitId": "67480381bd1eb568c78598df",
		"topId": "67480383b253d50226f3becd",
		"handsId": "67480396eda19f232a648533",
		"locales": {
			"en": "Lara's Tattered Tank Top"
		},
		"topBundlePath": "clothing/lara_top.bundle",
		"handsBundlePath": "clothing/lara_hands.bundle",
		"traderId": "RAGMAN",
		"loyaltyLevel": 1,
		"profileLevel": 1,
		"standing": 0,
		"currencyId": "ROUBLES",
		"price": 150
	}
}
`

CustomBotLoadoutService

What it does: Customizes  AI bot equipment, weapons, and appearance.

How to use:

`
wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
`

Configuration: Create bot loadout files in config/bots/:

`
{
	"chances": {
		"equipment": {
			"FirstPrimaryWeapon": 100
		},
		"weaponMods": {
			"mod_stock": 100,
			"mod_magazine": 100,
			"mod_mount_000": 100,
			"mod_charge": 70,
			"mod_foregrip": 70,
			"mod_tactical_002": 65,
			"mod_pistol_grip": 100
		}
	},
	"inventory": {
		"equipment": {
			"FirstPrimaryWeapon": {
				"679a6a534f3d279c99b135b9": 500
			}
		},
		"mods": {
			"679a6a534f3d279c99b135b9": {
				"mod_stock": [
					"679a6e58085b54fdd56f5d0d"
				],
				"mod_magazine": [
					"679a702c47bb7fa666fe618e"
				],
				"mod_mount_000": [
					"57486e672459770abd687134"
				],
				"mod_charge": [
					"5648ac824bdc2ded0b8b457d"
				],
				"mod_foregrip": [
					"588226e62459776e3e094af7"
				],
				"mod_tactical_002": [
					"5c5952732e2216398b5abda2"
				],
				"mod_pistol_grip": [
					"679a766855f7e9fa7b1abfdf"
				]
			},
			"679a6e58085b54fdd56f5d0d": {
				"mod_stock": [
					"5a0c59791526d8dba737bba7"
				]
			}
		},
		"Ammo": {
			"Caliber545x39": {
				"61962b617c6c7b169525f168": 1,
				"56dff061d2720bb5668b4567": 1,
				"56dfef82d2720bbd668b4567": 1
			}
		}
	}
}
`


CustomLootspawnService

What it does: Controls where and how often your custom items spawn as loot, and forced loot for quest objectives

How to use:

`
wttCommon.CustomLootspawnService.CreateCustomLootspawns(assembly);
`


CustomAssortSchemeService

What it does: Adds complex items to trader inventory and barter schemes.

How to use:

`
wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
`

Configuration: Usually reserved for complex items like weapons, you can create standalone assort files in db/CustomAssortSchemes/


CustomHideoutRecipeService

What it does: Creates crafting recipes for the hideout.

How to use:

`
wttCommon.CustomHideoutRecipeService.CreateCustomHideoutRecipes(assembly);
`

Configuration: Create recipe files in  db/CustomHideoutRecipes/:


CustomRigLayoutService

What it does: Creates custom rig layouts and sends them to the client for your items to utilize.

How to use:

`
wttCommon.CustomRigLayoutService.CreateCustomRigLayouts(assembly);
`

Asset requirements: Package rig layout GameObjects with  ContainedGridsView components into Unity AssetBundles and place them in   db/CustomRigLayouts/.

CustomSlotImageService

What it does: Provides custom inventory slot icons for unique items.

How to use:

`
wttCommon.CustomSlotImageService.CreateCustomSlotImages(assembly);
`

Configuration: Place images in  db/CustomSlotImages/ in your mod folder. Name them according to the slot ID you want to customize. This will also be the key you use if you want to support multiple Locales.


Typical Mod Workflow


1. Custom Weapon Mod Example

Mod structure:


MyWeaponMod/ 
├──  bundles/
│	├──  MyCustomWeapon.bundle
├──  db/
│	├──  CustomItems/
│	│	└──  weapons.json 
│	└──  CustomAssortSchemes/
|       └──  praporAssort.json
|──  bundles.json
└──  MyWeaponMod.dll


Code:
```
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Servers;
using Path = System.IO.Path;
using Range = SemanticVersioning.Range;

namespace WTTArmory;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.GrooveypenguinX.MyWeaponMod";
    public override string Name { get; init; } = "MyWeaponMod";
    public override string Author { get; init; } = "GrooveypenguinX";
    public override List<string>? Contributors { get; init; } = null;
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("4.0.1");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 20)]
public class MyWeaponMod(
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    WTTBot wttBot) : IOnLoad
{

    public Task OnLoad()
    {
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
        
        return Task.CompletedTask;
    }
}
```



Debugging

Check the SPT server console for errors during startup

Review user/logs/ for detailed error messages

Enable BepInEx console in-game to see client-side loading




For issues, questions, or contributions:

GitHub: https://github.com/WelcomeToTarkov/WTT-CommonLib
