using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils.Logger;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services
{
    [Injectable]
    public class WTTCustomVoiceBundleRequestService(
        SptLogger<WTTCustomVoiceBundleRequestService> logger)
    {
        private readonly Dictionary<string, string> _voiceBundleMappings = [];
        private readonly Lock _lock = new();

        public void RegisterVoiceBundle(string voiceId, string bundlePath)
        {
            lock (_lock)
            {
                if (_voiceBundleMappings.TryAdd(voiceId, bundlePath))
                {
                    LogHelper.Debug(logger,$"Registered voice bundle: {voiceId} -> {bundlePath}");
                }
                else
                {
                    logger.Warning($"Voice bundle {voiceId} already registered");
                }
            }
        }

        public Dictionary<string, string> GetVoiceBundleMappings()
        {
            return _voiceBundleMappings;
        }
    }
}