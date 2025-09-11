using CG.Profile;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using VoidManager.Callbacks;
using VoidManager.LobbyPlayerList;

namespace VoidManager.MPModChecks.Patches
{
    //On room create, initialize room properties with modded properties
    [HarmonyPatch(typeof(PhotonService), "PhotonCreateRoom")]
    internal class RoomInfoPatch
    {
        static RoomOptions PatchMethod(RoomOptions RoomOptions)
        {
            //Assign Host mod list to room.
            RoomOptions.CustomRoomProperties[InRoomCallbacks.RoomModsPropertyKey] = MPModCheckManager.Instance.MyModListData;

            //Assign Host UserID and Player name to room.
            RoomOptions.CustomRoomProperties[InRoomCallbacks.RoomPlayerListPropertyKey] = LobbyPlayerListManager.SerializePlayerList(new List<LobbyPlayer>() { new LobbyPlayer(PhotonNetwork.LocalPlayer.NickName, PhotonNetwork.LocalPlayer.UserId, PlayerProfile.Instance.Profile.Rank, PlayerProfile.Instance.Profile.FavorRank) });

            //Add Property keys for public display.
            RoomOptions.CustomRoomPropertiesForLobby = RoomOptions.CustomRoomPropertiesForLobby.Concat(new string[] { InRoomCallbacks.RoomModsPropertyKey, InRoomCallbacks.RoomPlayerListPropertyKey }).ToArray();

            return RoomOptions;
        }

        //insert PatchMethod before PhotonNetwork.CreateRoom is called, catching and returning RoomOptions value.
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RoomPropertiesPatch(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> InstructionList = instructions.ToList();

            int count = InstructionList.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (InstructionList[i].opcode == OpCodes.Call)
                {
                    if ((MethodInfo)InstructionList[i].operand == AccessTools.Method(typeof(PhotonNetwork), "CreateRoom"))
                    {
                        InstructionList.Insert(i - 3, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RoomInfoPatch), "PatchMethod")));
                    }
                    else
                    {
                        BepinPlugin.Log.LogError("Failed to patch PhotonService.PhotonCreateRoom. Targeted method appears to have changed. Index: " + i.ToString());
                    }
                    break;
                }
            }
            return InstructionList.AsEnumerable();
        }
    }
}
