using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using VoidManager.Callbacks;

namespace VoidManager.LobbyPlayerList
{
    /// <summary>
    /// Manages Player List for Lobby
    /// </summary>
    public class LobbyPlayerListManager
    {
        /// <summary>
        /// Static instance of the Player List Manager.
        /// </summary>
        public static LobbyPlayerListManager Instance { get; internal set; }

        const byte MyDataVersion = byte.MinValue;

        internal void UpdateLobbyPlayers()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                SetPlayerListData(PhotonNetwork.CurrentRoom);
            }
        }

        internal static byte[] SerializePlayerList(List<LobbyPlayer> lobbyPlayers)
        {
            MemoryStream dataStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(dataStream))
            {
                //Datastream storage structure:                //--Header--
                writer.Write(MyDataVersion);                   //byte   Data Version
                writer.Write((byte)lobbyPlayers.Count);        //byte    Player count
                for (int i = 0; i < lobbyPlayers.Count; i++)   //
                {                                              //
                    LobbyPlayer player = lobbyPlayers[i];      //--PlayerData--
                    writer.Write(player.Name);                 //string Player name
                    writer.Write(player.UserID);               //string Player User ID
                    writer.Write(player.Rank);                 //int Player Rank
                    writer.Write(player.FavorRank);            //int Player Favor Rank
                }
            }

            return dataStream.ToArray();
        }

        internal static List<LobbyPlayer> DeserializePlayerList(byte[] byteData)
        {
            MemoryStream memoryStream = new MemoryStream(byteData);
            memoryStream.Position = 0;
            List<LobbyPlayer> players = new();
            byte dataVersion = 255;
            try
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    //Datastream storage structure:                   //--Header--
                    dataVersion = reader.ReadByte();                  //byte   Data Version
                    if (dataVersion > MyDataVersion)                  //
                    {                                                 //
                        BepinPlugin.Log.LogError("Attempted to read data, but data version is greater than local.");
                        return players;                               //
                    }                                                 //
                                                                      //
                    byte PlayerCount = reader.ReadByte();             //byte    Player count
                    for (int i = 0; i < PlayerCount; i++)             //
                    {                                                 //--PlayerData--
                        string name = reader.ReadString();            //string Player name
                        string UserId = reader.ReadString();          //string Player User ID
                        int rank = reader.ReadInt32();                //int Player Rank
                        int favorRank = reader.ReadInt32();           //int Player Favor Rank
                        players.Add(new LobbyPlayer(name, UserId, rank, favorRank));
                    }
                }
                return players;
            }
            catch (Exception ex)
            {
                BepinPlugin.Log.LogInfo($"Failed to read player list from lobby data, returning null.\n{ex.Message}");
                memoryStream.Dispose();
                return players;
            }
        }

        internal static List<LobbyPlayer> GetPlayerListFromRoom(Room room)
        {
            if (room.CustomProperties.TryGetValue(InRoomCallbacks.RoomPlayerListPropertyKey, out object LobbyPlayerData))
            {
                return DeserializePlayerList((byte[])LobbyPlayerData);
            }
            return null;
        }

        internal void SetPlayerListData(Room room)
        {
            //Don't bother setting player list if not master. Non-Masters could set the player list, but if multiple VoidManager clients exist, all will send. Potentially reworkable in the future.
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            //Create lobbyPlayer list from Room PlayerList.
            List<LobbyPlayer> LobbyPlayers = new List<LobbyPlayer>();
            foreach(Player player in PhotonNetwork.PlayerList)
            {
                LobbyPlayers.Add(new LobbyPlayer(player));
            }

            //Set room properties to contain player list.
            room.SetCustomProperties(new Hashtable() { { InRoomCallbacks.RoomPlayerListPropertyKey, SerializePlayerList(LobbyPlayers) } });
        }

        /// <summary>
        /// Creates and returns player list data.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>LobbyPlayers as byte[]</returns>
        internal byte[] GetPlayerListData(Room room)
        {
            //Create lobbyPlayer list from Room PlayerList.
            List<LobbyPlayer> LobbyPlayers = new List<LobbyPlayer>();
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                LobbyPlayers.Add(new LobbyPlayer(player));
            }

            //Set room properties to contain player list.
            return SerializePlayerList(LobbyPlayers);
        }

        /// <summary>
        /// Attempts to get the player rank
        /// </summary>
        /// <param name="player"></param>
        /// <returns>Player Rank</returns>
        public static int GetPlayerRank(Player player)
        {
            try
            {
                if (player.CustomProperties.TryGetValue("RP_PR", out object obj))
                    return (int)obj;
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogWarning($"Failed to parse player rank\n{e}");
            }
            return 0;
        }

        /// <summary>
        /// Attempts to get the player favor rank
        /// </summary>
        /// <param name="player"></param>
        /// <returns>Player Favor Level</returns>
        public static int GetPlayerFavorRank(Player player)
        {
            try
            {
                if (player.CustomProperties.TryGetValue("RP_FR", out object obj))
                    return (int)obj;
            }
            catch (Exception e)
            {
                BepinPlugin.Log.LogWarning($"Failed to parse player favor rank\n{e}");
            }
            return 0;
        }
    }
}
