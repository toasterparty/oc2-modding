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

        [HarmonyPatch(typeof(WorldMapFlowController), nameof(WorldMapFlowController.GetSceneDirectory))]
        [HarmonyPostfix] 
        private static void GetSceneDirectory(ref SceneDirectoryData ___m_sceneDirectory, ref SceneDirectoryData __result)
        {
            if (OC2Config.CustomLevelOrder == null)
            {
                return;
            }

            if (!OC2Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return;
            }

            Dictionary<int, int> levelOrder = OC2Config.CustomLevelOrder["Story"];

            SceneDirectoryData.SceneDirectoryEntry[] originalScenes = (SceneDirectoryData.SceneDirectoryEntry[])___m_sceneDirectory.Scenes.Clone();

            for (int levelId = 0; levelId < originalScenes.Length; levelId++)
            {
                if (!levelOrder.ContainsKey(levelId))
                {
                    continue;
                }

                SceneDirectoryData.SceneDirectoryEntry originalScene = originalScenes[levelId];
                SceneDirectoryData.SceneDirectoryEntry newScene = originalScenes[levelOrder[levelId]];
                newScene.LevelChainEnd = originalScene.LevelChainEnd;
                newScene.IsHidden = originalScene.IsHidden;
                newScene.StarCost = originalScene.StarCost;

                ___m_sceneDirectory.Scenes[levelId] = newScene;
            }

            __result = ___m_sceneDirectory;
        }
    }
}
