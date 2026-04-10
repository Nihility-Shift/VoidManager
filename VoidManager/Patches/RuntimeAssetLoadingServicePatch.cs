using HarmonyLib;
using RuntimeAssets;
using VoidManager.Content;

namespace VoidManager.Patches
{
    /// <summary>
    /// Subscribes from post constructor to run as early as possible.
    /// </summary>
    [HarmonyPatch(typeof(RuntimeAssetLoadingService), MethodType.Constructor)]
    class RuntimeAssetLoadingServicePatch
    {
        static void Postfix(RuntimeAssetLoadingService __instance)
        {
            __instance.OnIntialized += SupportedContentLoader.Initialize;
        }
    }
}
