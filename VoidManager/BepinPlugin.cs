using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using VoidManager.Chat.Additions;
using VoidManager.Chat.Router;

namespace VoidManager
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.USERS_PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Void Crew.exe")]
    public class BepinPlugin : BaseUnityPlugin
    {
        internal static BepinPlugin instance;
        internal static readonly Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource Log;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "N/A")]
        private void Awake()
        {
            instance = this;
            Log = Logger;
            Configs.Load(this);

            //Modding Guidelines Compliance - Setup Mod_Local. Mod_Session set later.
            ModdingUtils.SessionModdingType = ModdingType.mod_local;

            //Wrapped with try catch. If PatchAll fails, further code does not run.
            try { Harmony.PatchAll(); }
            catch (Exception e) { Log.LogError(e); }


            //Content.Craftables.Instance = new();
            Content.Unlocks.Instance = new();
            Events.Instance = new();


            //Fix chainloader getting deleted by GC?
            Chainloader.ManagerObject.hideFlags = HideFlags.HideAndDontSave;

            Events.Instance.ChatWindowOpened += ChatHistory.OnChatOpened;
            Events.Instance.ChatWindowOpened += CursorUnlock.OnChatOpened;
            Events.Instance.ChatWindowOpened += AutoComplete.OnChatOpened;
            Events.Instance.ChatWindowClosed += ChatHistory.OnChatClosed;
            Events.Instance.ChatWindowClosed += CursorUnlock.OnChatClosed;
            Events.Instance.ChatWindowClosed += AutoComplete.OnChatClosed;
            Events.Instance.LateUpdate += ChatHistory.Tick;
            Events.Instance.LateUpdate += AutoComplete.Tick;
            Events.Instance.JoinedRoom += PublicCommandHandler.RefreshPublicCommandCache;
            Events.Instance.ClientModlistRecieved += PublicCommandHandler.RefreshPublicCommandCache;
            Events.Instance.MasterClientSwitched += PublicCommandHandler.RefreshPublicCommandCache;
            Events.Instance.PlayerEnteredRoom += AutoComplete.RefreshPlayerList;
            Events.Instance.PlayerLeftRoom += AutoComplete.RefreshPlayerList;
            Events.Instance.JoinedRoom += AutoComplete.RefreshPlayerList;

            Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} Initialized.");
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member