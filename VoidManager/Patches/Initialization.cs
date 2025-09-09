using CG.Profile;
using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using UnityEngine;
using VoidManager.CustomGUI;
using VoidManager.LobbyPlayerList;
using VoidManager.MPModChecks;
using VoidManager.Progression;

namespace VoidManager.Patches
{
    [HarmonyPatch(typeof(PlayerProfileLoader), "Awake")]
    internal class Initialization
    {
        static MethodInfo PhotonSetupLogging = AccessTools.Method(typeof(PhotonNetwork), "SetupLogging");

        [HarmonyPostfix]
        public static void PostAwakeInit()
        {
            BepinPlugin.Log.LogInfo($"- - - Void Manager Initialization - - -");

            new GameObject("ModManager", typeof(GUIMain)) { hideFlags = HideFlags.HideAndDontSave };
            new GameObject("ModList", typeof(ModListGUI)) { hideFlags = HideFlags.HideAndDontSave };
            new GameObject("ProgressDisabled", typeof(ProgressionDisabledGUI)) { hideFlags = HideFlags.HideAndDontSave };

            PluginHandler.DiscoverPlugins();

            NetworkedPeerManager.Instance = new NetworkedPeerManager();
            MPModCheckManager.Instance = new MPModCheckManager();
            LobbyPlayerListManager.Instance = new LobbyPlayerListManager();

            //Load Photon Logging settings.
            ServerSettings serverSettings = PhotonNetwork.PhotonServerSettings;
            if (serverSettings != null)
            {
                serverSettings.PunLogging = Configs.PunLoggingSettingLevel.Value;
                serverSettings.AppSettings.NetworkLogging = Configs.PunDebugLogLevel.Value;
                PhotonSetupLogging.Invoke(null, null);
            }
            BepinPlugin.Log.LogInfo($"- - - - - - - - - - - - - - - - - - - -");
        }
    }
}
