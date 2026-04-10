using Gameplay.Loot;
using Gameplay.Quests;
using ResourceAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoidManager.Content
{
    /// <summary>
    /// Utilized for registering content to containers.
    /// </summary>
    public static class Registration
    {
        /// <summary>
        /// Adds a loot entry to the QuestAsset loot table.
        /// </summary>
        /// <param name="questAsset"></param>
        /// <param name="lootEntry">Entry for adding</param>
        public static void RegisterDrop(this QuestAsset questAsset, LootTableEntry lootEntry)
        {
            questAsset.LootTable.Loot.Add(lootEntry);
        }

        /// <summary>
        /// Removes a loot entry from the QuestAsset loot table.
        /// </summary>
        /// <remarks>Works by reference, so be sure to keep a reference to previously registered loot!</remarks>
        /// <param name="questAsset"></param>
        /// <param name="lootEntry">Loot entry for removal</param>
        /// <returns>True if loot found and removed. False if loot didn't exist in tables or couldn't be removed.</returns>
        public static bool UnRegisterDrop(this QuestAsset questAsset, LootTableEntry lootEntry)
        {
            return questAsset.LootTable.Loot.Remove(lootEntry);
        }

        /*
        /// <summary>
        /// Creates an Asset Definition for registration with a container.
        /// </summary>
        /// <typeparam name="D"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="Asset"></param>
        /// <param name="GUID"></param>
        /// <returns></returns>
        public static D CreateAssetDef<D, T>(T Asset, GUIDUnion GUID) where T : UnityEngine.Object where D : ResourceAssetDef<T>
        {
            D assetDef = (D)new ResourceAssetDef<T>;
            ProjectionCosmeticRef assetRef = new ProjectionCosmeticRef(new ResourceAssetRef(GUID, string.Empty));
            assetRef.ResourceAsset = Asset;
            assetDef.Ref = assetRef;

            return assetDef;
        }*/

        /// <summary>
        /// Attempts to add an asset to the asset container.
        /// </summary>
        /// <typeparam name="U">Container</typeparam>
        /// <typeparam name="T">Object type</typeparam>
        /// <typeparam name="V">Asset Def</typeparam>
        /// <param name="container">Container for adding resources</param>
        /// <param name="assetDefinition">Asset Definition for adding asset</param>
        /// <returns>succesfully registered asset.</returns>
        public static bool TryAddAsset<U, T, V>(this ResourceAssetContainer<U, T, V> container, V assetDefinition) where U : ResourceAssetContainerBase where T : UnityEngine.Object where V : ResourceAssetDef<T>
        {
            if (container == null) BepinPlugin.Log.LogError("Container was null");
            if (container.assetDefLUT == null) BepinPlugin.Log.LogError("LUT was null");
            if (assetDefinition.AssetGuid == null) BepinPlugin.Log.LogError("AssetDef GUID was null");

            if (container.assetDefLUT.ContainsKey(assetDefinition.AssetGuid))
            {
                BepinPlugin.Log.LogWarning("Registration.TryRegisterAsset() Could not register an asset: GUID already exists.");
                return false;
            }
            container.AssetDescriptions.Add(assetDefinition);
            container.assetDefLUT.Add(assetDefinition.AssetGuid, assetDefinition);
            return true;
        }

        /// <summary>
        /// Removes an asset from the asset container.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="container"></param>
        /// <param name="assetDefinition">Asset Definition to remove.</param>
        /// <returns>true if succesful, false if asset was not in the container.</returns>
        public static bool RemoveAsset<U, T, V>(this ResourceAssetContainer<U, T, V> container, V assetDefinition) where U : ResourceAssetContainerBase where T : UnityEngine.Object where V : ResourceAssetDef<T>
        {
            bool Success = container.AssetDescriptions.Remove(assetDefinition);
            Success &= container.assetDefLUT.Remove(assetDefinition.AssetGuid);
            return Success;
        }

        private static HashSet<GUIDUnion> VanillaGUIDs;

        private static Dictionary<string, GUIDUnion> RegisteredGUIDs = new();

        /// <summary>
        /// Finds all Vanilla GUIDs and caches them.
        /// </summary>
        private static void FindVanillaGUIDs()
        {
            if (VanillaGUIDs != null) return;

            VanillaGUIDs = new();

            foreach (IResourceAssetContainer container in ResourceAssetContainerRegister.Instance.Containers)
            {
                foreach (GUIDUnion GUID in container.GetAllItems().Select(def => def.AssetGuid))
                {
                    VanillaGUIDs.Add(GUID);
                }
            }
        }

        /// <summary>
        /// Tests if the specified GUID is currently registered
        /// </summary>
        /// <param name="GUID">GUID for testing</param>
        /// <returns>True if registered. False otherwise.</returns>
        public static bool IsGUIDRegistered(GUIDUnion GUID)
        {
            // load vanilla GUIDs if not yet loaded.
            FindVanillaGUIDs();

            if (VanillaGUIDs.Contains(GUID) || RegisteredGUIDs.ContainsValue(GUID)) return true;

            return false;
        }

        /// <summary>
        /// Returns a newly generated GUID.
        /// </summary>
        public static GUIDUnion RandomGUID => new GUIDUnion(System.Guid.NewGuid().ToString("N")); // "N" specifies a hexidecimal string with no special grouping, as occurs with other formats.

        /// <summary>
        /// Generates a new unregistered GUID based on the provided registrationID. Provide a 100% unique asset registration ID, such as GUID + Asset Name.
        /// </summary>
        /// <param name="RegistrationID">string-based ID for unique identification of the asset.</param>
        /// <returns>Generated GUID</returns>
        public static GUIDUnion GenerateGUID(string RegistrationID)
        {
            // Attempt hashing Registration ID for GUID.
            GUIDUnion hashedGUID = new GUIDUnion(Hash128.Compute(RegistrationID).ToString());
            for (int i = 0; i < 10; i++)
            {
                if (Configs.IsDebugMode) BepinPlugin.Log.LogInfo($"Hashed GUID: {RegistrationID}");

                if (!IsGUIDRegistered(hashedGUID))
                {
                    return hashedGUID;
                }
                hashedGUID.m_Value3++;
            }

            // Generate random GUIDs if above failed.
            GUIDUnion randomGUID = RandomGUID;
            for (int i = 0; i < 10; i++)
            {
                if (Configs.IsDebugMode) BepinPlugin.Log.LogInfo($"Hashed GUID: {randomGUID}");
                if (!IsGUIDRegistered(randomGUID))
                {
                    return randomGUID;
                }
                randomGUID = RandomGUID;
            }

            //No Unregistered GUID generated.
            throw new System.Exception("Failed to generate new GUID; Please notify Nihility Shift that GUID generation failed.");
        }

        /// <summary>
        /// Creates and registers a GUID with the provided registration string. Provide a 100% unique asset registration ID, such as GUID + Asset Name.
        /// </summary>
        /// <param name="RegisterString"></param>
        /// <returns>Registered GUID</returns>
        public static GUIDUnion GenerateAndRegisterGUID(string RegisterString)
        {
            GUIDUnion guid = GenerateGUID(RegisterString);
            RegisteredGUIDs.Add(RegisterString, guid);
            return guid;
        }

        /// <summary>
        /// Attempts to researve a custom GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="RegisterString"></param>
        /// <param name="AutoPickGUID"></param>
        /// <returns></returns>
        public static bool TryRegisterAssetGUID(ref GUIDUnion guid, string RegisterString, bool AutoPickGUID = false)
        {
            // Pick first available GUID
            if (guid == null || guid == default)
            {
                if (AutoPickGUID)
                {
                    guid = GenerateGUID(RegisterString);
                }
                else
                {
                    return false;
                }
            }

            if (VanillaGUIDs.Contains(guid) || RegisteredGUIDs.ContainsValue(guid))
            {
                if (AutoPickGUID)
                {
                    guid = GenerateGUID(RegisterString);
                }
                else
                {
                    return false;
                }
            }

            RegisteredGUIDs.Add(RegisterString, guid);
            return true;
        }

        /// <summary>
        /// Finds GUID from provided registration string
        /// </summary>
        /// <param name="RegisterString">Unique string used when registering a GUID</param>
        /// <returns></returns>
        public static GUIDUnion GetGUID(string RegisterString)
        {
            return RegisteredGUIDs[RegisterString];
        }

        /// <summary>
        /// Finds GUID from provided registration string
        /// </summary>
        /// <param name="RegisterString">Unique string used when registering a GUID</param>
        /// <param name="GUID"></param>
        /// <returns>True if found, False if not</returns>
        public static bool TryGetGUID(string RegisterString, out GUIDUnion GUID)
        {
            return RegisteredGUIDs.TryGetValue(RegisterString, out GUID);
        }
    }
}
