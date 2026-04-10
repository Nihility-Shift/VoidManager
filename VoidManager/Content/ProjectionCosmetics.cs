using CG.Client.PlayerData;
using CG.Client.UserData;
using Client.Player.Interactions;
using ResourceAssets;
using System.Collections.Generic;
using UnityEngine;

namespace VoidManager.Content
{
    /// <summary>
    /// API for adding Projection Cosmetcs
    /// </summary>
    public static class ProjectionCosmetics
    {
        /// <summary>
        /// Creates and registers a ProjectionCosmetic.
        /// </summary>
        /// <param name="GUiD"></param>
        /// <param name="projectionCosmetic"></param>
        /// <param name="contextInfo"></param>
        /// <returns></returns>
        public static bool CreateAndRegisterProjectionAsset(GUIDUnion GUiD, ProjectionCosmetic projectionCosmetic, ContextInfo contextInfo = null)
        {
            // Create asset def, ref, for container registration.
            ProjectionCosmeticDef AssetDef = CreateProjectionAssetDef(GUiD, projectionCosmetic, contextInfo);

            // Attempt register asset with cosmetic container.
            return ProjectionCosmeticContainer.Instance.TryAddAsset(AssetDef);

            //Unlock code

            // Create unlockable options.
            UnlockOptions UO = new UnlockOptions();
            UO.UnlockCriteria = UnlockCriteriaType.Always;

            // Create UnlockItem Def/Ref
            UnlockItemDef MFprojectionUnlockDef = new UnlockItemDef();
            UnlockItemRef MFprojectionUnlockRef = new UnlockItemRef(GUiD, string.Empty);
            MFprojectionUnlockDef.Ref = MFprojectionUnlockRef;
            MFprojectionUnlockDef.rarity = CG.Ship.Object.RarityType.Common;
            MFprojectionUnlockDef.unlockOptions = UO;

            UnlockContainer.Instance.TryAddAsset(MFprojectionUnlockDef);
        }

        /// <summary>
        /// Returns a new Projection instance with values assigned.
        /// </summary>
        /// <param name="AssetGUID">GUID for SO instance</param>
        /// <param name="texture">Texture for display</param>
        /// <param name="animationModifiers">Applicable animation modifers</param>
        /// <param name="customMaterialOverride"></param>
        /// <returns></returns>
        public static ProjectionCosmetic CreateProjectionAsset(GUIDUnion AssetGUID, Texture2D texture, List<AnimationModifier> animationModifiers = null, Material customMaterialOverride = null)
        {
            ProjectionCosmetic projectionCosmeticInstance = ScriptableObject.CreateInstance<ProjectionCosmetic>();
            projectionCosmeticInstance.assetGuid = AssetGUID;
            projectionCosmeticInstance.ProjectionTexture = texture;
            projectionCosmeticInstance.animationModifiers = animationModifiers ?? new(); // List must not be null.
            projectionCosmeticInstance.customMaterialOverride = customMaterialOverride;

            return projectionCosmeticInstance;
        }

        /// <summary>
        /// Creates Ref and Def with assigned GUID. Optionally provide Asset and ContextInfo.
        /// </summary>
        /// <param name="GUID"></param>
        /// <param name="Asset"></param>
        /// <param name="contextInfo"></param>
        /// <returns></returns>
        public static ProjectionCosmeticDef CreateProjectionAssetDef(GUIDUnion GUID, ProjectionCosmetic Asset = null, ContextInfo contextInfo = null)
        {
            ProjectionCosmeticDef ProjectionDef = new ProjectionCosmeticDef();
            ProjectionCosmeticRef ProjectionRef = new ProjectionCosmeticRef(new ResourceAssetRef(GUID, string.Empty));
            ProjectionRef._cachedPathGuid = GUID;
            ProjectionRef.ResourceAsset = Asset;
            ProjectionDef.Ref = ProjectionRef;
            ProjectionDef.ContextInfo = contextInfo;

            return ProjectionDef;
        }
    }
}
