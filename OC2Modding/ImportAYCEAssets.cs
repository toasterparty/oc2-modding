using System;
using System.IO;
using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using HarmonyLib;

using UnityEngine;

namespace OC2Modding
{
    public static class ImportAYCEAssets
    {
        private const long AVATAR_DIRECTORY_ID = -1326050724655347751;

        private static AssetsManager Manager = new AssetsManager();

        private struct AssetData
        {
            public long id;
            public AssetClassID type;
            public string className;
            public AssetTypeValueField data;
            public List<AssetData> dependencies;
        };

        private struct MonoBehaviourTypeData
        {
            public string name;
            public int index;
            public int fileID;
            public long pathID;
            public AssetTypeTemplateField template;
        };
     
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(ImportAYCEAssets));

            Manager.LoadClassPackage("classdata.tpk");
            Manager.MonoTempGenerator = new MonoCecilTempGenerator("C:/Other/Games/Steam/steamapps/common/Overcooked! 2/Overcooked2_Data/Managed");

            if (Manager.MonoTempGenerator == null)
            {
                OC2Modding.Log.LogError($"Failed to init Mono template generator at (Managed folder)");
                return;
            }

            var oc2AvatarBundlePath = "C:/Other/Games/Steam/steamapps/common/Overcooked! 2/Overcooked2_Data/StreamingAssets/Windows/bundle18";
            var oc2AvatarBundleBakPath = oc2AvatarBundlePath + ".bak";

            if (!File.Exists(oc2AvatarBundleBakPath))
            {
                File.Copy(oc2AvatarBundlePath, oc2AvatarBundleBakPath);
            }

            ReadScriptInfo(oc2AvatarBundleBakPath);
            List<AssetData> chefMetadata = LoadAyceAvatars("C:/Other/Games/Steam/steamapps/common/Overcooked! All You Can Eat/Overcooked All You Can Eat_Data/StreamingAssets/aa/Windows/StandaloneWindows64/persistent_assets_all.bundle");
            AddAvatarsToBundle(oc2AvatarBundleBakPath, oc2AvatarBundlePath, chefMetadata);
            Manager.UnloadAll();
        }

        private static Dictionary<string, MonoBehaviourTypeData> MonoBehaviorTypes = new Dictionary<string, MonoBehaviourTypeData>();

        private static readonly string [] MONO_BEHAVIOR_TO_CONVERT = new string [] {
            "ChefAvatarData",
            "GameInputConfigData",
            "AudioDirectoryData",
            "DLCFrontendData",
        };

        private static void ReadScriptInfo(string bundlePath)
        {
            try
            {
                var bundle = Manager.LoadBundleFile(bundlePath);
                if (bundle == null)
                {
                    throw new Exception($"Failed to load bundle file: {bundlePath}");
                }
                
                var assets = Manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: true);
                if (assets == null || assets.file == null)
                {
                    throw new Exception($"Failed to load assets file from {bundlePath}");
                }

                foreach (var mb in MONO_BEHAVIOR_TO_CONVERT)
                {
                    ReadMonoBehaviorTypeData(assets, mb);
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to read script type data for {bundlePath}:\n {e}");
            }
        }

        private static void ReadMonoBehaviorTypeData(AssetsFileInstance assets, string className)
        {
            var scriptTypeInfos = AssetHelper.GetAssetsFileScriptInfos(Manager, assets);
            if (scriptTypeInfos == null)
            {
                throw new Exception($"Failed to read scriptTypeInfos");
            }

            var scriptIndex = scriptTypeInfos.FindIndex(s => s.ClassName == className);
            if (scriptIndex == -1)
            {
                throw new Exception($"Failed to find class of type '{className}'");
            }

            var scriptTypeInfo = scriptTypeInfos[scriptIndex];
            if (scriptTypeInfos == null)
            {
                throw new Exception($"Failed to get script info for class '{className}'");
            }

            var unityVersion = new UnityVersion(assets.file.Metadata.UnityVersion);
            var mbTempField = Manager.GetTemplateBaseField(assets, assets.file.Reader, -1, (int)AssetClassID.MonoBehaviour, 0xffff, assets.file.Metadata.TypeTreeEnabled, preferEditor: false, skipMonoBehaviourFields: true);
            if (mbTempField == null)
            {
                throw new Exception($"Failed to get template base field for assets file");
            }

            var template = Manager.MonoTempGenerator.GetTemplateField(
                    mbTempField,
                    scriptTypeInfo.AsmName,
                    scriptTypeInfo.Namespace,
                    className,
                    unityVersion
                );
            if (template == null)
            {
                throw new Exception($"Failed to make template for {className}");
            }

            var typeData = new MonoBehaviourTypeData {
                name = className,
                index = scriptIndex,
                fileID = assets.file.Metadata.ScriptTypes[scriptIndex].FileId,
                pathID = assets.file.Metadata.ScriptTypes[scriptIndex].PathId,
                template = template,
            };

            MonoBehaviorTypes[className] = typeData;

            // OC2Modding.Log.LogInfo($"{className} is index={typeData.index}"); 
        }

        private static List<AssetData> LoadAyceAvatars(string bundlePath)
        {
            var chefMetadata = new List<AssetData>();
            try
            {
                var bundle = Manager.LoadBundleFile(bundlePath);
                if (bundle == null)
                {
                    throw new Exception($"Failed to load bundle file: {bundlePath}");
                }

                var assets = Manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: true);
                if (assets == null || assets.file == null)
                {
                    throw new Exception($"Failed to load assets file from {bundlePath}");
                }

                var mainAvatarDirectoryInfo = assets.file.GetAssetInfo(AVATAR_DIRECTORY_ID);
                var mainAvatarDirectory = Manager.GetBaseField(assets, mainAvatarDirectoryInfo);
                if (mainAvatarDirectory["m_Name"].AsString != "MainAvatarDirectory")
                {
                    throw new Exception($"Unexpected name for AYCE MainAvatarDirectory: {mainAvatarDirectory["m_Name"].AsString}");
                }

                var baseAvatars = mainAvatarDirectory["m_baseAvatars"][0];
                // OC2Modding.Log.LogInfo($"{baseAvatars.AsArray.size} baseAvatars to parse");

                foreach (var baseAvatar in baseAvatars)
                {
                    string baseName = baseAvatar["m_name"].AsString;
                    var variants = baseAvatar["m_variants"][0];
                    // OC2Modding.Log.LogInfo($"{baseName} has {variants.AsArray.size} variants");

                    foreach (var variant in variants)
                    {
                        try
                        {
                            var assetData = AssetDataFromID(assets, variant["m_PathID"].AsLong);
                            chefMetadata.Add(assetData);

                            // OC2Modding.Log.LogInfo($"Deps of {assetData.id}:");
                            // foreach (var dep in assetData.dependencies)
                            // {
                            //     OC2Modding.Log.LogInfo($"    {dep.id}");
                            // }
                        }
                        catch (Exception e)
                        {
                            OC2Modding.Log.LogError($"Failed to load ayce chef variant:\n {e}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to load AYCE chefs from bundle\n {e}");
            }

            return chefMetadata;
        }

        private static AssetData AssetDataFromID(AssetsFileInstance assets, long id)
        {
            var assetInfo = assets.file.GetAssetInfo(id);
            var assetData = Manager.GetBaseField(assets, assetInfo);

            var depIDs = new List<long>();
            CollectAssetDependencies(ref assets, assetData, ref depIDs);

            var dependencies = new List<AssetData>();
            foreach (var depID in depIDs)
            {
                dependencies.Add(
                    AssetDataFromID(assets, depID, null)
                );               
            }

            return AssetDataFromID(assets, id, dependencies);
        }

        private static AssetData AssetDataFromID(AssetsFileInstance assets, long id, List<AssetData> dependencies)
        {
            var info = assets.file.GetAssetInfo(id);
            var type = (AssetClassID)info.TypeId;
            var data = Manager.GetBaseField(assets, info);
            var className = ScriptNameOfAsset(assets, info);

            return new AssetData {
                id = id,
                type = type,
                className = className,
                data = data,
                dependencies = dependencies,
            };
        }

        private static string ScriptNameOfAsset(AssetsFileInstance assets, AssetFileInfo info)
        {
            if (info.TypeId != (int)AssetClassID.MonoBehaviour)
            {
                return "";
            }

            var data = Manager.GetBaseField(assets, info);
            var id = data["m_Script.m_PathID"].AsLong;
            var scriptInfo = assets.file.GetAssetInfo(id);
            var scriptData = Manager.GetBaseField(assets, scriptInfo);
            return scriptData["m_ClassName"].AsString;
        }

        /* recursive tree search for asset ids referenced by this one */
        private static void CollectAssetDependencies(ref AssetsFileInstance assets, AssetTypeValueField data, ref List<long> deps)
        {
            if (data.FieldName == "m_PathID")
            {
                var depID = data.AsLong;

                if (depID == 0)
                {
                    return; // "null" reference
                }

                if (deps.Contains(depID))
                {
                    return; // avoid infinite recursion
                }

                var depInfo = assets.file.GetAssetInfo(depID);
                if (depInfo == null)
                {
                    // OC2Modding.Log.LogWarning($"{depID} doesn't exist in this file");
                    return;
                }

                var depData = Manager.GetBaseField(assets, depInfo);
                if (depData == null)
                {
                    OC2Modding.Log.LogError($"Couldn't get BaseField of {depID}");
                    return;
                }

                deps.Add(depID);

                // collect all dependencies from depenent asset
                CollectAssetDependencies(ref assets, data, ref deps);

                return;
            }

            if (data.Children == null)
            {
                return;
            }

            foreach (var child in data.Children)
            {
                // continue searching this asset
                CollectAssetDependencies(ref assets, child, ref deps);
            }
        }

        private static void AddAvatarsToBundle(string inBundlePath, string outBundlePath, List<AssetData> chefMetadata)
        {
            try
            {
                var bundle = Manager.LoadBundleFile(inBundlePath, true);
                if (bundle == null)
                {
                    throw new Exception($"Failed to load bundle file");
                }

                var assets = Manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: true);
                if (assets == null || assets.file == null)
                {
                    throw new Exception($"Failed to load assets file");
                }

                var mainAvatarDirectoryInfo = assets.file.GetAssetInfo(AVATAR_DIRECTORY_ID);
                if (mainAvatarDirectoryInfo == null)
                {
                    throw new Exception($"Failed to find file id={AVATAR_DIRECTORY_ID}");
                }

                var mainAvatarDirectory = Manager.GetBaseField(assets, mainAvatarDirectoryInfo);
                if (mainAvatarDirectory["m_Name"].AsString != "MainAvatarDirectory")
                {
                    throw new Exception($"Unexpected name for id={AVATAR_DIRECTORY_ID} '{mainAvatarDirectory["m_Name"].AsString}'");
                }

                var bundleReplacers = new List<BundleReplacer>();
                var assetsReplacers = new List<AssetsReplacer>();

                var avatars = mainAvatarDirectory["Avatars.Array"];
                if (avatars == null)
                {
                    throw new Exception($"Failed to find avatars list in MainAvatarDirectory");
                }

                if (avatars.Children.Count > 40)
                {
                    throw new Exception($"{inBundlePath} appears to already be patched (tried to add {chefMetadata.Count} Chefs when there were already {avatars.Children.Count})");
                }

                List<string> existingNames = new List<string>(); 
                foreach (var avatar in avatars.Children)
                {
                    var id = avatar["m_PathID"].AsLong;
                    if (id == 0)
                    {
                        continue;
                    }

                    var info = assets.file.GetAssetInfo(id);
                    if (info == null)
                    {
                        OC2Modding.Log.LogWarning($"Failed to lookup existing avatar: id={id}");
                        continue;
                    }

                    var data = Manager.GetBaseField(assets, info);
                    existingNames.Add(data["m_Name"].AsString);
                }

                var convertedIDs = new List<long>();

                foreach (var chef in chefMetadata)
                {
                    var chefName = "<unkown-name>";

                    try
                    {
                        chefName = chef.data["m_Name"].AsString;

                        if (existingNames.Contains(chefName))
                        {
                            continue; // This chef comes from OC2, so no need to backport...
                        }

                        // Add chef entry to AvatarDirectoryData array
                        var avatar = ValueBuilder.DefaultValueFieldFromArrayTemplate(avatars);
                        avatar["m_FileID"].AsInt = 0;
                        avatar["m_PathID"].AsLong = chef.id;
                        avatars.Children.Add(avatar);

                        // Make a new asset of type "ChefAvatarData" and convert fields from AYCE
                        {
                            var asset = ConvertAsset(assets, chef);
                            if (asset == null)
                            {
                                throw new Exception("ChefAvatarData convert returned null");
                            }
                            assetsReplacers.Add(asset);
                        }

                        // Convert dependencies
                        foreach (var dep in chef.dependencies)
                        {
                            if (convertedIDs.Contains(dep.id))
                            {
                                continue;
                            }
                            convertedIDs.Add(dep.id);

                            var asset = ConvertAsset(assets, dep);
                            if (asset == null)
                            {
                                continue;
                            }

                            assetsReplacers.Add(asset);
                        }
                        // OC2Modding.Log.LogInfo($"Successfully converted '{chefName}' and it's dependencies");
                    }
                    catch (Exception e)
                    {
                        OC2Modding.Log.LogError($"Failed to convert '{chefName}': {e}");
                    }
                }

                OC2Modding.Log.LogInfo("Finished converting assets");

                assetsReplacers.Add(new AssetsReplacerFromMemory(assets.file, mainAvatarDirectoryInfo, mainAvatarDirectory));
                bundleReplacers.Add(new BundleReplacerFromAssets(assets.name, null, assets.file, assetsReplacers));

                using (AssetsFileWriter writer = new AssetsFileWriter(outBundlePath))
                {
                    bundle.file.Write(writer, bundleReplacers);
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed make new bundle '{outBundlePath}' from base '{inBundlePath}':\n {e}");
            }
        }

        private static AssetsReplacerFromMemory ConvertAsset(AssetsFileInstance assets, AssetData data)
        {
            // OC2Modding.Log.LogInfo($"Converting {data.id} type={data.type}");

            AssetTypeValueField converted;

            switch (data.type)
            {
                case AssetClassID.MonoBehaviour:
                {
                    converted = ConvertMB(assets, data);
                    if (converted == null)
                    {
                        return null;
                    }

                    break;
                }
                case AssetClassID.MonoScript:
                {
                    converted = DefaultAssetConverter(assets, data);
                    break;
                }
                case AssetClassID.GameObject:
                {
                    converted = DefaultAssetConverter(assets, data);
                    return null; 
                    // break; // TODO: this crashes
                }
                case AssetClassID.Sprite:
                {
                    converted = DefaultAssetConverter(assets, data);
                    break;
                }
                case AssetClassID.AnimationClip:
                {
                    converted = DefaultAssetConverter(assets, data);
                    break;
                }
                default:
                {
                    throw new Exception($"Error: Cannot convert {data.type} because no converter is implemented");
                }
            }

            var scriptIndex = (ushort)(data.className == "" ? 0xffff : MonoBehaviorTypes[data.className].index);

            return new AssetsReplacerFromMemory(data.id, (int)data.type, scriptIndex, converted.WriteToByteArray());
        }

        private static AssetTypeValueField DefaultAssetConverter(AssetsFileInstance assets, AssetData data)
        {
            var newBaseField = Manager.CreateValueBaseField(assets, (int)data.type);

            var keys = new List<string>();
            FlattenAssetKeys(data.data, "", ref keys);
            foreach (var key in keys)
            {
                newBaseField[key].Value = data.data[key].Value;
            }

            return newBaseField;
        }

        private static void FlattenAssetKeys(AssetTypeValueField data, string currentKey, ref List<string> keys)
        {
            if (data.FieldName != "Base")
            {
                if (currentKey == "")
                {
                    currentKey += data.FieldName;
                }
                else
                {
                    currentKey += "." + data.FieldName;
                }
            }

            if (data.Children.Count == 0)
            {
                keys.Add(currentKey);
                return;
            }

            foreach (var key in data.Children)
            {
                FlattenAssetKeys(key, currentKey, ref keys);
            }
        }

        private static AssetTypeValueField ConvertMB(AssetsFileInstance assets, AssetData data)
        {
            if (!MonoBehaviorTypes.ContainsKey(data.className))
            {
                var helper = AssetHelper.GetAssetsFileScriptInfos(Manager, assets);
                throw new Exception($"Error: Cannot convert MonoBehavior '{data.className}' because no converter is implemented");
            }

            var typeData = MonoBehaviorTypes[data.className];

            // OC2Modding.Log.LogInfo($"Converting {data.type} id={data.id}, scriptType={typeData.index}, name={typeData.name}");

            var newBaseField = ValueBuilder.DefaultValueFieldFromTemplate(typeData.template);

            newBaseField["m_Script.m_FileID"].AsInt = typeData.fileID;
            newBaseField["m_Script.m_PathID"].AsLong = typeData.pathID;

            switch (typeData.name)
            {
                case "ChefAvatarData":
                {
                    return ConvertMBChefAvatarData(data.data, newBaseField);
                }
                case "GameInputConfigData":
                {
                    return null;
                }
                case "AudioDirectoryData":
                {
                    return null;
                }
                case "DLCFrontendData":
                {
                    return null;
                }
                default:
                {
                    throw new Exception($"Error: Cannot convert MonoBehavior '{typeData.name}' (idx={typeData.index}) because no converter is implemented");
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

            // ForDlc left at 0;0

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
