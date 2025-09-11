using Photon.Realtime;

namespace VoidManager.LobbyPlayerList
{
    internal class LobbyPlayer
    {
        public LobbyPlayer(Player player)
        {
            Name = player.NickName;
            UserID = player.UserId;
            myPlayer = player;
            Rank = LobbyPlayerListManager.GetPlayerRank(player);
            FavorRank = LobbyPlayerListManager.GetPlayerFavorRank(player);
        }

        public LobbyPlayer(string name, string userID, int rank, int favorRank)
        {
            Name = name;
            UserID = userID;
            Rank = rank;
            FavorRank = favorRank;
        }

        internal string Name;
        internal string UserID;
        internal int Rank;
        internal int FavorRank;
        internal Player myPlayer;
    }
}
