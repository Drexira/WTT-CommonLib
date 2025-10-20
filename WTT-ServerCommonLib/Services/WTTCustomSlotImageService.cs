using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils.Logger;

namespace WTTServerCommonLib.Services
{
    [Injectable]
    public class WTTCustomSlotImageService(ModHelper modHelper, SptLogger<WTTCustomSlotImageService> logger)
    {
        private readonly Dictionary<string, string> _imagePaths = new();

        public void LoadSlotImages(Assembly assembly, string? relativePath = null)
        {
            string assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            string defaultDir = Path.Combine("db", "CustomSlotImages");
            string finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);
            
            if (!Directory.Exists(finalDir))
            {
                logger.Info($"No CustomSlotImages directory at {finalDir}");
                return;
            }

            string[] extensions = [".png", ".jpg", ".jpeg", ".bmp"];
            foreach (var imagePath in Directory.GetFiles(finalDir))
            {
                string ext = Path.GetExtension(imagePath).ToLowerInvariant();
                if (extensions.Contains(ext))
                {
                    string imageName = Path.GetFileNameWithoutExtension(imagePath);
                    _imagePaths[imageName] = imagePath;
                    logger.Info($"Registered slot image: {imageName}");
                }
            }
        }

        public List<string> GetImageManifest()
        {
            return _imagePaths.Keys.ToList();
        }

        public byte[]? GetImageData(string imageName)
        {
            if (_imagePaths.TryGetValue(imageName, out var path) && File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            return null;
        }
    }
}