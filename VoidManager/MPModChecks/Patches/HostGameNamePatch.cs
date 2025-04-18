﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using VoidManager.Utilities;

namespace VoidManager.MPModChecks.Patches
{
    [HarmonyPatch(typeof(PhotonService))]
    internal class HostGameNamePatch
    {
        [HarmonyPatch("SetCurrentRoomName")]
        static void Prefix(ref string name)
        {
            name = SetGameName(name);
        }

        [HarmonyPatch("PhotonCreateRoom")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> targetSequence = new()
            {
                new CodeInstruction(OpCodes.Ldstr, " Room"),
                new CodeInstruction(OpCodes.Call)
            };
            List<CodeInstruction> patchSequence = new()
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HostGameNamePatch), nameof(SetGameName)))
            };

            return HarmonyHelpers.PatchBySequence(instructions, targetSequence, patchSequence, HarmonyHelpers.PatchMode.AFTER, HarmonyHelpers.CheckMode.NONNULL);
        }

        public static string SetGameName(string name)
        {
            if (MPModCheckManager.Instance.HighestLevelOfMPMods == MultiplayerType.All)
            {
                if (!name.StartsWith(ModdedRoomTagPatch.ModsRequiredString, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    return $"{ModdedRoomTagPatch.ModsRequiredString} " + name;
                }
            }
            return name;
        }
    }
}
