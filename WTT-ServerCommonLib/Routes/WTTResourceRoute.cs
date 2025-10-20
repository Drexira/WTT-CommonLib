using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services;

namespace WTTServerCommonLib.Routes
{
    [Injectable]
    public class WTTResourcesRouter(
        JsonUtil jsonUtil,
        WTTCustomQuestZoneService zoneService,
        WTTCustomRigLayoutService rigService,
        WTTCustomSlotImageService slotService,
        WTTCustomVoiceService voiceService,
        WTTCustomVoiceBundleRequestService customVoiceBundleRequestService
    ) : DynamicRouter(jsonUtil, [
        // Zones
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/zones/get",
            async (url, info, sessionId, output) => {
                var zones = zoneService.GetZones();
                return jsonUtil.Serialize(zones ?? new List<CustomQuestZone>());
            }
        ),
        
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/riglayouts/get",
            async (url, info, sessionId, output) => {
                var allBundles = rigService.GetLayoutManifest();
                var payload = new Dictionary<string, string>();
                foreach (var bundleName in allBundles)
                {
                    var bundleData = rigService.GetBundleData(bundleName);
                    if (bundleData != null && bundleData.Length > 0)
                        payload[bundleName] = System.Convert.ToBase64String(bundleData);
                }
                return jsonUtil.Serialize(payload);
            }
        ),

        // Slot Images - Manifest
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/slotimages/get",
            async (url, info, sessionId, output) => {
                var images = slotService.GetImageManifest();
                return jsonUtil.Serialize(images ?? new List<string>());
            }
        ),
        
        // Slot Images - Data 
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/slotimages/data",
            async (url, info, sessionId, output) => {
                // Extract query string from URL
                string imageName = null;
                var queryIndex = url.IndexOf('?');
                if (queryIndex >= 0 && queryIndex + 1 < url.Length)
                {
                    var queryString = url.Substring(queryIndex + 1);
                    var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);
                    imageName = queryParams["name"];
                }
                
                if (string.IsNullOrEmpty(imageName))
                {
                    return jsonUtil.Serialize(new { error = "Missing image name" });
                }
                
                var imageData = slotService.GetImageData(imageName);
                if (imageData == null)
                {
                    return jsonUtil.Serialize(new { error = $"Image not found: {imageName}" });
                }
                
                return jsonUtil.Serialize(new { data = System.Convert.ToBase64String(imageData) });
            }
        ),
        
        // Voices
        new RouteAction<EmptyRequestData>(
            "/wttcommonlib/voices/get",
            async (url, info, sessionId, output) => {
                var voiceMappings = customVoiceBundleRequestService.GetVoiceBundleMappings();
                return jsonUtil.Serialize(voiceMappings ?? new Dictionary<string, string>());
            }
        ),
    ])
    { }
}
