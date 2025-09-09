using BepInEx;
using BepInEx.Bootstrap;
using CG.GameLoopStateMachine;
using CG.GameLoopStateMachine.GameStates;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ToolClasses;
using UnityEngine;
using VoidManager.Callbacks;
using VoidManager.LobbyPlayerList;
using VoidManager.MPModChecks.Patches;
using VoidManager.Utilities;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace VoidManager.MPModChecks
{
    /// <summary>
    /// Manages multiplayer mod checking and user mod caching.
    /// </summary>
    public class MPModCheckManager
    {
        internal MPModCheckManager()
        {
            UpdateMyModList();
            BuildRoomProperties();
        }

        /// <summary>
        /// Highest level of mods MPType. Does as says
        /// </summary>
        public MultiplayerType HighestLevelOfMPMods { get; private set; } = MultiplayerType.Client;

        internal static InRoomCallbacks RoomCallbacksClass;
        private MPModDataBlock[] MyModList = null;
        internal byte[] MyModListData { get; private set; }
        //internal Dictionary<Player, MPUserDataBlock> NetworkedPeersModLists = new Dictionary<Player, MPUserDataBlock>();
        internal string LastModCheckFailReason;

        /// <summary>
        /// The static instance of MPModCheckManager
        /// </summary>
        public static MPModCheckManager Instance { get; internal set; }

        internal void BuildRoomProperties()
        {
            MyModListData = NetworkedPeerManager.SerializeHashlessMPUserData(MyModList);
        }

        internal void UpdateLobbyProperties()
        {
            if (!PhotonNetwork.IsMasterClient) // Only MC should update the lobby properties.
            {
                return;
            }

            Room CurrentRoom = PhotonNetwork.CurrentRoom;
            if (CurrentRoom == null) // This would probably break stuff
            {
                BepinPlugin.Log.LogWarning("Attempted to update lobby properties while room was null");
                return;
            }

            // Set lobby Mods property
            string[] publicProps = CurrentRoom.PropertiesListedInLobby;
            if (!CurrentRoom.CustomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey))
            {
                publicProps = publicProps.Append(InRoomCallbacks.RoomModsPropertyKey).ToArray();
                CurrentRoom.SetPropertiesListedInLobby(publicProps);
            }
            CurrentRoom.SetCustomProperties(new Hashtable { { InRoomCallbacks.RoomModsPropertyKey, MyModListData } });

            // Set lobby player list property
            if (!publicProps.Contains(InRoomCallbacks.RoomPlayerListPropertyKey))
            {
                CurrentRoom.SetPropertiesListedInLobby(publicProps.Append(InRoomCallbacks.RoomPlayerListPropertyKey).ToArray());
            }
            LobbyPlayerListManager.Instance.UpdateLobbyPlayers();
        }

        internal void UpdateHighestLevelOfMPMods(MultiplayerType MT)
        {
            switch (MT)
            {
                case MultiplayerType.All:
                    if (MT > HighestLevelOfMPMods)
                    {
                        //Modding Guidelines Compliance
                        ModdingUtils.SessionModdingType = ModdingType.mod_session;
                        HighestLevelOfMPMods = MT;
                        BepinPlugin.Log.LogInfo("Incrementing HighestLevelOfMPMods to " + MT.ToString());
                    }
                    break;
                case MultiplayerType.Session:
                    if (MT > HighestLevelOfMPMods)
                    {
                        //Modding Guidelines Compliance
                        ModdingUtils.SessionModdingType = ModdingType.mod_session;
                        HighestLevelOfMPMods = MT;
                        BepinPlugin.Log.LogInfo("Incrementing HighestLevelOfMPMods to " + MT.ToString());
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Detects if a room is been modded as Mod_Session.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>true if room is detected as Mod_Session</returns>
        public static bool RoomIsModded(RoomInfo room)
        {
            return room.CustomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey) || (room.CustomProperties.TryGetValue(InRoomCallbacks.OfficalModdedPropertyKey, out object MT) && (ModdingType)MT == ModdingType.mod_session);
        }

        /// <summary>
        /// Detects if a room has been modded as Mod_Session.
        /// </summary>
        /// <param name="roomProperties"></param>
        /// <returns>true if room is detected as Mod_Session</returns>
        public static bool RoomIsModded(Hashtable roomProperties)
        {
            return roomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey) || (roomProperties.TryGetValue(InRoomCallbacks.OfficalModdedPropertyKey, out object MT) && (ModdingType)MT == ModdingType.mod_session);
        }


        private void UpdateMyModList()
        {
            BepinPlugin.Log.LogInfo("Building MyModList");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            KeyValuePair<string, PluginInfo>[] UnprocessedMods = Chainloader.PluginInfos.ToArray();
            MPModDataBlock[] ProcessedMods = new MPModDataBlock[UnprocessedMods.Length];
            for (int i = 0; i < UnprocessedMods.Length; i++)
            {
                PluginInfo currentMod = UnprocessedMods[i].Value;
                string GUID = currentMod.Metadata.GUID;
                if (GUID == MyPluginInfo.PLUGIN_GUID)//Do not add VoidManager. Without a VoidPlugin it defaults to lacking
                {
                    continue;
                }
                else if (PluginHandler.ActiveVoidPlugins.TryGetValue(GUID, out VoidPlugin voidPlugin) || PluginHandler.GeneratedVoidPlugins.TryGetValue(GUID, out voidPlugin)) //Check for metadata for MPType. If metadata doesn't exist, default to MPType.all
                {
                    if (voidPlugin.MPType != MultiplayerType.Hidden) //Do nothing if marked as hidden.
                    {
                        ProcessedMods[i] = new MPModDataBlock(GUID, currentMod.Metadata.Name, currentMod.Metadata.Version.ToString(), voidPlugin.MPType, string.Empty, voidPlugin.ModHash);
                        UpdateHighestLevelOfMPMods(voidPlugin.MPType);
                    }
                }
            }
            ProcessedMods = ProcessedMods.Where(mod => mod != null).ToArray();
            MyModList = ProcessedMods;
            stopwatch.Stop();
            BepinPlugin.Log.LogInfo("Finished Building MyModList, time elapsted: " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
            BepinPlugin.Log.LogInfo($"MyModList:\n{NetworkedPeerManager.GetModListAsString(MyModList)}\n");
        }

        internal void JoinedRoom()
        {
            // No point continuing if master.
            if (PhotonNetwork.IsMasterClient) return;

            if (!MPModCheckManager.Instance.ModChecksClientside(PhotonNetwork.CurrentRoom.CustomProperties))
            {
                BepinPlugin.Log.LogInfo("Disconnecting from Room");
                GameStateMachine.Instance.ChangeState<GSPhotonDisconnected>();
                return;
            }

            //Send hashfull mod list to host.
            NetworkedPeerManager.SendModlistToHost(MyModList);

            //Collect player mod lists, send mod list if player doesn't have mod list in properties.
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // debug print mod list of all players on join.
                if (Configs.DebugMode.Value)
                {
                    NetworkedPeerManager.CheckPlayerModsFromProperties(player);
                }

                // Keep SetNetworkedPeerMods when removing later. Don't send mod list to master, as a hashfull list was already sent.
                if (!NetworkedPeerManager.Instance.SetNetworkedPeerMods(player)) 
                {
                    BepinPlugin.Log.LogInfo($"No mod data detected for {player.NickName}.");
                }
            }
        }

        internal void PlayerJoined(Player JoiningPlayer)
        {
            if (PhotonNetwork.IsMasterClient) //MasterClient must recieve mod data.
            {
                PunSingleton<PhotonService>.Instance.StartCoroutine(MPModCheckManager.PlayerJoinedChecks(JoiningPlayer)); //Plugin is not a valid monobehaviour.
            }
            else 
            {
                if (!NetworkedPeerManager.Instance.SetNetworkedPeerMods(JoiningPlayer))
                {
                    BepinPlugin.Log.LogInfo($"No mod data detected for {JoiningPlayer.NickName}.");
                }
            }
        }

        internal static IEnumerator PlayerJoinedChecks(Player JoiningPlayer)
        {
            //Kicks player imediately if no mods and MPType all mods exist.
            if (Instance.HighestLevelOfMPMods == MultiplayerType.All && !JoiningPlayer.CustomProperties.ContainsKey(InRoomCallbacks.PlayerModsPropertyKey))
            {
                BepinPlugin.Log.LogMessage($"Kicked player {JoiningPlayer.NickName} for not having mods.");
                Messaging.Echo($"Kicked player {JoiningPlayer.NickName} for not having mods.\n{NetworkedPeerManager.GetModListAsStringForChat(Instance.MyModList.Where(MDB => MDB.MPType == MultiplayerType.All).ToArray())}", false);
                PhotonNetwork.CloseConnection(JoiningPlayer);
                yield break;
            }

            for (int i = 0; i < 50; i++)
            {
                yield return new WaitForSeconds(.2f);
                if (NetworkedPeerManager.Instance.GetNetworkedPeerModlistExists(JoiningPlayer))
                {
                    Instance.ModChecksHostOnClientJoin(JoiningPlayer);
                    yield break;
                }
            }

            //Host Must recieve hashfull mod list from client, but did not.
            if (Instance.HighestLevelOfMPMods == MultiplayerType.All)
            {
                //Kick player if mod no mod list recieved and there are local MPType.All Mods.
                BepinPlugin.Log.LogMessage($"Kicked player {JoiningPlayer.NickName} for not having mods.");
                Messaging.Echo($"Kicked player {JoiningPlayer.NickName} for not having mods.\n{NetworkedPeerManager.GetModListAsStringForChat(Instance.MyModList.Where(MDB => MDB.MPType == MultiplayerType.All).ToArray())}", false);
                PhotonNetwork.CloseConnection(JoiningPlayer);
            }
            else
            {
                Events.Instance.OnHostVerifiedClient(JoiningPlayer);
            }
        }

        /// <summary>
        /// Calls vanilla isSessioModded check.
        /// </summary>
        /// <returns>ModdingUtils.IsCurrentSessionModded</returns>
        public static bool IsMod_Session()
        {
            return ModdingUtils.IsCurrentSessionModded();
        }

        internal bool ModChecksClientside(Hashtable RoomProperties, bool inRoom = true)
        {
            if (inRoom && PhotonNetwork.IsMasterClient) { return true; } // Should not check own room.

            LastModCheckFailReason = string.Empty;
            BepinPlugin.Log.LogMessage($"Starting Clientside mod checks for room: {RoomProperties[InRoomCallbacks.OfficalRoomNamePropertyKey]}");

            bool roomToJoinIsModded = RoomIsModded(RoomProperties);


            // Stop early if host doesn't have mods but local client requires such.
            if (!roomToJoinIsModded)
            {
                if (HighestLevelOfMPMods >= MultiplayerType.Session)
                {
                    // Case: Host doesn't have mods, but Client has mod(s) which need the room to be marked as 'Mod_Session'.
                    LastModCheckFailReason = $"Host has no mods, but client has Mod_Session or higher mods.{NetworkedPeerManager.GetModListAsString(Instance.MyModList.Where(MDB => MDB.MPType >= MultiplayerType.Session).ToArray())}";

                    KickMessagePatches.KickTitle = "Disconnected: Incompatable mod list";
                    KickMessagePatches.KickMessage = LastModCheckFailReason;
                    BepinPlugin.Log.LogMessage("Mod check failed.\n" + LastModCheckFailReason);
                    return false;
                }
                else
                {
                    // Case: Host doesn't have mods, but client doesn't have restrictive mods.
                    BepinPlugin.Log.LogMessage("Clientside mod check passed.");
                    return true;
                }
            }
            //Host must have mods beyond this point. 


            //Conditions Dictionary Init
            Dictionary<string, CheckConditions> conditions = new();


            //Selects only mods which have MPType set to all for comparison.
            MPUserDataBlock HostModData = NetworkedPeerManager.DeserializeHashlessMPUserData((byte[])RoomProperties[InRoomCallbacks.RoomModsPropertyKey]);
            MPModDataBlock[] HostMods = HostModData.ModData;

            BepinPlugin.Log.LogMessage($"VoidManager versions - Host: {HostModData.VMVersion} Client: {MyPluginInfo.PLUGIN_VERSION}");


            //Loop through Local/Client mods
            foreach (MPModDataBlock mod in MyModList)
            {
                CheckConditions condition = new();
                condition.IsMod_Session = roomToJoinIsModded;
                condition.HostCheck = false;
                condition.PlayersWithMod = PlayersWithMod.Client;
                condition.Mod = mod;
                condition.ClientModVersion = mod.Version;

                conditions.Add(mod.ModGUID, condition);
            }

            //Loop through Host mods
            foreach (MPModDataBlock mod in HostMods)
            {
                CheckConditions condition;
                if (conditions.TryGetValue(mod.ModGUID, out condition))
                {
                    condition.HostModVersion = mod.Version;
                    condition.PlayersWithMod = PlayersWithMod.Both;
                    conditions[mod.ModGUID] = condition;
                }
                else
                {
                    condition = new();
                    condition.IsMod_Session = roomToJoinIsModded;
                    condition.PlayersWithMod = PlayersWithMod.Host;
                    condition.HostCheck = false;
                    condition.Mod = mod;
                    condition.HostModVersion = mod.Version;
                    conditions.Add(mod.ModGUID, condition);
                }
            }



            //Fail reasons for messaging later.
            List<FailInfo> Session = new();
            List<FailInfo> MismatchedVersions = new();
            List<FailInfo> AllClientLacking = new();
            List<FailInfo> AllHostLacking = new();
            List<FailInfo> Custom = new();


            //Check mods and gather failDetails.
            foreach (CheckConditions condition in conditions.Values)
            {
                FailInfo failDetail = CheckMod(condition);
                switch (failDetail.CheckFailReason)
                {
                    case CheckFailReason.Session:
                        Session.Add(failDetail);
                        break;
                    case CheckFailReason.MismatchedVersions:
                        MismatchedVersions.Add(failDetail);
                        break;
                    case CheckFailReason.AllClientLacking:
                        AllClientLacking.Add(failDetail);
                        break;
                    case CheckFailReason.AllHostLacking:
                        AllHostLacking.Add(failDetail);
                        break;
                    case CheckFailReason.Custom:
                        Custom.Add(failDetail);
                        break;
                }
            }


            //Check if problems were found and add to error message.
            string errorMessage = string.Empty;


            if (Session.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods require rooms marked as Mod_Session:\n";
                foreach (FailInfo FI in Session)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (MismatchedVersions.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods have mismatched versions:\n";
                foreach (FailInfo FI in MismatchedVersions)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (AllClientLacking.Count > 0) //Case: Client is missing mods required by the host.
            {
                errorMessage += "The following mods are required to join the session:\n";
                foreach (FailInfo FI in AllClientLacking)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (AllHostLacking.Count > 0) //Case: Client has mods which need to be installed by the host.
            {
                errorMessage += "The following mods must be uninstalled to join the session:";
                foreach (FailInfo FI in AllHostLacking)
                {
                    errorMessage += "\n" + FI.FailingMod.ModName;
                }
            }

            //return false and finalize error message.
            if (errorMessage != string.Empty)
            {
                if (inRoom) //Provide kickedMessage popup
                {
                    KickMessagePatches.KickTitle = "Disconnected: Incompatable mods";
                    KickMessagePatches.KickMessage = errorMessage;
                }
                else //Provide Terminal fail to join popup
                {
                    LastModCheckFailReason = errorMessage;
                }


                BepinPlugin.Log.LogMessage("Couldn't join session.\n" + errorMessage);
                return false;
            }

            //Mod check passed.
            BepinPlugin.Log.LogMessage("Clientside mod check passed.");
            return true;
        }

        internal void ModChecksHostOnClientJoin(Player joiningPlayer)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }


            //Collect Client/Host mods
            MPUserDataBlock JoiningPlayerMPData = NetworkedPeerManager.Instance.GetNetworkedPeerMods(joiningPlayer);
            MPModDataBlock[] JoiningClientMods = JoiningPlayerMPData.ModData;

            if (Configs.DebugMode.Value) BepinPlugin.Log.LogInfo($"Host checking user mod list\n{NetworkedPeerManager.GetModListAsString(JoiningClientMods)}");
            if (Configs.DebugMode.Value) BepinPlugin.Log.LogInfo($"Host mod list:\n{NetworkedPeerManager.GetModListAsString(MyModList)}");

            //Conditions Dictionary Init
            Dictionary<string, CheckConditions> conditions = new();

            bool isModSession = IsMod_Session();


            //Loop through Local/Host mods
            foreach (MPModDataBlock mod in MyModList)
            {
                CheckConditions condition = new CheckConditions();
                condition.IsMod_Session = isModSession;
                condition.HostCheck = true;
                condition.PlayersWithMod = PlayersWithMod.Host;
                condition.Mod = mod;
                condition.HostModVersion = mod.Version;

                conditions.Add(mod.ModGUID, condition);
            }

            //Loop through Joining Client mods
            foreach (MPModDataBlock mod in JoiningClientMods)
            {
                CheckConditions condition;
                if (conditions.TryGetValue(mod.ModGUID, out condition))
                {
                    if (Configs.DebugMode.Value) BepinPlugin.Log.LogInfo($"Mod '{mod.ModName}' Matched with Client Mod");
                    condition.ClientModVersion = mod.Version;
                    condition.PlayersWithMod = PlayersWithMod.Both;
                    condition.HashesMatch = mod.Hash.SequenceEqual(condition.Mod.Hash);
                    conditions[mod.ModGUID] = condition;
                }
                else
                {
                    condition = new CheckConditions();
                    condition.IsMod_Session = isModSession;
                    condition.PlayersWithMod = PlayersWithMod.Client;
                    condition.HostCheck = true;
                    condition.Mod = mod;
                    condition.ClientModVersion = mod.Version;
                    conditions.Add(mod.ModGUID, condition);
                }
            }


            //Fail reasons for messaging later.
            List<FailInfo> Session = new();
            List<FailInfo> MismatchedVersions = new();
            List<FailInfo> AllClientLacking = new();
            List<FailInfo> AllHostLacking = new();
            List<FailInfo> Custom = new();


            //Check mods and gather failDetails.
            foreach (CheckConditions condition in conditions.Values)
            {
                FailInfo failDetail = CheckMod(condition);
                switch (failDetail.CheckFailReason)
                {
                    case CheckFailReason.Session:
                        Session.Add(failDetail);
                        break;
                    case CheckFailReason.MismatchedVersions:
                        MismatchedVersions.Add(failDetail);
                        break;
                    case CheckFailReason.AllClientLacking:
                        AllClientLacking.Add(failDetail);
                        break;
                    case CheckFailReason.AllHostLacking:
                        AllHostLacking.Add(failDetail);
                        break;
                    case CheckFailReason.Custom:
                        Custom.Add(failDetail);
                        break;
                }
            }


            string errorMessage = string.Empty;

            if (Session.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods require rooms marked as Mod_Session\n";
                foreach (FailInfo FI in Session)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (MismatchedVersions.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods have mismatched versions:\n";
                foreach (FailInfo FI in MismatchedVersions)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (AllClientLacking.Count > 0) //Case: Client is missing mods required by the host.
            {
                errorMessage += "The following mods are required to join the session:\n";
                foreach (FailInfo FI in AllClientLacking)
                {
                    errorMessage += FI.FailingMod.ModName + "\n";
                }
            }
            if (AllHostLacking.Count > 0) //Case: Client has mods which need to be installed by the host.
            {
                errorMessage += "The following mods must be uninstalled to join the session:";
                foreach (FailInfo FI in AllHostLacking)
                {
                    errorMessage += "\n" + FI.FailingMod.ModName;
                }
            }

            //Kick Player and finalize error message.
            if (errorMessage != string.Empty)
            {
                //Send message to joining client.
                Messaging.Echo($"Kicking player {joiningPlayer.NickName} from session for incompatable mods.\n{errorMessage}", false);
                Messaging.KickMessage("Kicked: Incompatable mods", errorMessage, joiningPlayer);
                PhotonNetwork.CloseConnection(joiningPlayer);
                BepinPlugin.Log.LogMessage($"Kicked player {joiningPlayer.NickName} from session for incompatable mods.\n{errorMessage}");
            }
            else
            {
                BepinPlugin.Log.LogMessage("Hostside mod check passed for player " + joiningPlayer.NickName);
                Events.Instance.OnHostVerifiedClient(joiningPlayer);
            }
        }

        internal FailInfo CheckMod(CheckConditions Conditions)
        {
            FailInfo failDetails = new(); //Default NoFail
            failDetails.FailingMod = Conditions.Mod;

            switch (Conditions.Mod.MPType)
            {
                case MultiplayerType.All:
                    if (Conditions.PlayersWithMod == PlayersWithMod.Both)
                    {
                        if (Conditions.HostModVersion == Conditions.ClientModVersion)
                        {
                            if (Conditions.HashesMatch || !Conditions.HostCheck) // Hash check only matters if host is checking.
                            {
                                failDetails.CheckFailReason = CheckFailReason.NoFail;
                                break;
                            }
                            BepinPlugin.Log.LogMessage($"Mismatched mod hash - {Conditions.Mod.ModName}");
                        }
                        else BepinPlugin.Log.LogMessage($"Mismatched mod version - Client:{Conditions.Mod.ModName}-{Conditions.ClientModVersion}, Host:{Conditions.HostModVersion}");
                        failDetails.CheckFailReason = CheckFailReason.MismatchedVersions; //Report Mismatched version for both hash and version fails.
                        break;
                    }

                    // All-required mod and a client or the host don't have the mod.
                    if (Conditions.PlayersWithMod == PlayersWithMod.Client)
                    {
                        failDetails.CheckFailReason = CheckFailReason.AllHostLacking;
                        BepinPlugin.Log.LogMessage($"Host is missing the required mod '{Conditions.Mod.ModName}'");
                    }
                    else
                    {
                        failDetails.CheckFailReason = CheckFailReason.AllClientLacking;
                        BepinPlugin.Log.LogMessage($"Client is missing the required mod '{Conditions.Mod.ModName}'");
                    }
                    break;


                // If a user has one of these mods, the session must be Mod_Session. If it is not, the host or joining client should fail connection.
                case MultiplayerType.Session:
                    if (Conditions.IsMod_Session)
                    {
                        failDetails.CheckFailReason = CheckFailReason.NoFail;
                    }
                    else
                    {
                        BepinPlugin.Log.LogMessage($"User attempting to join session with MPType.Session mod.");
                        failDetails.CheckFailReason = CheckFailReason.Session;
                    }
                    break;


                // These mods will enable/disable themselves as needed.
                case MultiplayerType.Client:
                case MultiplayerType.Host:
                    failDetails.CheckFailReason = CheckFailReason.NoFail;
                    break;
            }

            //All other MPTypes will default to allow.
            return failDetails;
        }
    }
}
