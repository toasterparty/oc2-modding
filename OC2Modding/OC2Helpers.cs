using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Team17.Online;

namespace OC2Modding
{
    public static class OC2Helpers
    {
        public static void Awake()
        {
            downloadLeaderboardFile();
            buildCache();

            Harmony.CreateAndPatchAll(typeof(CurrentDLC.DLCMenu));
        }

        public static int DLCFromWorld(SceneDirectoryData.World world)
        {
            return DLCFromWorld((int)world);
        }

        public static int DLCFromWorld(int world)
        {
            switch (world)
            {
                case 0  : return -1; // Tutorial
                case 1  : return -1; // One
                case 2  : return -1; // Two
                case 3  : return -1; // Three
                case 4  : return -1; // Four
                case 5  : return -1; // Five
                case 6  : return -1; // Six
                case 7  : return -1; // Seven
                case 8  : return  2; // DLC2_One
                case 9  : return  2; // DLC2_Two
                case 10 : return  2; // DLC2_Three
                case 11 : return  3; // DLC3_One
                case 12 : return  3; // DLC4_One
                case 13 : return  5; // DLC5_One
                case 14 : return  5; // DLC5_Two
                case 15 : return  5; // DLC5_Three
                case 16 : return  7; // DLC7_One
                case 17 : return  7; // DLC7_Two
                case 18 : return  7; // DLC7_Three
                case 19 : return  8; // DLC8_One
                case 20 : return  8; // DLC8_Two
                case 21 : return  8; // DLC8_Three
                case 22 : return  3; // DLC9_One
                case 23 : return  3; // DLC10_One
                case 24 : return  3; // DLC11_One
                case 25 : return  3; // DLC13_One
                default:
                    {
                        OC2Modding.Log.LogError($"Unexpected world #{world}");
                        return -1;
                    }
            }
        }

        public static int GetCurrentDLCID()
        {
            return CurrentDLC.m_DLCID;
        }

        public static int GetCurrentPlayerCount()
        {
            return ClientUserSystem.m_Users.Count;
        }

        /* Helpers for the helpers start here */

        [HarmonyPatch]
        private static class CurrentDLC
        {
            public static int m_DLCID = 0;

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

        private struct LeaderboardScoresKey
        {
            public int DLCID;
            public int levelId;
            public int playerCount;
        }
        private static Dictionary<LeaderboardScoresKey, int> leaderboardScores = null;
        private static IEnumerable<string> lines = null;

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

        public static int getScoresFromLeaderboard(int DLCID, int levelId, int playerCount)
        {
            LeaderboardScoresKey key;
            key.DLCID = DLCID;
            key.levelId = levelId;
            key.playerCount = playerCount;

            if (leaderboardScores == null)
            {
                leaderboardScores = new Dictionary<LeaderboardScoresKey, int>();
            }

            if (leaderboardScores.ContainsKey(key))
            {
                return leaderboardScores[key];
            }

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
            else if (dlc == "Campfire Cook Off" || dlc == "Carnival of Chaos" || dlc == "Surf 'n' Turf")
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
                    return 0; // Horde Levels
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
                        return 0; // Horde Levels
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
                OC2Modding.Log.LogWarning($"\tdlc={dlc}, level={level}");
                return 0;
            }

            List<int> result = getScoresFromLeaderboard("Overcooked 2", dlc, level, playerCount, 1);
            if (result.Count == 0)
            {
                return 0;
            }

            leaderboardScores[key] = result[0];
            return result[0];
        }

        private static List<int> getScoresFromLeaderboard(string game, string dlc, string level, int playerCount, uint numScores)
        {
            List<int> scores = new List<int>();

            try
            {
                if (lines == null)
                {
                    lines = File.ReadAllLines("leaderboard_scores.csv");
                }

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
                        
                        int commaCount = 0;
                        int score = 0;
                        bool inTeamName = false;
                        
                        for (int i = 0; i < sanitaryLine.Length-1; i++)
                        {
                            if(sanitaryLine[i] == '"' && commaCount == 5 && !inTeamName)
                            {
                                inTeamName = true;
                            }
                            else if (sanitaryLine[i] == '"' && inTeamName)
                            {
                                var substrings = sanitaryLine.Substring(i+2).Split(',');
                                score = Int32.Parse(substrings[0]);
                            }
                            else if (sanitaryLine[i] == ',')
                            {
                                commaCount++;
                            }
                        }

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

                        if (score == 0)
                        {
                            score = Int32.Parse(values[6]);
                        }

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

        private static void buildCache()
        {
            for (int playerCount = 1; playerCount <= 4; playerCount++)
            {
                foreach (int DLCID in new int[] { -1, 2, 3, 5, 7, 8})
                {
                    for (int i = 0; i < 100; i++)
                    {
                        int result = getScoresFromLeaderboard(DLCID, i, playerCount);
                        if (result == 0)
                        {
                            break;
                        }
                    }
                }   
            }

            lines = null;
        }
    }
}