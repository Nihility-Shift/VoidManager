using RuntimeAssets;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace VoidManager.Content
{
    /// <summary>
    /// Loads officially supported content from plugins dir
    /// </summary>
    public class SupportedContentLoader
    {
        static Type BepinType;
        static MethodInfo PluginsPathMI;

        internal static void Initialize()
        {
            BepinType = Type.GetType("BepInEx.Paths, BepInEx");
            if (BepinType == null)
            {
                Debug.Log("Type null");
                return;
            }
            PluginsPathMI = BepinType.GetMethod("get_PluginPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
            if (PluginsPathMI == null)
            {
                Debug.Log("PathMethod null");
                return;
            }
            foreach (string fileName in Directory.EnumerateFiles((string)PluginsPathMI.Invoke(null, null), "*.metem", SearchOption.AllDirectories))
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
