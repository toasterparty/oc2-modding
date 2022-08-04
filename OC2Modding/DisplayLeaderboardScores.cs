using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using Team17.Online;
using System.Reflection;

namespace OC2Modding
{
    public class DisplayLeaderboardScores
    {
        private static ConfigEntry<bool> configDisplayLeaderboardScores;
        private static MyOnScreenDebugDisplay onScreenDebugDisplay;
        private static LeaderboardDisplay leaderboardDisplay = null;
        private static Dictionary<int, bool> enabledNodes;

        public static void Awake()
        {
            /* Setup Configuration */
            configDisplayLeaderboardScores = OC2Modding.configFile.Bind(
                "QualityOfLife", // Config Category
                "DisplayLeaderboardScores", // Config key name
                true, // Default Config value
                "Set to true to show the top 5 leaderboard scores when previewing a level" // Friendly description
            );

            if (!configDisplayLeaderboardScores.Value)
            {
                return;
            }

            /* Setup */
            onScreenDebugDisplay = new MyOnScreenDebugDisplay();
            enabledNodes = new Dictionary<int, bool>();
            onScreenDebugDisplay.Awake();
            onScreenDebugDisplay.AddDisplay(new LeaderboardDisplay());

            downloadLeaderboardFile();

            /* Inject Mod */
            Harmony.CreateAndPatchAll(typeof(DisplayLeaderboardScores));
            Harmony.CreateAndPatchAll(typeof(CurrentDLC.DLCMenu));
            Harmony.CreateAndPatchAll(typeof(CurrentDLC.CampaignMenu));
        }

        public static void Update()
        {
            if (!configDisplayLeaderboardScores.Value)
            {
                return;
            }

            onScreenDebugDisplay.Update();
        }

        public static void OnGUI()
        {
            if (!configDisplayLeaderboardScores.Value)
            {
                return;
            }

            onScreenDebugDisplay.OnGUI();
        }

        private static void downloadLeaderboardFile()
        {
            Process downloadProcess = new Process();

            try
            {
                downloadProcess.StartInfo.UseShellExecute = false;
                downloadProcess.StartInfo.FileName = ".\\curl\\curl.exe";
                downloadProcess.StartInfo.Arguments = "https://overcooked.greeny.dev/assets/data/data.csv --output leaderboard_scores.csv";
                downloadProcess.StartInfo.CreateNoWindow = true;
                downloadProcess.Start();
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to download leaderboard-scores.csv: {e.ToString()}");
            }
        }

        private static List<int> getScoresFromLeaderboard(string game, string dlc, string level, uint playerCount, uint numScores)
        {
            List<int> scores = new List<int>();

            try
            {
                IEnumerable<string> lines = File.ReadAllLines("leaderboard_scores.csv");
                bool firstLine = true;
                foreach (string line in lines)
                {
                    if (firstLine)
                    {
                        firstLine = false;
                        continue;
                    }

                    try
                    {
                        if (scores.Count >= numScores)
                        {
                            break; // we found all the scores we need
                        }

                        string sanitaryLine = line.Replace("Sun’s Out,", "Sun's Out");
                        string[] values = sanitaryLine.Split(',');
                        if (UInt32.Parse(values[3]) != playerCount || values[2].Replace("\"", "") != level || values[0].Replace("\"", "") != game || values[1].Replace("\"", "") != dlc)
                        {
                            continue; // not the level (or player count) we are looking for
                        }

                        int place = Int32.Parse(values[4]);
                        if (place > numScores)
                        {
                            continue; // The score isn't good enough to return
                        }

                        // Return this score
                        int score = Int32.Parse(values[6]);
                        scores.Add(score);
                    }
                    catch (Exception e)
                    {
                        OC2Modding.Log.LogWarning($"Failed to parse line:\n'{line}'\n{e}");
                    }
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to lookup scores in leaderboard-scores.csv:\n{e}");
                return scores;
            }

            if (numScores > 0 && scores.Count == 0)
            {
                OC2Modding.Log.LogWarning($"Didn't find {dlc} {level} ({playerCount} Player) in leaderboard-scores.csv");
            }

            scores.Sort();
            scores.Reverse();

            return scores;
        }

        // TODO: this should be it's own file
        [HarmonyPatch]
        private static class CurrentDLC
        {
            private static int m_DLCID = 0;

            public static int GetCurrentDLCID()
            {
                return m_DLCID;
            }

            public static string DLCToString(int dlc)
            {
                switch (dlc)
                {
                    case -1: return "Story";
                    case 2: return "Surf 'n' Turf";
                    case 3: return "Seasonal";
                    case 5: return "Campfire Cook Off";
                    case 7: return "Night of the Hangry Horde";
                    case 8: return "Carnival of Chaos";
                    default:
                        {
                            OC2Modding.Log.LogError($"Unexpected DLC ID #{dlc}");
                            return "";
                        }
                }
            }

            public static class CampaignMenu
            {
                private static IEnumerable<MethodBase> TargetMethods()
                {
                    yield return AccessTools.Method(typeof(FrontendCampaignTabOptions), nameof(FrontendCampaignTabOptions.OnContinueGameClicked));
                    yield return AccessTools.Method(typeof(FrontendCampaignTabOptions), nameof(FrontendCampaignTabOptions.OnNewGameClicked));
                    yield return AccessTools.Method(typeof(FrontendCampaignTabOptions), nameof(FrontendCampaignTabOptions.OnLoadGameClicked));
                }

                [HarmonyPrefix]
                private static void LoadCampaign()
                {
                    m_DLCID = -1;
                    OC2Modding.Log.LogInfo($"DLC set to {DLCToString(m_DLCID)}");
                }
            }

            public static class DLCMenu
            {
                private static IEnumerable<MethodBase> TargetMethods()
                {
                    yield return AccessTools.Method(typeof(FrontendDLCMenu), nameof(FrontendDLCMenu.OnContinueGameButtonPressed));
                    yield return AccessTools.Method(typeof(FrontendDLCMenu), nameof(FrontendDLCMenu.OnNewGameButtonPressed));
                    yield return AccessTools.Method(typeof(FrontendDLCMenu), nameof(FrontendDLCMenu.OnLoadGameButtonPressed));
                }

                [HarmonyPrefix]
                private static void LoadDLC(ref DLCFrontendData dlcData)
                {
                    m_DLCID = dlcData.m_DLCID;
                    OC2Modding.Log.LogInfo($"DLC set to {m_DLCID}:{DLCToString(m_DLCID)}");
                }
            }
        }

        private static string ToStringHelper(int levelId)
        {
            return ToStringHelper(levelId, new int[] { });
        }

        private static string ToStringHelper(int levelId, int[] worldCapacities)
        {
            int x = levelId;
            int world = 0;

            for (world = 0; world < worldCapacities.Length; world++)
            {
                if (x < worldCapacities[world])
                {
                    return $"{world + 1}-{x + 1}";
                }

                x -= worldCapacities[world];
            }

            return $"{world + 1}-{x + 1}";
        }

        private static List<int> getScoresFromLeaderboard(int DLCID, int levelId, uint playerCount)
        {
            // TOOD: build cache and return answers from it on Awake

            string dlc = CurrentDLC.DLCToString(DLCID);

            string level = "";

            if (dlc == "Story")
            {
                if (levelId == 0)
                {
                    level = "Tutorial";
                }
                else if (levelId > 36)
                {
                    level = $"Kevin {(levelId - 36)}";
                }
                else
                {
                    level = ToStringHelper(
                        levelId - 1,
                        new int[] { 6, 6, 6, 6, 6, 6 }
                    );
                }
            }
            else if (dlc == "Campfire Cook Off" || dlc == "Carnival of Chaos" || dlc == "Surf 'n Turf")
            {
                if (levelId > 12)
                {
                    level = $"Kevin {(levelId - 12) + 1}";
                }
                else
                {
                    level = ToStringHelper(
                        levelId,
                        new int[] { 4, 4, 4 }
                    );
                }
            }
            else if (dlc == "Night of the Hangry Horde")
            {
                if (levelId < 9)
                {
                    level = ToStringHelper(
                        levelId,
                        new int[] { 3, 3, 3 }
                    );
                }
                else if (levelId >= 9 && levelId <= 11)
                {
                    level = $"Kevin {(levelId - 9) + 1}";
                }
                else
                {
                    return new List<int>(); // Horde Levels
                }
            }
            else if (dlc == "Seasonal")
            {
                if (levelId >= 0 && levelId <= 4)
                {
                    dlc = "Christmas";
                    level = ToStringHelper(levelId);
                }
                else if (levelId >= 5 && levelId <= 11)
                {
                    dlc = "Chinese New Year";
                    level = ToStringHelper(levelId - 5);
                }
                else if (levelId >= 12 && levelId <= 16)
                {
                    dlc = "Winter Wonderland";
                    level = ToStringHelper(levelId - 12);

                    if (level == "1-2" || level == "1-4")
                    {
                        return new List<int>(); // Horde Levels
                    }
                }
                else if (levelId >= 17 && levelId <= 21)
                {
                    level = ToStringHelper(levelId - 17);
                    dlc = "Spring Festival";
                }
                else if (levelId >= 22 && levelId <= 26)
                {
                    level = ToStringHelper(levelId - 22);
                    dlc = "Sun's Out Buns Out";
                }
                else if (levelId >= 27 && levelId <= 31)
                {
                    level = ToStringHelper(levelId - 27);
                    dlc = "Moon Harvest";
                }
                else
                {
                    dlc = "";
                }
            }

            if (dlc == "" || level == "" || playerCount < 1 || playerCount > 4)
            {
                OC2Modding.Log.LogWarning($"Failed to parse level for leaderboard display: dlc={DLCID}, levelId={levelId}, playerCount={playerCount}");
                return new List<int>();
            }

            OC2Modding.Log.LogInfo($"{dlc} {level} ({playerCount} Player)");

            return getScoresFromLeaderboard("Overcooked 2", dlc, level, playerCount, 5);
        }

        private static List<int> getScoresFromLeaderboard(int levelId)
        {
            uint playerCount = (uint)ClientUserSystem.m_Users.Count;

            int DLCID = CurrentDLC.GetCurrentDLCID();

            return getScoresFromLeaderboard(DLCID, levelId, playerCount);
        }

        private static string numToRank(int num)
        {
            string result = num.ToString();

            switch (result[result.Length - 1])
            {
                case '1':
                    {
                        result += "st";
                        break;
                    }
                case '2':
                    {
                        result += "nd";
                        break;
                    }
                case '3':
                    {
                        result += "rd";
                        break;
                    }
                default:
                    {
                        result += "th";
                        break;
                    }
            }

            result += " Place: ";

            return result;
        }

        private static string leaderboardTextFromLevelId(int levelId)
        {
            string result = string.Empty;

            List<int> scores = getScoresFromLeaderboard(levelId);

            int i = 1;
            foreach (int score in scores)
            {
                result += $"{numToRank(i++)}{score.ToString()}\n";
            }

            return result;
        }

        /* Adapted from OnScreenDebugDisplay */
        private class MyOnScreenDebugDisplay
        {
            public void AddDisplay(DebugDisplay display)
            {
                if (display != null)
                {
                    display.OnSetUp();
                    this.m_Displays.Add(display);
                }
            }

            public void RemoveDisplay(DebugDisplay display)
            {
                if (display != null)
                {
                    this.m_Displays.Remove(display);
                }
            }

            public void Awake()
            {
                this.m_Displays = new List<DebugDisplay>();
                this.m_GUIStyle = new GUIStyle();
                this.m_GUIStyle.alignment = TextAnchor.UpperRight;
                this.m_GUIStyle.fontSize = (int)((float)Screen.height * 0.03f);
                this.m_GUIStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            public void Update()
            {
                for (int i = 0; i < this.m_Displays.Count; i++)
                {
                    this.m_Displays[i].OnUpdate();
                }
            }

            public void OnGUI()
            {
                Rect rect = new Rect(0f, (float)Screen.height * 0.7f, (float)Screen.width * 0.99f, (float)this.m_GUIStyle.fontSize);
                for (int i = 0; i < this.m_Displays.Count; i++)
                {
                    this.m_Displays[i].OnDraw(ref rect, this.m_GUIStyle);
                }
            }

            private List<DebugDisplay> m_Displays;
            private GUIStyle m_GUIStyle;
        }

        /* Directly Copied from Code */
        private struct UIInstance
        {
            public void Reset()
            {
                this.m_uiInstance = null;
                this.m_prefab = null;
            }

            public WorldMapLevelIconUI m_uiInstance;
            public WorldMapLevelIconUI m_prefab;
        }

        private static void clearLeaderboardDisplay()
        {
            if (leaderboardDisplay != null)
            {
                onScreenDebugDisplay.RemoveDisplay(leaderboardDisplay);
                leaderboardDisplay.OnDestroy();
                leaderboardDisplay = null;
            }
            enabledNodes.Clear();
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            clearLeaderboardDisplay();
        }

        [HarmonyPatch(typeof(ClientPortalMapNode), nameof(ClientPortalMapNode.UpdateSynchronising))]
        [HarmonyPostfix]
        private static void UpdateSynchronising(ref UIInstance ___m_uiInstance, ref bool ___m_inSelectable)
        {
            if (___m_uiInstance.m_uiInstance == null)
            {
                return;
            }

            int hash = ___m_uiInstance.m_uiInstance.GetHashCode();
            enabledNodes[hash] = ___m_inSelectable;
        }

        [HarmonyPatch(typeof(ClientLevelPortalMapNode), "SetupUI")]
        [HarmonyPrefix]
        private static void SetupUI(ref LevelPortalMapNode ___m_baseLevelPortalMapNode, ref WorldMapLevelIconUI _ui)
        {
            if (___m_baseLevelPortalMapNode.m_sceneDirectoryEntry == null)
            {
                return;
            }

            int hash = _ui.GetHashCode();
            if (!enabledNodes.ContainsKey(hash))
            {
                return;
            }

            int levelId = ___m_baseLevelPortalMapNode.m_sceneProgress.LevelId;

            bool enabled = enabledNodes[hash];
            if (enabled)
            {
                if (leaderboardDisplay == null)
                {
                    leaderboardDisplay = new LeaderboardDisplay();
                    onScreenDebugDisplay.AddDisplay(leaderboardDisplay);
                }

                leaderboardDisplay.m_Text = leaderboardTextFromLevelId(levelId);
            }
            else
            {
                if (leaderboardDisplay != null)
                {
                    onScreenDebugDisplay.RemoveDisplay(leaderboardDisplay);
                    leaderboardDisplay.OnDestroy();
                    leaderboardDisplay = null;
                }
            }
        }

        public class LeaderboardDisplay : DebugDisplay
        {
            public override void OnSetUp()
            {
            }

            public override void OnUpdate()
            {
            }

            public override void OnDraw(ref Rect rect, GUIStyle style)
            {
                base.DrawText(ref rect, style, this.m_Text);
            }

            public string m_Text = string.Empty;
        }
    }
}
