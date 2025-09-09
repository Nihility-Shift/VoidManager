using BepInEx;
using BepInEx.Bootstrap;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using VoidManager.Callbacks;
using VoidManager.Chat.Router;
using VoidManager.CustomGUI;
using VoidManager.ModMessages;
using VoidManager.MPModChecks;
using VoidManager.Utilities;

namespace VoidManager
{
    static class PluginHandler
    {
        public static Dictionary<string, PluginInfo> ActiveBepinPlugins { get => Chainloader.PluginInfos; }
        public static Dictionary<string, VoidPlugin> ActiveVoidPlugins { get; private set; }
        public static Dictionary<string, VoidPlugin> GeneratedVoidPlugins { get; private set; }

        /// <summary>
        /// Iterates through the current plugin files and searches for VoidPlugins.
        /// </summary>
        internal static void DiscoverPlugins()
        {
            VoidPlugin voidPlugin;

            Configs.LoadModListOverride();
            Dictionary<string, MultiplayerType> OverridenMods = Configs.ModOverrideDictionary;

            ActiveVoidPlugins = new();
            GeneratedVoidPlugins = new();
            foreach (PluginInfo CurrentBepinPlugin in Chainloader.PluginInfos.Values)
            {
                Assembly assembly = CurrentBepinPlugin.Instance.GetType().Assembly;
                string BPluginGUID = CurrentBepinPlugin.Metadata.GUID;
                string BPluginName = CurrentBepinPlugin.Metadata.Name;
                // Finds VoidPlugin class.
                if (BPluginGUID == MyPluginInfo.PLUGIN_GUID)
                {
                    CommandHandler.DiscoverCommands(assembly, BPluginName);
                    CommandHandler.DiscoverPublicCommands(assembly, BPluginName);
                    ModMessageHandler.DiscoverModMessages(assembly, CurrentBepinPlugin);
                    continue;
                }
                try
                {
                    IEnumerable<Type> voidPluginInstances = assembly.GetTypes().Where(t => typeof(VoidPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    if (voidPluginInstances.Any())
                    {
                        voidPlugin = (VoidPlugin)Activator.CreateInstance(voidPluginInstances.First());
                        voidPlugin.VersionInfo = FileVersionInfo.GetVersionInfo(CurrentBepinPlugin.Location);
                        voidPlugin.ModHash = GetFileHash(CurrentBepinPlugin.Location);
                        voidPlugin.BepinPlugin = CurrentBepinPlugin;
                        CommandHandler.DiscoverCommands(assembly, BPluginName);
                        CommandHandler.DiscoverPublicCommands(assembly, BPluginName);
                        ModMessageHandler.DiscoverModMessages(assembly, CurrentBepinPlugin);
                        GUIMain.Instance.DiscoverGUIMenus(assembly, voidPlugin);
                        ActiveVoidPlugins.Add(CurrentBepinPlugin.Metadata.GUID, voidPlugin);

                        if (voidPlugin.BepinPlugin.Metadata.GUID == "NoUnrepairableDamage")
                        {
                            BepinPlugin.Log.LogInfo("NoUnrepairableDamage mod detected, Registering Session Mod.");
                            ModdingUtils.SessionModdingType = ModdingType.mod_session;
                        }
                    }
                    else
                    {
                        MultiplayerType MPType;
                        if (OverridenMods.TryGetValue(BPluginGUID, out MPType) || OverridenMods.TryGetValue(BPluginName, out MPType)) //Overrides Unmanaged type mods with input from config.
                        {
                            voidPlugin = new DefaultVoidPlugin(MPType);
                            BepinPlugin.Log.LogInfo($"Discovered MPType override for {BPluginName}. Setting MPType to {MPType}");
                        }
                        else
                        {
                            voidPlugin = new DefaultVoidPlugin(MultiplayerType.Unmanaged);
                        }

                        voidPlugin.VersionInfo = FileVersionInfo.GetVersionInfo(CurrentBepinPlugin.Location);
                        voidPlugin.ModHash = GetFileHash(CurrentBepinPlugin.Location);
                        voidPlugin.BepinPlugin = CurrentBepinPlugin;
                        GUIMain.Instance.DiscoverNonVoidManagerMod(voidPlugin);
                        GeneratedVoidPlugins.Add(BPluginGUID, voidPlugin);
                    }
                }
                catch (Exception ex)
                {
                    BepinPlugin.Log.LogError($"Error loading mod '{BPluginName}'\n{ex}");
                }
            }
            GUIMain.Instance.settings = GUIMain.Instance.settings.OrderByDescending(v => v is VManSettings).ThenBy(v => v.Name()).ToList();
            GUIMain.Instance.mods.Sort((plugin1, plugin2) => plugin1.BepinPlugin.Metadata.Name.CompareTo(plugin2.BepinPlugin.Metadata.Name));
            BepinPlugin.Log.LogInfo($"Loaded {CommandHandler.chatCommandCount} local command(s) and {CommandHandler.publicCommandCount} public command(s)");
            BepinPlugin.Log.LogInfo($"Loaded {ModMessageHandler.modMessageHandlers.Count()} mod message(s)");
            BepinPlugin.Log.LogInfo($"Discovered {ActiveVoidPlugins.Count} {MyPluginInfo.PLUGIN_NAME} plugin(s) from {ActiveBepinPlugins.Count - 1} mod(s)");
        }

        /// <summary>
        /// Gets the Sha256 File has from the given file location.
        /// </summary>
        /// <param name="fileLocation"></param>
        /// <returns>SHA256 File Hash</returns>
        public static byte[] GetFileHash(string fileLocation)
        {
            using (SHA256 Hasher = SHA256.Create())
            {
                return Hasher.ComputeHash(File.ReadAllBytes(fileLocation));
            }
        }

        internal static bool CreatedRoomAsHost = false;

        internal static void InternalSessionChanged(CallType callType, bool isMod_Session, bool isMasterClient, Player Host = null)
        {
            Dictionary<VoidPlugin, SessionChangedInput> CalledAsNonModSession = new();
            bool IncrimentedToMod_Session = false;

            //Instantiate InputValue and assing values.
            SessionChangedInput inputData = new();
            inputData.CallType = callType;
            inputData.IsHost = isMasterClient;
            inputData.HostHasMod = isMasterClient;
            inputData.CreatedRoomAsHost = CreatedRoomAsHost;
            inputData.StartedSessionAsHost = CreatedRoomAsHost || GameSessionManager.Instance.StartedSessionAsHost;
            inputData.IsMod_Session = isMod_Session;

            if (Configs.DebugMode.Value)
            {
                BepinPlugin.Log.LogInfo($"OnSessionChanged callback\ncallType: {inputData.CallType}, isHost: {inputData.IsHost}, IsModSession: {inputData.IsMod_Session}, CreatedRoomAsHost: {inputData.CreatedRoomAsHost}, StartedSessionAshost: {inputData.StartedSessionAsHost}");
            }

            foreach (KeyValuePair<string, VoidPlugin> KVP in ActiveVoidPlugins)
            {
                if (!isMasterClient)
                {
                    inputData.HostHasMod = NetworkedPeerManager.Instance.NetworkedPeerHasMod(Host, KVP.Key);
                }

                //Call OnSessionChange. Store mods which didn't incriment to ModSession for the case of one incrimenting.
                if (!isMod_Session)
                {
                    if (!KVP.Value.OnSessionChange(inputData).SetMod_Session)
                    {
                        CalledAsNonModSession.Add(KVP.Value, inputData);
                    }
                    else
                    {
                        IncrimentedToMod_Session = true;
                        isMod_Session = true;
                    }
                }
                else
                {
                    try
                    {
                        KVP.Value.OnSessionChange(inputData);
                    }
                    catch(Exception e)
                    {
                        BepinPlugin.Log.LogError($"Caught error on session changed for {KVP.Key}\n{e}");
                    }
                }
            }

            //FixMe - Should call as Escalation if a mod escalated.
            //Call previous OnSessionChanged values if ModSession changed.
            if (IncrimentedToMod_Session && isMasterClient)
            {
                if(Configs.DebugMode.Value)
                {
                    BepinPlugin.Log.LogInfo("Mod requested Incriment to Mod_Session");
                }
                ModdingUtils.RegisterSessionMod();
                foreach (KeyValuePair<VoidPlugin, SessionChangedInput> KVP in CalledAsNonModSession)
                {
                    SessionChangedInput inputValue = KVP.Value;
                    inputValue.IsMod_Session = true;
                    KVP.Key.OnSessionChange(inputValue);
                }
            }
        }

        //internal use for recieving escalation event.
        internal static void InternalEscalateSession()
        {
            InternalSessionChanged(CallType.SessionEscalated, true, PhotonNetwork.IsMasterClient, PhotonNetwork.MasterClient);
        }

        internal static bool SessionWasEscalated = false;

        internal static bool CanEscalateSession()
        {
            return PhotonNetwork.IsMasterClient && !SessionWasEscalated;
        }

        internal static void EscalateSession()
        {
            if(!CanEscalateSession()) { return; }

            Messaging.Echo("Escalating to Mod_Session", false);
            ModdingUtils.RegisterSessionMod();
            PhotonNetwork.RaiseEvent(InRoomCallbacks.SessionEscalationEventCode, default, default, SendOptions.SendReliable);
            InternalEscalateSession();
            SessionWasEscalated = true;
        }
    }
}
