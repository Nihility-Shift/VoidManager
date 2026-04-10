using CG.Client.UserData;
using CG.Ship.Object;
using ResourceAssets;
using System;
using System.Collections.Generic;

namespace VoidManager.Content
{
    /// <summary>
    /// API for modifying recipe UnlockOptions.
    /// </summary>
    public static class Unlocks
    {
        private static Dictionary<GUIDUnion, Tuple<string, UnlockOptions>> ModifiedUnlockOptions = new();

        /// <summary>
        /// Sets UnlockOptions for GUID if previously-existing UnlockOptions exists.
        /// </summary>
        /// <param name="GUID"></param>
        /// <param name="CallerID"></param>
        /// <param name="UnlockOptions"></param>
        /// <exception cref="ArgumentException">An asset with the provided GUID does not exist.</exception>
        /// <returns>UnlockOptions succesfully modified</returns>
        public static bool SetUnlockOptions(GUIDUnion GUID, string CallerID, UnlockOptions UnlockOptions)
        {
            if (!UnlockContainer.Instance.TryGetByGuid(GUID, out UnlockItemDef asset))
            {
                throw new ArgumentException("An asset with the provided GUID does not exist.");
            }
            else if (ModifiedUnlockOptions.TryGetValue(GUID, out Tuple<string, UnlockOptions> value))
            {
                if (value.Item1 != CallerID)
                {
                    BepinPlugin.Log.LogError($"Attempted to modify recipe for object at GUID: {GUID}, however it has already been modified by another mod.");
                    return false;
                }
                else //Mod that set GUID is overwriting value.
                {
                    asset.unlockOptions = UnlockOptions;
                    return true;
                }
            }
            else
            {
                ModifiedUnlockOptions.Add(GUID, new Tuple<string, UnlockOptions>(CallerID, asset.unlockOptions));
                asset.unlockOptions = UnlockOptions;
                return true;
            }
        }

        /// <summary>
        /// Undoes UnlockOptions modification for the provided GUID
        /// </summary>
        /// <param name="GUID"></param>
        /// <param name="CallerID"></param>
        public static void ResetUnlockOptions(GUIDUnion GUID, string CallerID)
        {
            if (ModifiedUnlockOptions.TryGetValue(GUID, out Tuple<string, UnlockOptions> value))
            {
                if (value.Item1 != CallerID)
                {
                    throw new ArgumentException("CallerID must match Assignment CallerID. Maybe another mod changed the same UnlockOptions?", "CallerID");
                }
                UnlockContainer.Instance.GetAssetDefById(GUID).unlockOptions = value.Item2;
                ModifiedUnlockOptions.Remove(GUID);
            }
        }

        /// <summary>
        /// Returns the current UnlockOptions for the given GUID.
        /// </summary>
        /// <param name="GUID"></param>
        /// <returns>UnlockOptions for GUID</returns>
        public static UnlockOptions GetUnlockOptions(GUIDUnion GUID)
        {
            return UnlockContainer.Instance.GetAssetDefById(GUID).unlockOptions;
        }

        /// <summary>
        /// Returns whether the given GUID UnlockOptions was modified.
        /// </summary>
        /// <param name="GUID"></param>
        /// <returns>UnlockOptions modified</returns>
        public static bool UnlockOptionsModified(GUIDUnion GUID)
        {
            return ModifiedUnlockOptions.ContainsKey(GUID);
        }

        /// <summary>
        /// Creates an UnlockItemDef with values assigned.
        /// </summary>
        /// <param name="GUID"></param>
        /// <param name="UO"></param>
        /// <param name="rarity"></param>
        /// <returns></returns>
        public static UnlockItemDef CreateUnlockItemDef(GUIDUnion GUID, UnlockOptions UO, RarityType rarity = RarityType.None)
        {
            UnlockItemDef UIDef = new UnlockItemDef();
            UnlockItemRef UIRef = new UnlockItemRef(GUID, string.Empty);
            UIDef.Ref = UIRef;
            UIDef.rarity = rarity;
            UIDef.unlockOptions = UO;

            return UIDef;
        }

        /// <summary>
        /// Public extension for setting unlock options of a UID.
        /// </summary>
        /// <param name="UID"></param>
        /// <param name="UO"></param>
        public static void SetUnlockOptions(this UnlockItemDef UID, UnlockOptions UO)
        {
            UID.unlockOptions = UO;
        }
    }
}
