using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class CustomLevelOrder
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomLevelOrder));
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsLevelUnlocked))]
        [HarmonyPrefix]
        private static bool IsLevelUnlocked(ref int _levelIndex, ref bool __result)
        {
            __result = true;
            return false;
        }

        private static Dictionary<OC2Config.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry> replacementScenes = null;

        [HarmonyPatch(typeof(T17FrontendFlow), nameof(T17FrontendFlow.Awake))]
        [HarmonyPostfix] 
        private static void T17FrontendFlow_Awake()
        {
            OC2Modding.Log.LogInfo("T17FrontendFlow_Awake");
            if (replacementScenes != null)
            {
                return;
            }

            replacementScenes = new Dictionary<OC2Config.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry>();
            
            if (!OC2Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return; // No levels in Story are shuffled
            }

            foreach(KeyValuePair<int, OC2Config.DlcIdAndLevelId> kvp in OC2Config.CustomLevelOrder["Story"])
            {   
                SceneDirectoryData.SceneDirectoryEntry[] scenes = OC2Helpers.getScenesFromDLC(kvp.Value.Dlc);
                
                if (scenes == null)
                {
                    continue;
                }

                int levelId = kvp.Value.LevelId;
                if (levelId >= scenes.Length)
                {
                    OC2Modding.Log.LogError($"Out of range levelId={levelId}");
                    continue;
                }

                replacementScenes.Add(kvp.Value, scenes[levelId]);
            }
        }

        [HarmonyPatch(typeof(WorldMapFlowController), nameof(WorldMapFlowController.GetSceneDirectory))]
        [HarmonyPostfix] 
        private static void GetSceneDirectory(ref SceneDirectoryData ___m_sceneDirectory, ref SceneDirectoryData __result)
        {
            if (OC2Config.CustomLevelOrder == null)
            {
                return; // No levels are shuffled
            }

            if (!OC2Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return; // No levels in Story are shuffled
            }

            if (OC2Helpers.GetCurrentDLCID() != -1) {
                return; // We're not in Story
            }

            if (replacementScenes == null)
            {
                OC2Modding.Log.LogError("replacementScenes dict was null");
                return;
            }

            // TOOD: we are running this logic far more often than we need to

            Dictionary<int, OC2Config.DlcIdAndLevelId> levelOrder = OC2Config.CustomLevelOrder["Story"];

            SceneDirectoryData.SceneDirectoryEntry[] originalScenes = (SceneDirectoryData.SceneDirectoryEntry[])___m_sceneDirectory.Scenes.Clone();

            for (int levelId = 0; levelId < originalScenes.Length; levelId++)
            {
                if (!levelOrder.ContainsKey(levelId))
                {
                    continue;
                }

                SceneDirectoryData.SceneDirectoryEntry originalScene = originalScenes[levelId];
                OC2Config.DlcIdAndLevelId newDlcAndLevel = levelOrder[levelId];
                if (!replacementScenes.ContainsKey(newDlcAndLevel))
                {
                    OC2Modding.Log.LogError($"replacementScenes missing key {newDlcAndLevel.Dlc}:{newDlcAndLevel.LevelId}");
                    continue;
                }
                SceneDirectoryData.SceneDirectoryEntry newScene = replacementScenes[newDlcAndLevel];

                newScene.LevelChainEnd = originalScene.LevelChainEnd;
                newScene.IsHidden = originalScene.IsHidden;
                newScene.StarCost = originalScene.StarCost;

                ___m_sceneDirectory.Scenes[levelId] = newScene;
            }

            __result = ___m_sceneDirectory;
        }
    }
}
