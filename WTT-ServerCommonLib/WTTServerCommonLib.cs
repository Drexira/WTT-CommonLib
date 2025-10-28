﻿using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using WTTServerCommonLib.Services;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace WTTServerCommonLib;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.grooveypenguinx.WTT-ServerCommonLib";
    public override string Name { get; init; } = "WTT-ServerCommonLib";
    public override string Author { get; init; } = "GrooveypenguinX";
    public override List<string>? Contributors { get; init; }
    public override Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("4.0.1");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "WTT";
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class WTTServerCommonLib(
    WTTCustomItemServiceExtended customItemServiceExtended,
    WTTCustomAssortSchemeService customAssortSchemeService,
    WTTCustomLootspawnService customLootspawnService,
    WTTCustomQuestService customQuestService,
    WTTCustomLocaleService customLocaleService,
    WTTCustomHideoutRecipeService hideoutRecipeService,
    WTTCustomBotLoadoutService botLoadoutService,
    WTTCustomClothingService customClothingService,
    WTTCustomHeadService customHeadService,
    WTTCustomVoiceService customVoiceService,
    WTTCustomQuestZoneService customQuestZoneService,
    WTTCustomRigLayoutService customRigLayoutService,
    WTTCustomSlotImageService customSlotImageService,
    WTTCustomStaticSpawnService customStaticSpawnService,
    ISptLogger<WTTServerCommonLib> logger
) : IOnLoad
{
    public WTTCustomItemServiceExtended CustomItemServiceExtended { get; } = customItemServiceExtended;
    public WTTCustomAssortSchemeService CustomAssortSchemeService { get; } = customAssortSchemeService;
    public WTTCustomLootspawnService CustomLootspawnService { get; } = customLootspawnService;
    public WTTCustomQuestService CustomQuestService { get; } = customQuestService;
    public WTTCustomLocaleService CustomLocaleService { get; } = customLocaleService;
    public WTTCustomHideoutRecipeService CustomHideoutRecipeService { get; } = hideoutRecipeService;
    public WTTCustomBotLoadoutService CustomBotLoadoutService { get; } = botLoadoutService;
    public WTTCustomClothingService CustomClothingService { get; } = customClothingService;
    public WTTCustomHeadService CustomHeadService { get; } = customHeadService;
    public WTTCustomVoiceService CustomVoiceService { get; } = customVoiceService;
    public WTTCustomQuestZoneService CustomQuestZoneService { get; } = customQuestZoneService;
    public WTTCustomRigLayoutService CustomRigLayoutService { get; } = customRigLayoutService;
    public WTTCustomSlotImageService CustomSlotImageService { get; } = customSlotImageService;
    public WTTCustomStaticSpawnService CustomStaticSpawnService { get; } = customStaticSpawnService;

    public Task OnLoad()
    {
        return Task.CompletedTask;
    }
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class WTTServerCommonLibPostSptLoad(WTTCustomItemServiceExtended customItemServiceExtended) : IOnLoad
{
    public Task OnLoad()
    {
        customItemServiceExtended.ProcessDeferredModSlots();
        return Task.CompletedTask;
    }
}