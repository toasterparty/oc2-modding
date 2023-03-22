using System;
using System.Linq;
using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace OC2Modding
{
    public struct AssetData
    {
        public AssetFileInfo info;
        public string className;
        public int scriptIndex;
        public AssetTypeValueField baseField;
    };
    
    public class BundleHelper
    {
        private struct BundleData
        {
            public BundleFileInstance bundle;
            public AssetsFileInstance assets;
        };

        private struct MonoBehaviourInfo
        {
            public string name;
            public int index;
            public int fileID;
            public long pathID;
        };

        private AssetsManager Manager;
        private List<BundleData> Bundles;

        private Dictionary<string, MonoBehaviourInfo> MonoBehaviourInfos = new Dictionary<string, MonoBehaviourInfo>();

        public static void CopyAsset(ref AssetTypeValueField toData, ref AssetTypeValueField fromData, ref BundleHelper bundleHelper)
        {
            var keys = new List<string>();
            FlattenAssetKeys(fromData, "", ref keys);
            foreach (var key in keys)
            {
                if (
                        key.EndsWith("m_FileID") &&
                        bundleHelper != null &&
                        fromData[key].Value.AsInt != 0 &&
                        !bundleHelper.ContainsID(fromData[key.Replace("m_FileID", "m_PathID")].Value.AsLong) // TODO check if this is in the list of assets to convert
                    )
                {
                    // In all you can eat, this file belonged to a dependency bundle, however we write these assets to the main bundle for oc2
                    OC2Modding.Log.LogWarning("adjust m_FileID");
                    toData[key].Value.AsInt = 0;
                }
                else
                {
                    toData[key].Value = fromData[key].Value;
                }
            }
        }

        private static void FlattenAssetKeys(AssetTypeValueField field, string currentKey, ref List<string> keys)
        {
            if (field.FieldName != "Base")
            {
                if (currentKey == "")
                {
                    currentKey += field.FieldName;
                }
                else
                {
                    currentKey += "." + field.FieldName;
                }
            }

            if (field.Children == null || field.Children.Count == 0)
            {
                keys.Add(currentKey);
                return;
            }

            foreach (var key in field.Children)
            {
                FlattenAssetKeys(key, currentKey, ref keys);
            }
        }

        public BundleHelper(string managedPath = null, string classDataTpkPath = null)
        {
            Bundles = new List<BundleData>();
            Manager = new AssetsManager();
            if (Manager == null)
            {
                throw new Exception($"Failed to init AssetsManager");
            }

            if (classDataTpkPath != null)
            {
                Manager.LoadClassPackage(classDataTpkPath);
            }
        }

        /* The first bundle you call is the "Main Bundle", further calls are used to find dependencies */
        public void LoadBundle(string bundlePath)
        {
            var bundleData = GetBundleData(bundlePath);
            Bundles.Add(bundleData);
            OC2Modding.Log.LogInfo($"Loaded {bundlePath}");
        }

        private BundleData GetBundleData(string bundlePath)
        {
            var bundle = Manager.LoadBundleFile(bundlePath);
            if (bundle == null)
            {
                throw new Exception($"Failed to load bundle file: {bundlePath}");
            }
            
            var assets = Manager.LoadAssetsFileFromBundle(bundle, 0);
            if (assets == null || assets.file == null)
            {
                throw new Exception($"Failed to load assets file from {bundlePath}");
            }

            return new BundleData {
                bundle = bundle,
                assets = assets,
            };
        }

        private BundleData MainBundleData()
        {
            if (Bundles.Count <= 0)
            {
                throw new Exception("Tried to get MainBundle before any were loaded");
            }

            return Bundles[0];
        }

        public bool ContainsID(long id)
        {
            foreach (var bundle in Bundles)
            {
                if (bundle.assets.file.GetAssetInfo(id) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public AssetFileInfo GetAssetInfo(long id)
        {
            return this.GetAssetsInstance(id).file.GetAssetInfo(id);
        }

        public AssetClassID GetAssetType(long id)
        {
            return (AssetClassID)this.GetAssetInfo(id).TypeId;
        }

        private AssetsFileInstance GetAssetsInstance(long id)
        {
            foreach (var bundle in Bundles)
            {
                if (bundle.assets.file.GetAssetInfo(id) != null)
                {
                    return bundle.assets;
                }
            }

            throw new Exception($"{id} wasn't found in any of {Bundles.Count} loaded bundle(s)");
        }

        public string GetClassName(long id)
        {
            var info = this.GetAssetInfo(id);

            if (info.TypeId != (int)AssetClassID.MonoBehaviour)
            {
                return ""; // empty string represents non-monobehavior
            }

            var baseField = this.GetBaseField(id);
            var scriptID = baseField["m_Script.m_PathID"].AsLong;
            var scriptBaseField = this.GetBaseField(scriptID);
            return scriptBaseField["m_ClassName"].AsString;
        }

        public AssetTypeValueField GetBaseField(long id)
        {
            var assets = this.GetAssetsInstance(id);
            var info = assets.file.GetAssetInfo(id);
            return Manager.GetBaseField(assets, info);
        }

        public AssetData GetAssetData(long id)
        {
            var className = this.GetClassName(id);
            var scriptIndex = this.GetScriptIndex(className);

            return new AssetData {
                info = this.GetAssetInfo(id),
                className = className,
                scriptIndex = scriptIndex,
                baseField = this.GetBaseField(id),
            };
        }

        public List<long> GetDependencies(long id)
        {
            var deps = new List<long>();
            var baseField = this.GetBaseField(id);
            var bundleHelper = this;
            CollectAssetDependencies(ref bundleHelper, baseField, ref deps);
            return deps;
        }

        /* recursive tree search for asset ids referenced by this one (static because I'm paranoid of modern languages) */
        private static void CollectAssetDependencies(ref BundleHelper bundleHelper, AssetTypeValueField field, ref List<long> deps)
        {
            if (field.FieldName == "m_PathID")
            {
                var depID = field.AsLong;
                if (depID == 0)
                {
                    return; // "null" reference
                }

                if (deps.Contains(depID))
                {
                    return; // avoid infinite recursion
                }

                try
                {
                    var depData = bundleHelper.GetBaseField(depID);
                    deps.Add(depID);

                    // collect all dependencies from depenent asset
                    CollectAssetDependencies(ref bundleHelper, field, ref deps);
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"{e}");
                }

                return; // end of tree branch (id)
            }

            if (field.Children == null)
            {
                return; // end of tree branch (non-id)
            }

            foreach (var child in field.Children)
            {
                // continue searching this asset
                CollectAssetDependencies(ref bundleHelper, child, ref deps);
            }
        }

        public AssetsReplacerFromMemory FileReplacer(long id, AssetTypeValueField baseField)
        {
            var file = this.MainBundleData().assets.file;
            var info = this.GetAssetInfo(id);
            return new AssetsReplacerFromMemory(file, info, baseField);
        }

        public void ModifyBundle(List<AssetsReplacer> assetsReplacers, string outPath = null)
        {
            var bundleData = this.MainBundleData();
            
            if (outPath == null)
            {
                outPath = bundleData.assets.path;
            }
            
            OC2Modding.Log.LogInfo($"Writing modified bundle to {outPath}");

            var bundleReplacers = new List<BundleReplacer>();
            bundleReplacers.Add(new BundleReplacerFromAssets(bundleData.assets.name, null, bundleData.assets.file, assetsReplacers));
            using (AssetsFileWriter writer = new AssetsFileWriter(outPath))
            {
                bundleData.bundle.file.Write(writer, bundleReplacers);
            }
        }

        public ushort GetScriptIndex(string className)
        {
            if (className == null || className == "")
            {
                return 0xffff;
            }

            return (ushort)this.GetMonoBehaviourInfo(className).index;
        }

        private MonoBehaviourInfo GetMonoBehaviourInfo(string className)
        {
            if (className == null || className == "")
            {
                throw new Exception("Expected non-empty className when looking up monobehavior info");
            }

            if (!MonoBehaviourInfos.ContainsKey(className))
            {
                ReadMonoBehaviourInfo(className);
            }

            return MonoBehaviourInfos[className];
        }

        /**
         * Collect information regarding this MonoBheavior in the main bundle for future use
         */
        private void ReadMonoBehaviourInfo(string className)
        {
            var assets = this.MainBundleData().assets;

            var scriptTypeInfos = AssetHelper.GetAssetsFileScriptInfos(Manager, assets);
            var scriptIndex = scriptTypeInfos.Values.ToList().FindIndex(x => x.ClassName == className);
            if (scriptIndex == -1)
            {
                throw new Exception($"Failed to find class of type '{className}'");
            }

            var monoScriptPPtr = assets.file.Metadata.ScriptTypes[scriptIndex];

            var typeData = new MonoBehaviourInfo {
                name = className,
                index = scriptIndex,
                fileID = monoScriptPPtr.FileId,
                pathID = monoScriptPPtr.PathId,
            };
    
            MonoBehaviourInfos[className] = typeData;

            // OC2Modding.Log.LogInfo($"{className} is index={typeData.index}"); 
        }

        public AssetTypeValueField CreateBaseField(string className)
        {
            var info = GetMonoBehaviourInfo(className);
            var assets = this.MainBundleData().assets;
            
            var baseField = Manager.CreateValueBaseField(assets, (int)AssetClassID.MonoBehaviour, (ushort)info.index);
            baseField["m_Script.m_FileID"].AsInt = info.fileID;
            baseField["m_Script.m_PathID"].AsLong = info.pathID;

            return baseField;
        }

        public AssetTypeValueField CreateBaseField(AssetClassID type)
        {
            if (type == AssetClassID.MonoBehaviour)
            {
                throw new Exception("cannot create basefield for mono using this method");
            }

            return Manager.CreateValueBaseField(this.MainBundleData().assets, (int)type);
        }

        public void UnloadAll()
        {
            Bundles.Clear();
            Manager.UnloadAll();
        }

        ~BundleHelper()
        {
            this.UnloadAll();
        }
    } 
}
