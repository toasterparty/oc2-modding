using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

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

            /* Setup */
            onScreenDebugDisplay = new MyOnScreenDebugDisplay();
            enabledNodes = new Dictionary<int, bool>();
            onScreenDebugDisplay.Awake();
            onScreenDebugDisplay.AddDisplay(new LeaderboardDisplay());

            downloadLeaderboardFile();

            /* Inject Mod */
            if (configDisplayLeaderboardScores.Value)
            {
                Harmony.CreateAndPatchAll(typeof(DisplayLeaderboardScores));
            }
        }

        public static void Update()
        {
            onScreenDebugDisplay.Update();
        }

        public static void OnGUI()
        {
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
                foreach (string line in lines)
                {
                    try
                    {
                        string[] values = line.Split(',');
                        if (values[0] != game || values[1] != dlc || values[2] != level || UInt32.Parse(values[3]) != playerCount)
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

                        if (scores.Count >= numScores)
                        {
                            break; // we found all the scores we need
                        }
                    }
                    catch
                    {
                        // pass
                    }
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to lookup scores in leaderboard-scores.csv: {e}");
                return scores;
            }

            scores.Sort();
            scores.Reverse();

            return scores;
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
                this.m_GUIStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);
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
                Rect rect = new Rect(0f, (float)Screen.height * 0.7f, (float)Screen.width * 0.95f, (float)this.m_GUIStyle.fontSize);
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

                leaderboardDisplay.m_Text = "my cool\nleaderboard text\nisn't that neat";
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
