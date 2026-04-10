using RuntimeAssets;
using System;
using System.IO;
using UnityEngine;

namespace VoidManager.Content
{
    /// <summary>
    /// Loads officially supported content from plugins dir
    /// </summary>
    public class SupportedContentLoader
    {
        internal static void Initialize()
        {
            foreach (string fileName in Directory.EnumerateFiles(BepInEx.Paths.PluginPath, "*.metem", SearchOption.AllDirectories))
            {
                try
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(fileName);
                    RuntimeAssetsAPI.LoadAssetBundle(bundle);
                }
                catch (Exception ex)
                {
                    BepinPlugin.Log.LogError($"SupportedContentLoader failed to load asset bundle from {fileName}\n" + ex);
                }
            }
        }
    }
}
