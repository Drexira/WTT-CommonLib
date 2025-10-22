using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib.Services;

namespace WTTServerCommonLib.Routes
{
    [Injectable]
    public class WTTResourcesRouter(
        JsonUtil jsonUtil,
        WTTCustomQuestZoneService zoneService,
        WTTCustomRigLayoutService rigService,
        WTTCustomSlotImageService slotService,
        WTTCustomStaticSpawnService staticSpawnService,
        WTTCustomVoiceBundleRequestService customVoiceBundleRequestService
    ) : DynamicRouter(jsonUtil, [
        // Zones
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/zones/get", (_, _, _, _) => {
                var zones = zoneService.GetZones();
                return ValueTask.FromResult(jsonUtil.Serialize(zones) ?? string.Empty);
            }
        ),
        
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/riglayouts/get", (_, _, _, _) => {
                var allBundles = rigService.GetLayoutManifest();
                var payload = new Dictionary<string, string>();
                foreach (var bundleName in allBundles)
                {
                    var bundleData = rigService.GetBundleData(bundleName);
                    if (bundleData is { Length: > 0 })
                        payload[bundleName] = Convert.ToBase64String(bundleData);
                }
                return ValueTask.FromResult(jsonUtil.Serialize(payload) ?? string.Empty);
            }
        ),
        // Bundles route
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/spawnsystem/bundles/get", (_, _, _, _) =>
            {
                var manifest = staticSpawnService.GetBundleManifest();
                var payload = new Dictionary<string,string>();
                foreach (var name in manifest)
                {
                    var data = staticSpawnService.GetBundleData(name);
                    if (data?.Length > 0)
                        payload[name] = Convert.ToBase64String(data);
                }
                return ValueTask.FromResult(jsonUtil.Serialize(payload) ?? string.Empty);
            }
        ),

        // Configs route
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/spawnsystem/configs/get", (_, _, _, _) =>
            {
                var configs = staticSpawnService.GetAllSpawnConfigs();
                return ValueTask.FromResult(jsonUtil.Serialize(configs) ?? string.Empty);
            }
        ),

        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/slotimages/get", (_, _, _, _) => {
                var result = new Dictionary<string, string>();
                foreach (var name in slotService.GetImageManifest()) {
                    var data = slotService.GetImageData(name);
                    if (data is { Length: > 0 }) {
                        result[name] = Convert.ToBase64String(data);
                    }
                }
                return ValueTask.FromResult(jsonUtil.Serialize(result) ?? string.Empty);
            }
        ),
        // Voices
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/voices/get", (_, _, _, _) => {
                var voiceMappings = customVoiceBundleRequestService.GetVoiceBundleMappings();
                return ValueTask.FromResult(jsonUtil.Serialize(voiceMappings) ?? string.Empty);
            }
        ),
    ])
    { }
}
