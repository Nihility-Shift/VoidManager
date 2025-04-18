using CG.Game;
using CG.Ship.Modules;
using Gameplay.Quests;
using Gameplay.Ship.VoidJump;
using ResourceAssets;
using System;
using System.Collections.Generic;

namespace VoidManager.Utilities
{
    /// <summary>
    /// Contains ultility methods for the game.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// Reference to the clients current game.
        /// </summary>
        public static ClientGame Instance
        {
            get => ClientGame.Current;
        }

        /// <summary>
        /// Boolean value if within game.
        /// </summary>
        public static bool InGame
        {
            get
            {
                //Equivelant to Instance == null ? false : true
                return Instance != null;
            }
        }

        /// <summary>
        /// Returns list of in game players.
        /// </summary>
        public static List<CG.Game.Player.Player> Players
        {
            get
            {
                if (!InGame) return new List<CG.Game.Player.Player>();
                return ClientGame.Current.Players;
            }
        }

        /// <summary>
        /// Gets a player id from player.
        /// </summary>
        /// <param name="player">Player of the id / ActorNumber to get</param>
        /// <returns>id / ActorNumber</returns>
        public static int GetIDFromPlayer(CG.Game.Player.Player player)
        {
            if (player == null || player.photonView == null || player.photonView.Owner == null) return -1;
            return player.photonView.Owner.ActorNumber;
        }

        /// <summary>
        /// Gets a player from supplied player id.
        /// </summary>
        /// <param name="id">Id of the Player to get (From Playerlist) / ActorNumber</param>
        /// <returns>Player GameObject</returns>
        public static CG.Game.Player.Player GetPlayerFromID(int id)
        {
            if (!InGame) return null;
            return ClientGame.Current.GetPlayerCharacterByActorNumber(id);
        }

        /// <summary>
        /// Gets a player from supplied player name.
        /// </summary>
        /// <param name="playerName">Fullname of the player to get</param>
        /// <returns>Player GameObject</returns>
        public static CG.Game.Player.Player GetPlayerByName(string playerName)
        {
            if (!InGame) return null;
            foreach (CG.Game.Player.Player player in Players)
            {
                if (string.Equals(player.DisplayName, playerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Does a player ship exist
        /// </summary>
        /// <returns>True if a player ship exists, false otherwise</returns>
        public static bool PlayerShipExists { get => ClientGame.Current?.PlayerShip?.Platform != null; }

        /// <summary>
        /// Is the player ship in a void jump<br/><br/>
        /// true for: VoidJumpTravellingStable, VoidJumpTravellingUnstable, VoidJumpInterdiction, VoidJumpApproachingDestination, VoidJumpSpinningDown<br/>
        /// false otherwise
        /// </summary>
        public static bool InVoid
        {
            get
            {
                VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.GameObject?.GetComponent<VoidJumpSystem>();

                if (voidJumpSystem == null)
                    return false;

                if (voidJumpSystem.ActiveState is VoidJumpTravellingStable or VoidJumpTravellingUnstable or VoidJumpInterdiction or VoidJumpApproachingDestination or VoidJumpSpinningDown)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// The player currently sitting in the Pilot's seat<br/>
        /// null if the seat is empty
        /// </summary>
        public static CG.Game.Player.Player CurrentPilot
        {
            get
            {
                TakeoverChair pilotsSeat = ClientGame.Current?.PlayerShip?.GetModule<Helm>()?.Chair as TakeoverChair;
                if (pilotsSeat == null || pilotsSeat.IsAvailable) return null;

                return ClientGame.Current.GetPlayerCharacterByActorNumber(pilotsSeat.photonView.Owner.ActorNumber);
            }
        }


        private static readonly GUIDUnion _EndlessQuestGUID = new GUIDUnion("57ff22b0dae09944b9fa81fe5c37c470");

        /// <summary>
        /// GUID of Endless Pilgrimage
        /// </summary>
        public static GUIDUnion EndlessQuestGUID
        {
            get
            {
                return _EndlessQuestGUID;
            }
        }

        /// <summary>
        /// QuestAsset of Endless Pilgrimage
        /// </summary>
        public static QuestAsset EndlessQuestAsset
        {
            get
            {
                return GetQuestAsset(EndlessQuestGUID);
            }
        }

        /// <summary>
        /// Safely attempts to get QuestAsset from GUID
        /// </summary>
        /// <param name="QuestGUID"></param>
        /// <param name="QuestAsset"></param>
        /// <returns>QuestAsset</returns>
        public static bool TryGetQuestAsset(GUIDUnion QuestGUID, out QuestAsset QuestAsset)
        {
            if (ResourceAssetContainer<QuestAssetContainer, QuestAsset, QuestAssetDef>.Instance.TryGetByGuid(QuestGUID, out QuestAssetDef questAssetDef))
            {
                QuestAsset = questAssetDef.Asset;
                return true;
            }
            BepinPlugin.Log.LogError("Provided QuestGUID did not exist");
            QuestAsset = null;
            return false;
        }

        /// <summary>
        /// Gets QuestAsset from GUID
        /// </summary>
        /// <param name="QuestGUID"></param>
        /// <returns>QuestAsset</returns>
        public static QuestAsset GetQuestAsset(GUIDUnion QuestGUID)
        {
            return ResourceAssetContainer<QuestAssetContainer, QuestAsset, QuestAssetDef>.Instance.GetAssetDefById(QuestGUID).Asset;
        }
    }
}
