﻿using BepInEx;
using BepInEx.Bootstrap;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ToolClasses;
using UnityEngine;
using VoidManager.Callbacks;
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
        public MultiplayerType HighestLevelOfMPMods { get; private set; } = MultiplayerType.Hidden;

        internal static InRoomCallbacks RoomCallbacksClass;
        private MPModDataBlock[] MyModList = null;
        private MPModDataBlock[] MyMPUnspecifiedModList = null;
        private MPModDataBlock[] MyMPAllModList = null;
        internal byte[] RoomProperties { get; private set; }
        internal Dictionary<Player, MPUserDataBlock> NetworkedPeersModLists = new Dictionary<Player, MPUserDataBlock>();
        internal string LastModCheckFailReason;

        /// <summary>
        /// The static instance of MPModCheckManager
        /// </summary>
        public static MPModCheckManager Instance { get; internal set; }

        internal void BuildRoomProperties()
        {
            RoomProperties = SerializeHashlessMPUserData();
        }

        internal void UpdateLobbyProperties()
        {
            if(!PhotonNetwork.IsMasterClient) //Only MC should update the lobby properties.
            {
                return;
            }

            Room CurrentRoom = PhotonNetwork.CurrentRoom;
            if (CurrentRoom == null) //This would probably break stuff
            {
                BepinPlugin.Log.LogWarning("Attempted to update lobby properties while room was null");
                return;
            }
            if(!CurrentRoom.CustomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey))//If the key doesn't already exist, there have been no limitations imposed on the room.
            {
                return;
            }

            //Sets VMan modded property.
            CurrentRoom.SetCustomProperties(new Hashtable { { InRoomCallbacks.RoomModsPropertyKey, RoomProperties } });
        }

        private void UpdateHighestLevelOfMPMods(MultiplayerType MT)
        {
            //Tiers: Hidden < Client < Host < Unspecified < All
            if(MT > HighestLevelOfMPMods)
            {
                HighestLevelOfMPMods = MT;
                BepinPlugin.Log.LogInfo("Incrementing HighestLevelOfMPMods to " + MT.ToString());
            }
        }

        /// <summary>
        /// Detects if a room has been modified.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>true if room is detected as modded</returns>
        public static bool RoomIsModded(RoomInfo room)
        {
            return room.CustomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey) || room.CustomProperties.ContainsKey(InRoomCallbacks.OfficalModdedPropertyKey);
        }

        private void UpdateMyModList()
        {
            BepinPlugin.Log.LogInfo("Building MyModList");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            KeyValuePair<string,PluginInfo>[] UnprocessedMods = Chainloader.PluginInfos.ToArray();
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
            MyMPAllModList = ProcessedMods.Where(Mod => Mod.MPType == MultiplayerType.All).ToArray();
            MyMPUnspecifiedModList = ProcessedMods.Where(Mod => Mod.MPType == MultiplayerType.Session).ToArray();
            stopwatch.Stop();
            BepinPlugin.Log.LogInfo("Finished Building MyModList, time elapsted: " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
            BepinPlugin.Log.LogInfo($"MyModList:\n{GetModListAsString(MyModList)}\n");
        }

        /// <summary>
        /// Serilizes user data into a byte array for network transfer. Does not contain a hash
        /// </summary>
        /// <returns>Serilized User data (Hashless)</returns>
        public byte[] SerializeHashlessMPUserData()
        {
            MemoryStream dataStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(dataStream))
            {
                //Datastream storage structure:
                writer.Write(MyPluginInfo.PLUGIN_VERSION);      //--Header--
                writer.Write(MyModList.Length);                 //string VMVersion
                for (int i = 0; i < MyModList.Length; i++)      //int    modcount
                {                                               //
                    MPModDataBlock dataBlock = MyModList[i];    //--ModData--
                    writer.Write(dataBlock.ModName);            //string mod name
                    writer.Write(dataBlock.ModGUID);            //string harmony ident
                    writer.Write(dataBlock.Version);            //string mod version
                    writer.Write((byte)dataBlock.MPType);       //byte   AllRequireMod
                    writer.Write(dataBlock.DownloadID);         //string ModID
                }
            }

            return dataStream.ToArray();
        }

        /// <summary>
        /// Deserializes bytes representing a serialized MPUserDataBlock which does not contain a hash.
        /// </summary>
        /// <param name="byteData"></param>
        /// <returns>MPUserDataBlock (Hashless)</returns>
        public static MPUserDataBlock DeserializeHashlessMPUserData(byte[] byteData)
        {
            MemoryStream memoryStream = new MemoryStream(byteData);
            memoryStream.Position = 0;
            try
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {

                    string VMVersion = reader.ReadString();
                    int ModCount = reader.ReadInt32();
                    MPModDataBlock[] ModList = new MPModDataBlock[ModCount];
                    for (int i = 0; i < ModCount; i++)
                    {
                        string modname = reader.ReadString();
                        string HarmonyIdent = reader.ReadString();
                        string ModVersion = reader.ReadString();
                        MultiplayerType MPType = (MultiplayerType)reader.ReadByte();
                        string ModID = reader.ReadString();
                        ModList[i] = new MPModDataBlock(HarmonyIdent, modname, ModVersion, MPType, ModID);
                    }

                    memoryStream.Dispose();
                    return new MPUserDataBlock(VMVersion, ModList);

                }
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogInfo($"Failed to read mod list from Hashless MPUserData, returning null.\n{ex.Message}");
                memoryStream.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Serilizes user data into a byte array for network transfer. Contains a hash.
        /// </summary>
        /// <returns>Serilized User data (Hashfull)</returns>
        public byte[] SerializeHashfullMPUserData()
        {
            MemoryStream dataStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(dataStream))
            {
                //Datastream storage structure:
                writer.Write(MyPluginInfo.PLUGIN_VERSION);      //--Header--
                writer.Write(MyModList.Length);                 //string VMVersion
                for (int i = 0; i < MyModList.Length; i++)      //int    modcount
                {                                               //
                    MPModDataBlock dataBlock = MyModList[i];    //--ModData--
                    writer.Write(dataBlock.ModName);            //string mod name
                    writer.Write(dataBlock.ModGUID);            //string harmony ident
                    writer.Write(dataBlock.Version);            //string mod version
                    writer.Write((byte)dataBlock.MPType);       //bool   AllRequireMod
                    writer.Write(dataBlock.DownloadID);         //string ModID
                    writer.Write(dataBlock.Hash);               //byte[] Hash
                }
            }
            return dataStream.ToArray();
        }

        /// <summary>
        /// Deserializes bytes representing a serialized MPUserDataBlock containing a hash.
        /// </summary>
        /// <param name="byteData"></param>
        /// <returns>MPUserDataBlock (Hashfull)</returns>
        public static MPUserDataBlock DeserializeHashfullMPUserData(byte[] byteData)
        {
            MemoryStream memoryStream = new MemoryStream(byteData);
            memoryStream.Position = 0;
            try
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {

                    string VoidManager = reader.ReadString();
                    int ModCount = reader.ReadInt32();
                    MPModDataBlock[] ModList = new MPModDataBlock[ModCount];
                    for (int i = 0; i < ModCount; i++)
                    {
                        string modname = reader.ReadString();
                        string HarmonyIdent = reader.ReadString();
                        string ModVersion = reader.ReadString();
                        MultiplayerType MPType = (MultiplayerType)reader.ReadByte();
                        string ModID = reader.ReadString();
                        byte[] Hash = reader.ReadBytes(32);
                        ModList[i] = new MPModDataBlock(HarmonyIdent, modname, ModVersion, MPType, ModID, Hash);
                    }
                    memoryStream.Dispose();
                    return new MPUserDataBlock(VoidManager, ModList);
                }
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogInfo($"Failed to read mod list from Hashfull MPUserData, returning null.\n{ex.Message}");
                memoryStream.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Converts a ModDataBlock array to a string list, usually for logging purposes. Starts with a new line.
        /// </summary>
        /// <param name="ModDatas"></param>
        /// <returns>Converts ModDataBLocks to a string list.</returns>
        public static string GetModListAsString(MPModDataBlock[] ModDatas)
        {
            string ModList = string.Empty;
            foreach (MPModDataBlock DataBlock in ModDatas)
            {
                ModList += $"\n - {DataBlock.ModName} {DataBlock.Version}";
            }
            return ModList;
        }

        /// <summary>
        /// Converts a ModDataBlock array to a string list for echo chat purposes.
        /// </summary>
        /// <param name="ModDatas"></param>
        /// <returns>Converts ModDataBLocks to a string list.</returns>
        public static string GetModListAsStringForChat(MPModDataBlock[] ModDatas)
        {
            string ModList = string.Empty;
            bool first = true;
            foreach (MPModDataBlock DataBlock in ModDatas)
            {
                if (first)
                {
                    first = false;
                    ModList += $" - {DataBlock.ModName} {DataBlock.Version}";
                }
                else
                {
                    ModList += $"\n - {DataBlock.ModName} {DataBlock.Version}";
                }
            }
            return ModList;
        }

        /// <summary>
        /// Provides the host MPUserDataBlock from room properties.
        /// </summary>
        /// <returns>Host's MPUserDataBlock</returns>
        public MPUserDataBlock GetHostModList()
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey))
            {
                try
                {
                    return DeserializeHashlessMPUserData((byte[])PhotonNetwork.CurrentRoom.CustomProperties[InRoomCallbacks.RoomModsPropertyKey]);
                }
                catch
                {
                    BepinPlugin.Log.LogError("Failed to Deserialize host mod list.");
                }
            }
            return null;
        }

        /// <summary>
        /// Gets full mod list of Networked Peer.
        /// </summary>
        /// <param name="Player"></param>
        /// <returns>MPUserDataBlock of NetworkedPeer. Returns null if no modlist found.</returns>
        public MPUserDataBlock GetNetworkedPeerMods(Player Player)
        {
            if (NetworkedPeersModLists.TryGetValue(Player, out MPUserDataBlock value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if given player has mod, checked by mod GUID
        /// </summary>
        /// <param name="Player"></param>
        /// <param name="ModGUID"></param>
        /// <returns>Returns true if player has mod</returns>
        public bool NetworkedPeerHasMod(Player Player, string ModGUID)
        {
            MPUserDataBlock userData = GetNetworkedPeerMods(Player);
            if (userData != null)
            {
                foreach (MPModDataBlock modData in userData.ModData)
                {
                    if (modData.ModGUID == ModGUID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Finds all Networked Peers with a given mod GUID
        /// </summary>
        /// <param name="ModGUID"></param>
        /// <returns>NetworkedPeers using given mod</returns>
        public List<Player> NetworkedPeersWithMod(string ModGUID)
        {
            List<Player> playerList = new List<Player>();
            foreach (KeyValuePair<Player, MPUserDataBlock> userEntry in NetworkedPeersModLists)
            {
                foreach (MPModDataBlock modData in userEntry.Value.ModData)
                {
                    if (modData.ModGUID == ModGUID)
                    {
                        playerList.Add(userEntry.Key);
                    }
                }
            }
            return playerList;
        }

        /// <summary>
        /// Adds a player's mod list to the local NetworkedPeersModLists
        /// </summary>
        /// <param name="Player"></param>
        /// <param name="modList"></param>
        public void AddNetworkedPeerMods(Player Player, MPUserDataBlock modList)
        {
            BepinPlugin.Log.LogMessage($"recieved modlist from user '{Player.NickName}' with the following info:\nVoidManager Version: {modList.VMVersion}\nModList:\n{MPModCheckManager.GetModListAsString(modList.ModData)}\n");
            if (NetworkedPeersModLists.ContainsKey(Player))
            {
                NetworkedPeersModLists[Player] = modList;
                return;
            }
            NetworkedPeersModLists.Add(Player, modList);

            Events.Instance.OnClientModlistRecieved(Player);
        }

        /// <summary>
        /// Clears player from NetworkedPeersModLists
        /// </summary>
        /// <param name="Player"></param>
        public void RemoveNetworkedPeerMods(Player Player)
        {
            NetworkedPeersModLists.Remove(Player);
        }

        /// <summary>
        /// Clears all lists from NetworkedPeersModLists
        /// </summary>
        internal void ClearAllNetworkedPeerMods()
        {
            NetworkedPeersModLists.Clear();
        }

        /// <summary>
        /// Checks if player has a mod list in NetworkedPeersModLists 
        /// </summary>
        /// <param name="Player"></param>
        /// <returns>existance of player key in dictionary</returns>
        public bool GetNetworkedPeerModlistExists(Player Player)
        {
            return NetworkedPeersModLists.ContainsKey(Player);
        }

        private static MPUserDataBlock GetHostModList(RoomInfo room)
        {
            if (room.CustomProperties.ContainsKey("modList"))
            {
                try
                {
                    return DeserializeHashlessMPUserData((byte[])room.CustomProperties["modList"]);
                }
                catch
                {
                    BepinPlugin.Log.LogError("Failed to Deserialize host mod list. Could be an older version of VoidManager");
                }
            }
            return new MPUserDataBlock();
        }

        internal void SendModlistToClient(Player Player)
        {
            if (Player.IsLocal)
            {
                return;
            }
            PhotonNetwork.RaiseEvent(InRoomCallbacks.PlayerMPUserDataEventCode, new object[] { false, SerializeHashlessMPUserData() }, new RaiseEventOptions { TargetActors = new int[1] { Player.ActorNumber } }, SendOptions.SendReliable);
        }

        internal void SendModlistToHost()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                return;
            }
            PhotonNetwork.RaiseEvent(InRoomCallbacks.PlayerMPUserDataEventCode, new object[] { true, SerializeHashfullMPUserData() }, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
        }

        internal void SendModListToOthers()
        {
            BepinPlugin.Log.LogMessage("sending others");
            PhotonNetwork.RaiseEvent(InRoomCallbacks.PlayerMPUserDataEventCode, new object[] { false, SerializeHashlessMPUserData() }, null, SendOptions.SendReliable);
        }

        internal void PlayerJoined(Player JoiningPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PunSingleton<PhotonService>.Instance.StartCoroutine(MPModCheckManager.PlayerJoinedChecks(JoiningPlayer)); //Plugin is not a valid monobehaviour.
            }
            else
            {
                MPModCheckManager.Instance.SendModlistToClient(JoiningPlayer);
            }
        }

        internal static IEnumerator PlayerJoinedChecks(Player JoiningPlayer)
        {
            for (int i = 0; i < 50; i++)
            {
                yield return new WaitForSeconds(.2f);
                if (Instance.GetNetworkedPeerModlistExists(JoiningPlayer))
                {
                    Instance.ModChecksHostOnClientJoin(JoiningPlayer);
                    yield break;
                }
            }
            if (Instance.HighestLevelOfMPMods == MultiplayerType.All)
            {
                //Kick player if mod no mod list recieved and there are local MPType.All Mods.
                BepinPlugin.Log.LogMessage($"Kicked player {JoiningPlayer.NickName} for not having mods.");
                Messaging.Echo($"Kicked player {JoiningPlayer.NickName} for not having mods.\n{GetModListAsStringForChat(Instance.MyMPAllModList)}", false);
                PhotonNetwork.CloseConnection(JoiningPlayer);
            }
            Events.Instance.CallHostOnClientVerified(JoiningPlayer);
        }

        internal bool ModChecksClientside(Hashtable RoomProperties, bool inRoom = true)
        {
            LastModCheckFailReason = string.Empty;
            BepinPlugin.Log.LogMessage($"Starting Clientside mod checks for room: {RoomProperties["R_Na"]}");

            if (!RoomProperties.ContainsKey(InRoomCallbacks.RoomModsPropertyKey))//Host doesn't have mods
            {
                if (HighestLevelOfMPMods >= MultiplayerType.Session)
                {
                    LastModCheckFailReason = $"Host has no mods, but client has unspecified or higher mods.{GetModListAsString(MyMPAllModList)}{GetModListAsString(MyMPUnspecifiedModList)}";

                    KickMessagePatches.KickTitle = "Disconnected: Incompatable mod list";
                    KickMessagePatches.KickMessage = LastModCheckFailReason;
                    BepinPlugin.Log.LogMessage("Mod check failed.\n" + LastModCheckFailReason);
                    return false; //Case: Host doesn't have mods, but Client has mod(s) which need the host to install.
                }
                else
                {
                    BepinPlugin.Log.LogMessage("Clientside mod check passed.");
                    return true; //Case: Host doesn't have mods, but client doesn't have restrictive mods.
                }
            }
            //Host must have mods beyond this point. 
            

            //Selects only mods which have MPType set to all for comparison.
            MPUserDataBlock HostModData = DeserializeHashlessMPUserData((byte[])RoomProperties[InRoomCallbacks.RoomModsPropertyKey]);
            MPModDataBlock[] HostMods = HostModData.ModData.Where(Mod => Mod.MPType == MultiplayerType.All).ToArray();

            BepinPlugin.Log.LogMessage($"Void Manager versions - Host: {HostModData.VMVersion} Client: {MyPluginInfo.PLUGIN_VERSION}");

            List<string> MismatchedVersions = new List<string>();
            List<string> ClientMissing = new List<string>();
            List<string> HostMissing = new List<string>();

            //Compare Client mods against Host mods
            int i;
            int x;
            MPModDataBlock CurrentHostMod;
            for (i = 0; i < MyMPAllModList.Length; i++)
            {
                MPModDataBlock CurrentLocalMod = MyMPAllModList[i];
                bool found = false;
                for (x = 0; x < HostMods.Length; x++)
                {
                    if (CurrentLocalMod.ModGUID == HostMods[x].ModGUID)
                    {
                        CurrentHostMod = HostMods[x];
                        found = true;
                        if (CurrentLocalMod.Version != CurrentHostMod.Version)
                        {
                            //Mod versions do not match
                            MismatchedVersions.Add($"Client:{CurrentLocalMod.ModName}-{CurrentLocalMod.Version}, Host:{CurrentHostMod.Version}");
                            BepinPlugin.Log.LogMessage($"Mismatched mod version - {MismatchedVersions.Last()}. { ((CurrentHostMod.DownloadID != string.Empty) ? $"Download Link: {CurrentHostMod.DownloadID}" : "") }");
                        }
                        break;
                    }
                }
                if (!found)
                {
                    //Client MPType.All Mod not found in host mods
                    HostMissing.Add(CurrentLocalMod.ModName);
                    BepinPlugin.Log.LogMessage($"Host is missing the required mod '{CurrentLocalMod.ModName}'");
                }
            }

            //Compare Host mods against client mods
            for (i = 0; i < HostMods.Length; i++)
            {
                CurrentHostMod = HostMods[i];
                bool found = false;
                for (x = 0; x < MyMPAllModList.Length; x++)
                {
                    if (CurrentHostMod.ModGUID == MyMPAllModList[x].ModGUID)
                    {
                        found = true;
                        break;
                    }
                }
                if(!found)
                {
                    //Host MPType.All Mod not found in Client mods
                    ClientMissing.Add(CurrentHostMod.ModName);
                    BepinPlugin.Log.LogMessage($"Client is missing the required mod '{CurrentHostMod.ModName}'");
                }
            }


            //Check if problems were found and add to error message.
            string errorMessage = string.Empty;

            if(MismatchedVersions.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods have mismatched versions:\n";
                foreach(string str in MismatchedVersions)
                {
                    errorMessage += str + "\n";
                }
            }
            if (ClientMissing.Count > 0) //Case: Client is missing mods required by the host.
            {
                errorMessage += "The following mods are required to join the session:\n";
                foreach (string str in ClientMissing)
                {
                    errorMessage += str + "\n";
                }
            }
            if (HostMissing.Count > 0) //Case: Client has mods which need to be installed by the host.
            {
                errorMessage += "The following mods must be uninstalled to join the session:";
                foreach (string str in HostMissing)
                {
                    errorMessage += "\n" + str;
                }
            }

            //return false and finalize error message.
            if(errorMessage != string.Empty)
            {
                if (inRoom) //Provide kickedMessage popup
                {
                    KickMessagePatches.KickTitle = "Disconnected: Incompatable mod list";
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
            if(!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            MPUserDataBlock JoiningPlayerMPData = GetNetworkedPeerMods(joiningPlayer);
            MPModDataBlock[] JoiningClientMPTypeAllMods = JoiningPlayerMPData.ModData.Where(Mod => Mod.MPType == MultiplayerType.All).ToArray();

            MPModDataBlock[] HostModListForProcessing;
            if (!BepinPlugin.Bindings.TrustMPTypeUnspecified.Value)
            {
                MPModDataBlock[] JoiningClientMPTypeUnspecifiedMods = JoiningPlayerMPData.ModData.Where(Mod => Mod.MPType == MultiplayerType.Session).ToArray();
                JoiningClientMPTypeAllMods = JoiningClientMPTypeAllMods.Concat(JoiningClientMPTypeUnspecifiedMods).ToArray();

                HostModListForProcessing = MyModList.Concat(MyMPUnspecifiedModList).ToArray();
            }
            else
            {
                HostModListForProcessing = MyModList;
            }

            List<string> MismatchedVersions = new List<string>();
            List<string> JoiningClientMissing = new List<string>();
            List<string> LocalClientMissing = new List<string>();


            int i;
            int x;
            MPModDataBlock CurrentJoiningClientMod;
            for (i = 0; i < MyMPAllModList.Length; i++)
            {
                MPModDataBlock CurrentLocalMod = MyMPAllModList[i];
                bool found = false;
                for (x = 0; x < JoiningClientMPTypeAllMods.Length; x++)
                {
                    if (CurrentLocalMod.ModGUID == JoiningClientMPTypeAllMods[x].ModGUID)
                    {
                        CurrentJoiningClientMod = JoiningClientMPTypeAllMods[x];
                        found = true;
                        if (CurrentLocalMod.Version != CurrentJoiningClientMod.Version)
                        {
                            //Mod versions do not match
                            MismatchedVersions.Add($"Client:{CurrentLocalMod.ModName}-{CurrentLocalMod.Version}, Host:{CurrentJoiningClientMod.Version}");
                            BepinPlugin.Log.LogMessage($"Mismatched mod version - {MismatchedVersions.Last()}. {((CurrentLocalMod.DownloadID != string.Empty) ? $"Download Link: {CurrentLocalMod.DownloadID}" : "")}");
                        }
                        else if (Encoding.ASCII.GetString(CurrentLocalMod.Hash) != Encoding.ASCII.GetString(CurrentJoiningClientMod.Hash))
                        {
                            //Mod Hash Mismatch. Log hash, but tell joining client it's a version mismatch.
                            MismatchedVersions.Add($"Client:{ CurrentLocalMod.ModName}-{ CurrentLocalMod.Version}, Host: { CurrentJoiningClientMod.Version}");
                            BepinPlugin.Log.LogMessage($"Mismatched mod hash - {MismatchedVersions.Last()} - LocalHash: {Encoding.ASCII.GetString(CurrentLocalMod.Hash)} IncomingHash: {Encoding.ASCII.GetString(CurrentJoiningClientMod.Hash)}. {((CurrentLocalMod.DownloadID != string.Empty) ? $"Download Link: {CurrentLocalMod.DownloadID}" : "")}");
                        }
                        break;
                    }
                }
                if (!found)
                {
                    //Client MPType.All mod not found in host mods
                    JoiningClientMissing.Add(CurrentLocalMod.ModName);
                    BepinPlugin.Log.LogMessage($"Client is missing the required mod '{CurrentLocalMod.ModName}'");
                }
            }


            //Check if joining client MPType.All mods exist for host.
            for (i = 0; i < JoiningClientMPTypeAllMods.Length; i++)
            {
                CurrentJoiningClientMod = JoiningClientMPTypeAllMods[i];
                bool found = false;
                for (x = 0; x < HostModListForProcessing.Length; x++)
                {
                    if (CurrentJoiningClientMod.ModGUID == HostModListForProcessing[x].ModGUID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    //Host MPType.All Mod not found in Joining Client mods
                    LocalClientMissing.Add(CurrentJoiningClientMod.ModName);
                    BepinPlugin.Log.LogMessage($"Client must uninstall the {CurrentJoiningClientMod.MPType} Mod '{CurrentJoiningClientMod.ModName}'");
                }
            }



            string errorMessage = string.Empty;

            if (MismatchedVersions.Count > 0) //Case: Client and Host have mismatched mod versions
            {
                errorMessage += "The following mods have mismatched versions:\n";
                foreach (string str in MismatchedVersions)
                {
                    errorMessage += str + "\n";
                }
            }
            if (JoiningClientMissing.Count > 0) //Case: Client is missing mods required by the host.
            {
                errorMessage += "The following mods are required to join the session:\n";
                foreach (string str in JoiningClientMissing)
                {
                    errorMessage += str + "\n";
                }
            }
            if (LocalClientMissing.Count > 0) //Case: Client has mods which need to be installed by the host.
            {
                errorMessage += "The following mods must be uninstalled to join the session:";
                foreach (string str in LocalClientMissing)
                {
                    errorMessage += "\n" + str;
                }
            }

            //Kick Player and finalize error message.
            if (errorMessage != string.Empty)
            {
                //Send message to joining client.
                Messaging.Echo($"Kicking player {joiningPlayer.NickName} from session for incompatable mods.\n{errorMessage}", false);
                Messaging.KickMessage("Kicked: Incompatable mod list", errorMessage, joiningPlayer);
                PhotonNetwork.CloseConnection(joiningPlayer);
                BepinPlugin.Log.LogMessage($"Kicked player {joiningPlayer.NickName} from session for incompatable mods.\n{errorMessage}");
            }
            else
            {
                BepinPlugin.Log.LogMessage("Hostside mod check passed for player " + joiningPlayer.NickName);
                Events.Instance.CallHostOnClientVerified(joiningPlayer);
            }
        }
    }
}
