# WTT-CommonLib

A comprehensive modding library for SPT that simplifies adding custom content to Escape from Tarkov. WTT-CommonLib handles both server-side database modifications and client-side resource loading automatically - you just configure your content and call the appropriate services.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Available Services](#available-services)
  - [CustomItemServiceExtended](#customitemserviceextended)
  - [CustomLocaleService](#customlocaleservice)
  - [CustomQuestService](#customquestservice)
  - [CustomQuestZoneService](#customquestzoneservice)
  - [CustomVoiceService](#customvoiceservice)
  - [CustomHeadService](#customheadservice)
  - [CustomClothingService](#customclothingservice)
  - [CustomBotLoadoutService](#custombotloadoutservice)
  - [CustomLootspawnService](#customlootspawnservice)
  - [CustomAssortSchemeService](#customassortschemeservice)
  - [CustomHideoutRecipeService](#customhideoutrecipeservice)
  - [CustomRigLayoutService](#customriglayoutservice)
  - [CustomSlotImageService](#customslotimageservice)
- [Example Mod Structure](#example-mod-structure)
- [Debugging](#debugging)
- [Support](#support)

## Features

**Simplified Item Creation** - Clone and modify items with JSON configs  
**Quest System** - Create custom quests with zone-based objectives  
**Character Customization** - Add heads, voices, and clothing  
**Bot Configuration** - Customize AI loadouts and equipment  
**Loot Management** - Control item spawns and distributions  
**Hideout Integration** - Add crafting recipes  
**Multi-language Support** - Easy localization system  


## Quick Start

Here's a minimal example showing how to use WTT-CommonLib:

```
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;

namespace YourModName;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.yourname.yourmod";
    public override string Name { get; init; } = "Your Mod Name";
    public override string Author { get; init; } = "Your Name";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("4.0.1");
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 20)]
public class YourMod(
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{
    public Task OnLoad()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        // Use WTT-CommonLib services
        wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
        
        return Task.CompletedTask;
    }
}
```

### Key Points:
- Inject `WTTServerCommonLib.WTTServerCommonLib` through the constructor
- Set `TypePriority = OnLoadOrder.PostDBModLoader + 20` to load after the database
- Get your assembly with `Assembly.GetExecutingAssembly()`
- Pass your assembly to the services you want to use

---

## Available Services

### CustomItemServiceExtended

**Purpose**: Creates custom items (weapons, armor, consumables, etc.) and integrates them into traders, loot tables, and bot loadouts.

**Usage**:
```
// Use default path (db/CustomItems/)
wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);

// Or specify custom path
wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, 
    Path.Join("db", "MyCustomItemFolder"));
```

**Configuration**: Place JSON files in `db/CustomItems/` (or your custom path):

<details>
<summary> Example Item Configuration (Click to expand)</summary>

```
{
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
                "Filter": ["54009119af1c881c07000029"]
              }
            ]
          }
        }
      ]
    },
    "locales": {
      "en": {
        "name": "Fanny Pack",
        "shortName": "Fanny Pack",
        "description": "A fanny pack that can be worn at the waist."
      }
    },
    "fleaPriceRoubles": 10900,
    "handbookPriceRoubles": 7250,
    "addtoInventorySlots": ["ArmBand"],
    "addtoTraders": true,
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
    }
  }
}
```
</details>

**Features**:
- Clone existing items and modify properties
- Add to trader inventories with custom barters
- Add to bot loadouts (inherits spawn chances from cloned item)
- Add to loot containers
- Weapon preset support
- Weapon mastery integration
- Hall of Fame integration
- Generator fuel integration
- Hideout poster/statuette integration
- Custom inventory slot placement
- Special slot support
- Caliber-based weapon compatibility
- Mod slot propagation (based on the cloned item's locations)

---

### CustomLocaleService

**Purpose**: Handles translations for all your custom content.

**Usage**:
```
wttCommon.CustomLocaleService.CreateCustomLocales(assembly);
```

**Configuration**: Create locale files in `db/CustomLocales/`:
- `en.json` - English
- `ru.json` - Russian
- `de.json` - German
- etc.

<details>
<summary> Example Locale File (Click to expand)</summary>

```
{
  "my_custom_weapon_001 Name": "Custom Assault Rifle",
  "my_custom_weapon_001 ShortName": "CAR",
  "my_custom_weapon_001 Description": "A powerful custom rifle",
  "my_custom_quest_001 name": "Custom Quest Name",
  "my_custom_quest_001 description": "Quest description here"
}
```
</details>

---

### CustomQuestService

**Purpose**: Adds custom quests to the database with support for complex objectives and rewards.

**Usage**:
```
wttCommon.CustomQuestService.CreateCustomQuests(assembly);
```

**Configuration**: Create quest files in `db/CustomQuests/`

**Features**:
- Time-limited quests (seasonal/date-based)
- Faction restrictions (BEAR/USEC only)
- Full locale support

---

### CustomQuestZoneService

**Purpose**: Manages custom quest zones for Visit, PlaceItem, and other location-based objectives.

**Usage**:
```
wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
```

**Configuration**: Place zone files in `db/CustomQuestZones/`

<details>
<summary> Example Zone Configuration (Click to expand)</summary>

```
{
  "ZoneId": "deadbody_1",
  "ZoneName": "deadbody_1",
  "ZoneLocation": "woods",
  "ZoneType": "placeitem",
  "FlareType": "",
  "Position": {
    "X": "91.6219",
    "Y": "16.7",
    "Z": "-845.9562"
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
    "Z": "1.75"
  }
}
```
</details>

**In-Game Editor**: Press **F12** in-game → Navigate to **WTT-ClientCommonLib** settings → Create and position zones visually

---

### CustomVoiceService

**Purpose**: Adds custom character voices for players and bots.

**Usage**:
```
wttCommon.CustomVoiceService.CreateCustomVoices(assembly);
```

**Configuration**: Create voice config files in `db/CustomVoices/`:

<details>
<summary> Example Voice Configuration (Click to expand)</summary>

```
{
  "6747aa4495b4845a0f3d9f98": {
    "locales": {
      "en": "Duke"
    },
    "name": "Duke",
    "bundlePath": "voices/Duke/Voices/duke_voice.bundle",
    "addVoiceToPlayer": true
  }
}
```
</details>

**Requirements**: Package voice audio into Unity AssetBundles → Place in `bundles/` folder → Add to `bundles.json`

---

### CustomHeadService

**Purpose**: Adds custom character head models for player customization.

**Usage**:
```
wttCommon.CustomHeadService.CreateCustomHeads(assembly);
```

**Configuration**: Create head config files in `db/CustomHeads/`:

<details>
<summary> Example Head Configuration (Click to expand)</summary>

```
{
  "6747aa715be2c2e443264f32": {
    "path": "heads/chrishead.bundle",
    "addHeadToPlayer": true,
    "side": ["Bear", "Usec"],
    "locales": {
      "en": "Chris Redfield"
    }
  }
}
```
</details>

**Requirements**: Package head models into Unity AssetBundles → Place in `bundles/` folder

---

### CustomClothingService

**Purpose**: Adds custom clothing sets (tops, bottoms) for players.

**Usage**:
```
wttCommon.CustomClothingService.CreateCustomClothing(assembly);
```

**Configuration**: Create clothing config files in `db/CustomClothing/`:

<details>
<summary> Example Clothing Configuration (Click to expand)</summary>

```
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
```
</details>

---

### CustomBotLoadoutService

**Purpose**: Customizes AI bot equipment, weapons, and appearance.

**Usage**:
```
wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
```

**Configuration**: Create bot loadout files in `db/CustomBotLoadouts/`:

<details>
<summary> Example Bot Loadout (Click to expand)</summary>

```
{
  "chances": {
    "equipment": {
      "FirstPrimaryWeapon": 100
    },
    "weaponMods": {
      "mod_stock": 100,
      "mod_magazine": 100,
      "mod_tactical_002": 65
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
        "mod_stock": ["679a6e58085b54fdd56f5d0d"],
        "mod_magazine": ["679a702c47bb7fa666fe618e"]
      }
    },
    "Ammo": {
      "Caliber545x39": {
        "61962b617c6c7b169525f168": 1
      }
    }
  }
}
```
</details>

---

### CustomLootspawnService

**Purpose**: Controls where and how often your custom items spawn as loot, plus forced loot for quest objectives.

**Usage**:
```
wttCommon.CustomLootspawnService.CreateCustomLootspawns(assembly);
```

**Configuration**: Create loot spawn files in `db/CustomLootspawns/`

---

### CustomAssortSchemeService

**Purpose**: Adds complex items to trader inventory with custom barter schemes.

**Usage**:
```
wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
```

**Configuration**: Create assort files in `db/CustomAssortSchemes/`

> **Note**: Usually reserved for complex items like fully-modded weapons, or armor with plates

---

### CustomHideoutRecipeService

**Purpose**: Creates crafting recipes for the hideout.

**Usage**:
```
wttCommon.CustomHideoutRecipeService.CreateCustomHideoutRecipes(assembly);
```

**Configuration**: Create recipe files in `db/CustomHideoutRecipes/`

---

### CustomRigLayoutService

**Purpose**: Creates custom rig layouts and sends them to the client for your items to utilize.

**Usage**:
```
wttCommon.CustomRigLayoutService.CreateCustomRigLayouts(assembly);
```

**Requirements**: Package rig layout GameObjects with `ContainedGridsView` components into Unity AssetBundles → Place in `db/CustomRigLayouts/`

---

### CustomSlotImageService

**Purpose**: Provides custom inventory slot icons for unique items.

**Usage**:
```
wttCommon.CustomSlotImageService.CreateCustomSlotImages(assembly);
```

**Configuration**: Place PNG images in `db/CustomSlotImages/` → Name them by slot ID → Image name becomes the locale key

---

## Example Mod Structure

### Custom Weapon Mod Structure

```
MyWeaponMod/
├── bundles/
│ └── MyCustomWeapon.bundle
├── db/
│ ├── CustomItems/
│ │ └── weapons.json
│ └── CustomAssortSchemes/
│ 	└── praporAssort.json
├── bundles.json
└── MyWeaponMod.dll
```
