using Gameplay.Loot;
using Gameplay.Quests;
using ResourceAssets;
using System.Collections.Generic;
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

        /// <summary>
        /// Attempts to add an asset to the provided Container.
        /// </summary>
        /// <typeparam name="U">Container</typeparam>
        /// <typeparam name="T">Object type</typeparam>
        /// <typeparam name="V">Asset Def</typeparam>
        /// <param name="container">Container for adding resources</param>
        /// <param name="assetDefinition">Asset Definition for adding asset</param>
        /// <returns>succesfully registered asset.</returns>
        public static bool TryRegisterAsset<U, T, V>(this ResourceAssetContainer<U, T, V> container, V assetDefinition) where U : ResourceAssetContainerBase where T : UnityEngine.Object where V : ResourceAssetDef<T>
        {
            if (container.assetDefLUT.ContainsKey(assetDefinition.AssetGuid))
            {
                BepinPlugin.Log.LogWarning("Registration.TryRegisterAsset() Could not register an asset: GUID already exists.");
                return false;
            }
            container.AssetDescriptions.Add(assetDefinition);
            container.assetDefLUT.Add(assetDefinition.AssetGuid, assetDefinition);
            return true;
        }

        private static HashSet<GUIDUnion> VanillaGUIDs;

        private static Dictionary<GUIDUnion, string> RegisteredGUIDs;

        /// <summary>
        /// Finds all Vanilla GUIDs and caches them.
        /// </summary>
        private static void FindVanillaGUIDs()
        {
            if (VanillaGUIDs != null) return;

            VanillaGUIDs = new();

            foreach (ResourceAssetContainerBase container in ResourceAssetContainerRegister.Instance.Containers)
            {
                ResourceAssetContainer<ResourceAssetContainerBase, Object, ResourceAssetDef<Object>> AssetContainer = container as ResourceAssetContainer<ResourceAssetContainerBase, Object, ResourceAssetDef<Object>>;
                foreach (GUIDUnion GUID in AssetContainer.assetDefLUT.Keys)
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

            if (VanillaGUIDs.Contains(GUID) || RegisteredGUIDs.ContainsKey(GUID)) return true;

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
        /// Creates and registers a GUID with the provided registration ID. Provide a 100% unique asset registration ID, such as GUID + Asset Name.
        /// </summary>
        /// <param name="RegistrationID"></param>
        /// <returns>Registered GUID</returns>
        public static GUIDUnion GenerateAndRegisterGUID(string RegistrationID)
        {
            GUIDUnion guid = GenerateGUID(RegistrationID);
            RegisteredGUIDs.Add(guid, RegistrationID);
            return guid;
        }

        /// <summary>
        /// Attempts to researve a custom GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="RegisterID"></param>
        /// <param name="AutoPickGUID"></param>
        /// <returns></returns>
        public static bool TryReserveAssetGUID(ref GUIDUnion guid, string RegisterID, bool AutoPickGUID = false)
        {
            // Pick first available GUID
            if (guid == null || guid == default)
            {
                if (AutoPickGUID)
                {
                    guid = GenerateGUID(RegisterID);
                }
                else
                {
                    return false;
                }
            }

            if (VanillaGUIDs.Contains(guid) || RegisteredGUIDs.ContainsKey(guid))
            {
                if (AutoPickGUID)
                {
                    guid = GenerateGUID(RegisterID);
                }
                else
                {
                    return false;
                }
            }

            RegisteredGUIDs.Add(guid, RegisterID);
            return true;
        }
    }
}
