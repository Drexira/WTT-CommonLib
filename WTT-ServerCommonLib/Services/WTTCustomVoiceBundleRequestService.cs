using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Logger;

namespace WTTServerCommonLib.Services
{
    [Injectable]
    public class WTTCustomVoiceBundleRequestService(
        SptLogger<WTTCustomVoiceBundleRequestService> logger,
        ModHelper modHelper)
    {
        private readonly Dictionary<string, string> _voiceBundleMappings = new();
        private readonly Dictionary<string, string> _bundlePaths = new();
        private readonly object _lock = new();

        public void RegisterVoiceBundle(string voiceId, string bundlePath)
        {
            lock (_lock)
            {
                if (_voiceBundleMappings.TryAdd(voiceId, bundlePath))
                {
                    _bundlePaths[voiceId] = bundlePath;
                    logger.Info($"Registered voice bundle: {voiceId} -> {bundlePath}");
                }
                else
                {
                    logger.Warning($"Voice bundle {voiceId} already registered");
                }
            }
        }

        public Dictionary<string, string> GetVoiceBundleMappings()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_voiceBundleMappings);
            }
        }
    }
}