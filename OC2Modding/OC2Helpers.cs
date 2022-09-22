using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using Team17.Online;
using UnityEngine;

namespace OC2Modding
{
    public static class OC2Helpers
    {
        private static Dictionary<LeaderboardScoresKey, int> leaderboardScores;

        public static void Awake()
        {
            /* Asynchronously download leaderboard file and populate memory structure with score */
            ThreadPool.QueueUserWorkItem((o) => BuildLeaderboardScores());

            Harmony.CreateAndPatchAll(typeof(CurrentDLC));
            Harmony.CreateAndPatchAll(typeof(CurrentDLC.DLCMenu));
            Harmony.CreateAndPatchAll(typeof(CurrentDLC.CampaignMenu));

            /* Register callback handler to cache game state */
            UserSystemUtils.OnServerChangedGameState = (GenericVoid<GameState, GameStateMessage.GameStatePayload>)Delegate.Combine(UserSystemUtils.OnServerChangedGameState, new GenericVoid<GameState, GameStateMessage.GameStatePayload>(OnServerChangedGameState));
        }
        
        public static bool IsEpicGames()
        {
            // at the time or writing, epic games is ver=22 and steam is ver=17
            return Team17.Online.OnlineMultiplayerConfig.CodeVersion >= 22;
        }

        private static GameState CachedGameState = GameState.NotSet;
        private static void OnServerChangedGameState(GameState state, GameStateMessage.GameStatePayload payload)
        {
            // GameLog.LogMessage($"{CachedGameState} -> {state}");
            CachedGameState = state;
        }

        public static bool IsHostPlayer()
        {
            IOnlinePlatformManager onlinePlatformManager = GameUtils.RequireManagerInterface<IOnlinePlatformManager>();
            if (onlinePlatformManager == null)
            {
                return true;
            }

            IOnlineMultiplayerSessionCoordinator coordinator = onlinePlatformManager.OnlineMultiplayerSessionCoordinator();
            if (coordinator == null)
            {
                return true;
            }

            if (coordinator.IsIdle())
            {
                return true;
            }

            // if (coordinator.Members().Length == 0)
            // {
            //     return true;
            // }

            return coordinator.IsHost();
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

        public static int getScoresFromLeaderboard(int dlcID, int levelId, int playerCount)
        {
            DlcAndLevel dlcAndLevel = getLevelName(dlcID, levelId);

            bool timerLevel = isLevelTimerLevel(dlcAndLevel.dlc, dlcAndLevel.level);
            // if (timerLevel)
            // {
            //     OC2Modding.Log.LogInfo($"{dlcAndLevel.dlc} {dlcAndLevel.level} is a timer level");
            // }

            LeaderboardScoresKey key = new LeaderboardScoresKey();
            key.game = (timerLevel && OC2Config.TimerAlwaysStarts) ? "All You Can Eat" : "Overcooked 2";
            key.DLCID = dlcID;
            key.level = dlcAndLevel.level;
            key.playerCount = (uint)playerCount;

            if (leaderboardScores.ContainsKey(key))
            {                
                // OC2Modding.Log.LogInfo($"{dlcID}|{dlcAndLevel.level}, id={levelId}, ({playerCount}-Player) [{key.game}] WR is {leaderboardScores[key]}");
                return leaderboardScores[key];
            }

            OC2Modding.Log.LogWarning($"{key.game}: {dlcAndLevel.dlc} {dlcAndLevel.level} ({key.playerCount} Player) does not have any scores submitted");
            return 0;
        }

        public static string DLCFromDLCID(int dlcId)
        {
            return CurrentDLC.DLCToString(dlcId);
        }

        public static int[] getDlcArray()
        {
            return new int[] { -1, 2, 3, 5, 7, 8 };
        }

        public static SceneDirectoryData.SceneDirectoryEntry[] getScenesFromDLC(int dlcId)
        {
            if (!getDlcArray().Contains(dlcId))
            {
                OC2Modding.Log.LogError($"Unexpected dlcId {dlcId}");
                return null;
            }

            if (T17FrontendFlow.Instance == null)
            {
                OC2Modding.Log.LogError("T17FrontendFlow.Instance was null");
                return null;
            }

            FieldInfo m_CoopGameSessionPrefabs_prop = T17FrontendFlow.Instance.GetType().GetField("m_CoopGameSessionPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (m_CoopGameSessionPrefabs_prop == null)
            {
                OC2Modding.Log.LogError("m_CoopGameSessionPrefabs_prop was null");
                return null;
            }

            DLCSerializedData<GameSession> m_CoopGameSessionPrefabs = (DLCSerializedData<GameSession>)m_CoopGameSessionPrefabs_prop.GetValue(T17FrontendFlow.Instance);
            if (m_CoopGameSessionPrefabs == null)
            {
                OC2Modding.Log.LogError("m_CoopGameSessionPrefabs was null");
                return null;
            }

            if (m_CoopGameSessionPrefabs.AllData == null)
            {
                OC2Modding.Log.LogError("m_CoopGameSessionPrefabs.AllData was null");
                return null;
            }

            if (m_CoopGameSessionPrefabs.AllData.Length == 0)
            {
                OC2Modding.Log.LogError("m_CoopGameSessionPrefabs.AllData was empty");
                return null;
            }

            GameSession[] gameSessions = (GameSession[])m_CoopGameSessionPrefabs.AllData;
            gameSessions = gameSessions.AllRemoved_Predicate((GameSession x) => x == null);
            if (gameSessions == null)
            {
                OC2Modding.Log.LogError("m_CoopGameSessionPrefabs.AllData contained all null things");
                return null;
            }

            GameSession gameSession = Array.Find<GameSession>(gameSessions, (GameSession x) => x.DLC == dlcId);
            if (gameSession == null)
            {
                OC2Modding.Log.LogError($"No game session matched DLC={dlcId}");
                return null;
            }

            GameProgress gameProgress = gameSession.gameObject.RequireComponentRecursive<GameProgress>();
            if (gameProgress == null)
            {
                OC2Modding.Log.LogError($"gameProgress was null");
                return null;
            }

            SceneDirectoryData sceneDirectory = gameProgress.GetSceneDirectory();
            if (sceneDirectory == null)
            {
                OC2Modding.Log.LogError($"sceneDirectory was null");
                return null;
            }

            if (sceneDirectory.Scenes.Length == 0)
            {
                OC2Modding.Log.LogError($"sceneDirectory.Scenes were null");
                return null;
            }

            return sceneDirectory.Scenes;
        }

        public static string getCustomSaveDirectory()
        {
            string saveFolderName = OC2Config.SaveFolderName == "" ? "_default" : (OC2Config.SaveFolderName + ArchipelagoClient.SaveDirSuffix());
            return Application.persistentDataPath + "/OC2Modding/" + saveFolderName + "/";
        }

        static float lastSoundTime = Time.time;
        const float ERROR_SFX_COOLDOWN = 0.4f;
        public static void PlayErrorSfx()
        {
            if (Time.time - lastSoundTime < ERROR_SFX_COOLDOWN)
            {
                return;
            }

            var session = GameUtils.GetGameSession();
            if (session == null)
            {
                return;
            }

            lastSoundTime = Time.time;
            GameUtils.TriggerAudio(GameOneShotAudioTag.RecipeTimeOut, session.gameObject.layer);
        }

        /* Helpers for the helpers start here */

        private struct LeaderboardScoresKey
        {
            public string game;
            public int DLCID;
            public string level;
            public uint playerCount;
        }

        private class DlcAndLevel
        {
            public string dlc;
            public string level;
        }

        private static DlcAndLevel getLevelName(int DLCID, int levelId)
        {
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
                    level = ""; // Horde Levels
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
                        level = ""; // Horde Levels
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

                level = $"{dlc} {level}"; // seasonal needs additional identifiers
            }

            DlcAndLevel result = new DlcAndLevel();
            result.dlc = dlc;
            result.level = level;
            return result;
        }

        public static bool IsDynamicLevel(string levelName)
        {
            return levelName.Contains("_Dynamic_") || levelName.Contains("_Special_");
        }

        private static void BuildLeaderboardScores()
        {
            leaderboardScores = new Dictionary<LeaderboardScoresKey, int>();

            downloadLeaderboardFile();

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadAllLines("leaderboard_scores.csv");
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to lookup scores in leaderboard-scores.csv:\n{e}");
                return;
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
                    string sanitaryLine = line.Replace("Sun’s Out,", "Sun's Out");
                    string[] values = sanitaryLine.Split(',');

                    int place = Int32.Parse(values[4]);
                    if (place != 1)
                    {
                        continue; // The score isn't good enough to return
                    }

                    string game = values[0].Replace("\"", "");
                    if (game != "Overcooked 2" && game != "All You Can Eat")
                    {
                        continue; // We don't care about OC1
                    }

                    string dlc = values[1].Replace("\"", "");
                    int dlcId = DLCFromString(dlc);
                    if (dlcId == -2)
                    {
                        continue; // We don't care about AYCE Exclusive DLC
                    }

                    int commaCount = 0;
                    int score = 0;
                    for (int i = sanitaryLine.Length - 1; i >= 0; i--)
                    {
                        if (sanitaryLine[i] == ',')
                        {
                            commaCount++;
                        }

                        if (commaCount == 2)
                        {
                            var substrings = sanitaryLine.Substring(i + 1).Split(',');
                            score = Int32.Parse(substrings[0]);
                            break;
                        }
                    }

                    if (score == 0)
                    {
                        OC2Modding.Log.LogWarning($"Failed to parse score from line:\n\t{line}");
                    }

                    string level = values[2].Replace("\"", "");
                    if (dlcId == 3) // seasonal needs additional identifiers
                    {
                        level = $"{dlc} {level}";
                    }

                    uint playerCount = UInt32.Parse(values[3]);

                    LeaderboardScoresKey key = new LeaderboardScoresKey();
                    key.game = game;
                    key.DLCID = dlcId;
                    key.level = level;
                    key.playerCount = playerCount;

                    // OC2Modding.Log.LogWarning($"Interpreted {key.game}|dlc={key.DLCID}|level={key.level}|score={score} ({key.playerCount}-Player) from:\n{line}\n");

                    leaderboardScores[key] = score;
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogWarning($"Failed to parse line:\n'{line}'\n{e}");
                }
            }

            OC2Modding.Log.LogInfo("Finished bulding high score repository");
        }

        public static bool IsLevelHordeLevel(int _levelIndex) {
            if (OC2Config.CustomLevelOrder == null)
            {
                return false;
            }

            if (!OC2Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return false;
            }

            if (!OC2Config.CustomLevelOrder["Story"].ContainsKey(_levelIndex))
            {
                return false;
            }

            OC2Config.DlcIdAndLevelId level = OC2Config.CustomLevelOrder["Story"][_levelIndex];

            int[] levels;
            if (level.Dlc == 7) // horde
            {
                levels = new int[] { 12, 13, 14, 15, 16, 17, 18, 19 };
            }
            else if (level.Dlc == 3) // seasonal
            {
                levels = new int[] { 13, 15 };
            }
            else
            {
                levels = new int[] {};
            }

            return levels.Contains(level.LevelId);
        }

        public static int DLCFromString(string dlc)
        {
            switch (dlc)
            {
                case "Story"                    : return -1;
                case "Story 2"                  : return -1;
                case "Surf 'n' Turf"            : return  2;
                case "Seasonal"                 : return  3;
                case "Christmas"                : return  3;
                case "Chinese New Year"         : return  3;
                case "Winter Wonderland"        : return  3;
                case "Moon Harvest"             : return  3;
                case "Spring Festival"          : return  3;
                case "Sun's Out Buns Out"       : return  3;
                case "Campfire Cook Off"        : return  5;
                case "Night of the Hangry Horde": return  7;
                case "Carnival of Chaos"        : return  8;
                default:
                    {
                        // OC2Modding.Log.LogWarning($"Unexpected DLC '{dlc}'");
                        return -2;
                    }
            }
        }

        [HarmonyPatch]
        private static class CurrentDLC
        {
            public static int m_DLCID = -1;

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

            [HarmonyPatch(typeof(SelectSaveDialog), nameof(SelectSaveDialog.OnDontSaveSelected))]
            [HarmonyPrefix]
            private static void OnDontSaveSelected()
            {
                m_DLCID = -1; // return to default "story"
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
                private static bool LoadDLC(ref DLCFrontendData dlcData)
                {
                    if (OC2Config.ForbidDLC)
                    {
                        GameLog.LogMessage("Loading DLC is not supported when playing this mod!");
                        return false;
                    }

                    m_DLCID = dlcData.m_DLCID;
                    OC2Modding.Log.LogInfo($"DLC set to {m_DLCID}:{DLCToString(m_DLCID)}");

                    return true;
                }
            }
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
                downloadProcess.WaitForExit();
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to download leaderboard-scores.csv: {e.ToString()}");
            }
        }

        private static bool isLevelTimerLevel(string dlc, string level)
        {
            string[] levels = new string[] { };
            if (dlc == "Story")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                    "1-5",
                    "2-4",
                    "2-6",
                    "3-1",
                    "5-4",
                    "6-1",
                };
            }
            else if (dlc == "Christmas")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                };
            }
            else if (dlc == "Chinese New Year")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                };
            }
            else if (dlc == "Winter Wonderland")
            {
                levels = new string[] {
                    "1-1",
                    "1-3",
                    "1-5",
                };
            }
            else if (dlc == "Spring Festival")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                };
            }
            else if (dlc == "Sun's Out Buns Out")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                    "1-3",
                };
            }
            else if (dlc == "Moon Harvest")
            {
                levels = new string[] {
                    "1-3",
                };
            }
            else if (dlc == "Surf 'n' Turf")
            {
                levels = new string[] {
                    "1-1",
                    "2-1",
                };
            } 
            else if (dlc == "Campfire Cook Off")
            {
                levels = new string[] {
                    "1-1",
                    "1-3",
                    "2-1",
                    "3-2",
                };
            } 
            else if (dlc == "Night of the Hangry Horde")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                    "2-2",
                };
            } 
            else if (dlc == "Carnival of Chaos")
            {
                levels = new string[] {
                    "1-1",
                    "1-2",
                    "1-4",
                    "2-1",
                    "2-2",
                };
            }

            // get just the world/sublevel from any level specifier
            var level_split = level.Split(' ');
            level = level_split[level_split.Length - 1];
            
            return levels.Contains(level);
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
    }
}
