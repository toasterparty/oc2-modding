using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using HarmonyLib;

namespace OC2Modding
{
    public static class ImportAYCEAssets
    {
        private static BundleHelper AyceBundleHelper;
        private static BundleHelper Oc2BundleHelper;

        private const long MAIN_AVATAR_DIRECTORY_ID = -1326050724655347751;

        private static string AYCE_BUNDLE_PATH = "StreamingAssets/aa/Windows/StandaloneWindows64/";
        private static string[] AYCE_BUNDLE_NAMES = new string[] {
            "persistent_assets_all.bundle", // Main Bundle
            "chefs_assets_all.bundle",
            "duplicate_aasets_models_characters_assets_all.bundle",
            "duplicate_aasets_models_characters_assets_all1.bundle",
            "duplicate_aasets_models_characters_assets_all2.bundle",
            "duplicate_aasets_models_characters_assets_all3.bundle",
            "duplicateassetisolation_assets_all11.bundle", // TODO: this is just a single sprite logo for cat ginger
        };

        private static string OC2_BUNDLE_PATH = "Overcooked2_Data/StreamingAssets/Windows/";

        public static void Awake()
        {
            /* Inject Harmony Mods */
            Harmony.CreateAndPatchAll(typeof(ImportAYCEAssets));

            try
            {
                MigrateAvatars();
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to migrate avatars from AYCE to OC2 \n{e}");
                GameLog.LogMessage($"Failed to migrate avatars from AYCE to OC2 \n{e.Message}");
            }
        }

        private static void MigrateAvatars()
        {
            /* Initialize AYCE Bundle Helper */
            var ayceBasePath = "C:/Other/Games/Steam/steamapps/common/Overcooked! All You Can Eat/Overcooked All You Can Eat_Data/";
            var ayceBaseBundlePath = Path.Combine(ayceBasePath, AYCE_BUNDLE_PATH);

            AyceBundleHelper = new BundleHelper(classDataTpkPath: "classdata.tpk");
            foreach (var bundleName in AYCE_BUNDLE_NAMES)
            {
                var bundlePath = Path.Combine(ayceBaseBundlePath, bundleName);
                AyceBundleHelper.LoadBundle(bundlePath);
            }

            /* Collect AYCE Assets */
            Dictionary<long, string> avatarIDsAndNames = GetAyceAvatarIDsAndNames();
            List<long> avatarRelatedIDs = GetAyceAvatarDepenencyIDs(avatarIDsAndNames.Keys.ToList());
            List<AssetData> avatarAssets = GetAyceAvatarAssets(avatarRelatedIDs);
            AyceBundleHelper.UnloadAll();
            OC2Modding.Log.LogInfo($"Unloaded AYCE Bundles");

            /* Initialize OC2 Bundle Helper */
            var oc2BasePath = "C:/Other/Games/Steam/steamapps/common/Overcooked! 2/";
            var oc2ManagedPath = oc2BasePath + "Overcooked2_Data/Managed";
            var oc2BundlePath = oc2BasePath + OC2_BUNDLE_PATH;
            var oc2AvatarBundlePath = oc2BundlePath + "bundle18";
            var oc2AvatarBundleBakPath = oc2BundlePath + "bundle18.bak";
            var oc2PigBundlePath = oc2BundlePath + "bundle158";

            if (!File.Exists(oc2AvatarBundleBakPath) && File.Exists(oc2AvatarBundlePath))
            {
                File.Copy(oc2AvatarBundlePath, oc2AvatarBundleBakPath);
            }

            if (File.Exists(oc2AvatarBundlePath))
            {
                File.Delete(oc2AvatarBundlePath);
            }

            Oc2BundleHelper = new BundleHelper(oc2ManagedPath, classDataTpkPath: "classdata.tpk");
            Oc2BundleHelper.LoadBundle(oc2AvatarBundleBakPath);
            Oc2BundleHelper.LoadBundle(oc2PigBundlePath);

            /* Convert assets to OC2 format and write new bundle */

            List<AssetsReplacer> replacers = ConvertAvatarAssets(avatarAssets);
            AddAvatarsToBundle(avatarIDsAndNames, replacers, oc2AvatarBundlePath);
            Oc2BundleHelper.UnloadAll();
            OC2Modding.Log.LogInfo($"Unloaded OC2 Bundles");
        }

        private static Dictionary<long, string> GetAyceAvatarIDsAndNames()
        {
            var avatarIDsAndNames = new Dictionary<long, string>();
            var mainAvatarDirectory = AyceBundleHelper.GetBaseField(MAIN_AVATAR_DIRECTORY_ID);
            if (mainAvatarDirectory["m_Name"].AsString != "MainAvatarDirectory")
            {
                throw new Exception($"Unexpected name for AYCE MainAvatarDirectory: {mainAvatarDirectory["m_Name"].AsString}");
            }

            var baseAvatars = mainAvatarDirectory["m_baseAvatars"][0];

            foreach (var baseAvatar in baseAvatars)
            {
                string baseName = "<unknown-name>";

                try
                {
                    baseName = baseAvatar["m_name"].AsString;
                    foreach (var variant in baseAvatar["m_variants"][0])
                    {
                        try
                        {
                            var variantID = variant["m_PathID"].AsLong;
                            if (variantID == 0)
                            {
                                continue;
                            }

                            var variantBaseField = AyceBundleHelper.GetBaseField(variantID);
                            var variantName = variantBaseField["m_Name"].AsString;
                            avatarIDsAndNames[variantID] = variantName;
                            return avatarIDsAndNames;
                        }
                        catch (Exception e)
                        {
                            OC2Modding.Log.LogWarning($"Failed to read a variant ID/Name of '{baseName}'\n{e}");
                        }
                    }
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"Failed to read variant IDs/Names for '{baseName}'\n{e}");
                }
            }

            if (avatarIDsAndNames.Count == 0)
            {
                throw new Exception("Failed to successfully read any avatar IDs/Names from AYCE");
            }

            OC2Modding.Log.LogInfo($"Found {avatarIDsAndNames.Count} AYCE avatar IDs to potentially convert");

            return avatarIDsAndNames;
        }

        private static List<long> GetAyceAvatarDepenencyIDs(List<long> avatarIDs)
        {
            List<long> avatarRelatedIDs = new List<long>(avatarIDs);

            foreach (var avatarID in avatarIDs)
            {
                try
                {
                    var depIDs = AyceBundleHelper.GetDependencies(avatarID);
                    foreach (var depID in depIDs)
                    {
                        avatarRelatedIDs.Add(depID);
                    }
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"Failed to read dependencies of {avatarID}\n{e}");
                }
            }
        
            avatarRelatedIDs = avatarRelatedIDs.Distinct().ToList();
            avatarRelatedIDs.Remove(0);

            if (avatarRelatedIDs.Count - avatarIDs.Count <= 0)
            {
                throw new Exception("Failed to successfully read any dependency IDs from AYCE for any avatar");
            }

            OC2Modding.Log.LogInfo($"Found {avatarRelatedIDs.Count - avatarIDs.Count} AYCE dependencies to potentially convert");   

            return avatarRelatedIDs;
        }

        private static List<AssetData> GetAyceAvatarAssets(List<long> assetIDs)
        {
            OC2Modding.Log.LogInfo($"Reading {assetIDs.Count} required assets from AYCE...");

            var avatarAssets = new List<AssetData>();

            foreach (var assetID in assetIDs)
            {
                try
                {
                    avatarAssets.Add(AyceBundleHelper.GetAssetData(assetID));
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"Failed to load asset {assetID} from AYCE\n{e}");
                }
            }

            if (avatarAssets.Count == 0)
            {
                throw new Exception("Failed to read any avatar-related assets from bundle(s) from AYCE");
            }

            OC2Modding.Log.LogInfo($"Read {avatarAssets.Count} avatar-related assets from AYCE");

            return avatarAssets;
        }

        private static List<AssetsReplacer> ConvertAvatarAssets(List<AssetData> avatarAssets)
        {
            var assetsReplacers = new List<AssetsReplacer>();

            foreach (var assetData in avatarAssets)
            {
                try
                {
                    var name = "";
                    try
                    {
                        name = assetData.baseField["m_Name"].AsString;
                    }
                    catch {}

                    var replacer = ConvertAsset(assetData);
                    if (replacer == null)
                    {
                        OC2Modding.Log.LogWarning($"Skipped converting {assetData.info.TypeId} | {name} | {assetData.info.PathId}");
                        continue; // skip converting
                    }

                    assetsReplacers.Add(replacer);
                    OC2Modding.Log.LogInfo($"Converted {assetData.info.TypeId} | {name} | {assetData.info.PathId}");
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogError($"Failed to convert {assetData.className} {assetData.info.PathId}:\n{e}");
                }
            }

            if (assetsReplacers.Count == 0)
            {
                throw new Exception("Failed to convert any AYCE avatar-related assets to OC2 specifications");
            }

            OC2Modding.Log.LogInfo($"Converted {assetsReplacers.Count} avatar-related assets from AYCE's format to OC2's format");

            return assetsReplacers;
        }

        private static void AddAvatarsToBundle(Dictionary<long, string> avatarIDsAndNames, List<AssetsReplacer> assetsReplacers, string outBundlePath)
        {
            int addedCount = 0;
            int skippedCount = 0;

            var mainAvatarDirectory = Oc2BundleHelper.GetBaseField(MAIN_AVATAR_DIRECTORY_ID);
            if (mainAvatarDirectory["m_Name"].AsString != "MainAvatarDirectory")
            {
                throw new Exception($"Unexpected name for id={MAIN_AVATAR_DIRECTORY_ID} '{mainAvatarDirectory["m_Name"].AsString}'");
            }

            var avatars = mainAvatarDirectory["Avatars.Array"];
            if (avatars == null)
            {
                throw new Exception($"Failed to find avatars list in MainAvatarDirectory");
            }

            if (avatars.Children.Count > 40)
            {
                throw new Exception($"{outBundlePath} appears to already be patched (tried to add {avatarIDsAndNames.Count} avatars when there were already {avatars.Children.Count})");
            }

            List<string> oc2Names = new List<string>(); 
            foreach (var avatar in avatars.Children)
            {
                try
                {
                    var id = avatar["m_PathID"].AsLong;
                    if (id == 0)
                    {
                        continue;
                    }

                    var baseField = Oc2BundleHelper.GetBaseField(id);
                    oc2Names.Add(baseField["m_Name"].AsString);
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"Failed to get the name of an oc2 avatar: {e.Message}");
                }
            }

            foreach (var idAndName in avatarIDsAndNames)
            {
                var avatarID = idAndName.Key;
                var avatarName = idAndName.Value;

                try
                {
                    if (oc2Names.Contains(avatarName) || Oc2BundleHelper.ContainsID(avatarID))
                    {
                        skippedCount++;
                        continue; // This chef comes from OC2, so no need to backport...
                    }

                    // Add chef entry to AvatarDirectoryData array
                    var avatar = ValueBuilder.DefaultValueFieldFromArrayTemplate(avatars);
                    avatar["m_FileID"].AsInt = 0;
                    avatar["m_PathID"].AsLong = avatarID; // TODO: check for collision with existing IDs
                    avatars.Children.Add(avatar);
                    addedCount++;
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogError($"Failed to add directory entry for converted avatar '{avatarName}'\n{e}");
                }
            }

            int beforeLen = assetsReplacers.Count;
            assetsReplacers = assetsReplacers.Where(x => !Oc2BundleHelper.ContainsID(x.GetPathID())).ToList();
            
            OC2Modding.Log.LogInfo($"Skipped adding {beforeLen - assetsReplacers.Count} assets whose IDs already existed in OC2");

            assetsReplacers.Add(Oc2BundleHelper.FileReplacer(MAIN_AVATAR_DIRECTORY_ID, mainAvatarDirectory));

            OC2Modding.Log.LogInfo($"Skipped adding {skippedCount} directory entries due to redundant chefs");
            
            Oc2BundleHelper.ModifyBundle(assetsReplacers, outBundlePath);
            OC2Modding.Log.LogInfo($"Successfully added {addedCount} of {avatarIDsAndNames.Count - skippedCount} attempted ChefAvatarData entries to OC2 and {assetsReplacers.Count - 1} dependencies from AYCE to OC2");
        }

        private static AssetsReplacerFromMemory ConvertAsset(AssetData assetData)
        {
            AssetTypeValueField converted;
            var type = (AssetClassID)assetData.info.TypeId;
            var scriptIndex = Oc2BundleHelper.GetScriptIndex(assetData.className);

            var SKIP_IDS = new long[] {
                // -6594572577078433743L, // Chef_AxolotlPink (ChefAvatarData)    `                                      
                // -8987147349260460087L, // ChefAvatarData                       `                  
                // -1673985575946039472L, // Chef_AxolotlPink                     `                      
                // 5048923704244734417L,  // Chef_AxolotlPink_Frontend            `                              
                // -8343719870780976275L, // Chef_AxolotlPink_UI                  `                          
                // 4358930316311271624L,  // CHR_Chef_01                          `                  
                // -6079689613363647449L, // chef_axolotl_pink                    `                      
                // 8775198998330553421L,  // New_Chef@FE_Idle_01                  `                          
                // 3037418633120753884L,  // New_Chef@Celebrate_01                `                          
                // 6182019441836036947L,  // 
            };

            if (SKIP_IDS.Contains(assetData.info.PathId))
            {
                var name = assetData.baseField["m_Name"].AsString;
                return null;
            }

            switch (type)
            {
                case AssetClassID.MonoBehaviour:
                {
                    converted = ConvertMonoBehaviour(assetData);
                    if (converted == null)
                    {
                        OC2Modding.Log.LogWarning($"{assetData.className} converter not yet implemented");
                        return null;
                    }

                    break;
                }
                case AssetClassID.MonoScript:
                {
                    converted = DefaultAssetConverter(assetData);
                    break;
                }
                case AssetClassID.GameObject:
                {
                    converted = DefaultAssetConverter(assetData);
                    break;
                }
                case AssetClassID.Sprite:
                {
                    converted = DefaultAssetConverter(assetData);
                    break;
                }
                case AssetClassID.AnimationClip:
                {
                    converted = DefaultAssetConverter(assetData);
                    break;
                }
                case AssetClassID.Material:
                {
                    converted = DefaultAssetConverter(assetData);
                    break;
                }
                default:
                {
                    throw new Exception($"Error: Cannot convert {type} because no converter is implemented");
                }
            }

            return new AssetsReplacerFromMemory(
                assetData.info.PathId,
                (int)type,
                (ushort)scriptIndex,
                converted.WriteToByteArray()
            );
        }

        private static AssetTypeValueField DefaultAssetConverter(AssetData assetData)
        {
            var newBaseField = Oc2BundleHelper.CreateBaseField((AssetClassID)assetData.info.TypeId);
            BundleHelper.CopyAsset(ref newBaseField, ref assetData.baseField, ref Oc2BundleHelper);
            return newBaseField;
        }

        private static AssetTypeValueField ConvertMonoBehaviour(AssetData assetData)
        {
            var oldBaseField = assetData.baseField;
            if (oldBaseField == null)
            {
                throw new Exception($"Unexpectedly null ayce baseField for {assetData.info.PathId}/{assetData.className}");
            }

            var newBaseField = Oc2BundleHelper.CreateBaseField(assetData.className);
            if (newBaseField == null)
            {
                throw new Exception($"Failed to make a new oc2 asset of className: {assetData.className}");
            }

            switch (assetData.className)
            {
                case "ChefAvatarData":
                {
                    return ConvertMBChefAvatarData(oldBaseField, newBaseField);
                }
                case "DLCFrontendData":
                {
                    return null;
                }
                default:
                {
                    throw new Exception($"Error: Cannot convert MonoBehavior '{assetData.className}' because no converter is implemented");
                }
            }
        }

        private static AssetTypeValueField ConvertMBChefAvatarData(AssetTypeValueField fromData, AssetTypeValueField toData)
        {
            toData["m_GameObject.m_FileID"].AsInt = 0;
            toData["m_GameObject.m_PathID"].AsLong = 0;

            toData["m_Enabled"].AsInt = 1;

            toData["m_Name"].AsString = fromData["m_Name"].AsString;

            toData["ModelPrefab.m_FileID"].AsInt = fromData["ModelPrefab.m_FileID"].AsInt;
            toData["ModelPrefab.m_PathID"].AsLong = fromData["ModelPrefab.m_PathID"].AsLong;

            toData["FrontendModelPrefab.m_FileID"].AsInt = fromData["ModelPrefab_Frontend.m_FileID"].AsInt;
            toData["FrontendModelPrefab.m_PathID"].AsLong = fromData["ModelPrefab_Frontend.m_PathID"].AsLong;

            toData["UIModelPrefab.m_FileID"].AsInt = fromData["ModelPrefab_UI.m_FileID"].AsInt;
            toData["UIModelPrefab.m_PathID"].AsLong = fromData["ModelPrefab_UI.m_PathID"].AsLong;

            toData["HeadName"].AsString = fromData["Head"].AsString;

            toData["ColourisationMode"].AsInt = (int)ChefMeshReplacer.ChefColourisationMode.SwapColourValue;
            toData["ActuallyAllowed"].AsBool = true;

            toData["ForDlc.m_FileID"].AsInt = 0;
            toData["ForDlc.m_PathID"].AsLong = 0;

            toData["m_PC"].AsBool = true;
            toData["m_XboxOne"].AsBool = true;
            toData["m_PS4"].AsBool = true;
            toData["m_Switch"].AsBool = true;

            return toData;
        }

        // [HarmonyPatch(typeof(MetaGameProgress), "Awake")]
        // [HarmonyPostfix]
        // private static void Awake(ref AvatarDirectoryData ___m_combinedAvatarDirectory)
        // {
        //     var dir = ___m_combinedAvatarDirectory;
        //     if (___m_combinedAvatarDirectory == null) return;

        //     OC2Modding.Log.LogInfo($"Before: {dir.Avatars.Length}|{dir.Colours.Length}");
        //     ApplyExtraAvatars(ref dir);
        //     OC2Modding.Log.LogInfo($"After: {dir.Avatars.Length}|{dir.Colours.Length}");
        // }

        [HarmonyPatch(typeof(MetaGameProgress), nameof(MetaGameProgress.GetUnlockedAvatars))]
        [HarmonyPostfix]
        private static void GetUnlockedAvatars(ref AvatarDirectoryData ___m_combinedAvatarDirectory, ref ChefAvatarData[] __result)
        {
            if (___m_combinedAvatarDirectory != null)
            {
                __result =___m_combinedAvatarDirectory.Avatars;
            }
        }
    }
}
