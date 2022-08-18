using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;

namespace OC2Modding
{
    public static class OC2Config
    {
        public const bool CHEATS_ALLOWED = true;

        private static string JsonConfigPath = "";

        /* Globally Accessible Config Values */
        public static bool DisableAllMods;
        public static bool DisplayLeaderboardScores;
        public static bool AlwaysServeOldestOrder;
        public static float CustomOrderLifetime;
        public static float Custom66TimerScale = 1.0f;
        public static bool DisplayFPS;
        public static bool FixDoubleServing;
        public static bool FixSinkBug;
        public static bool FixControlStickThrowBug;
        public static bool FixEmptyBurnerThrow;
        public static bool PreserveCookingProgress;
        public static bool SkipTutorialPopups;
        public static bool TimerAlwaysStarts;
        public static bool UnlockAllChefs;
        public static bool UnlockAllDLC;
        public static bool RevealAllLevels;
        public static bool PurchaseAllLevels;
        public static bool SkipTutorial;
        public static bool CheatsEnabled = false;
        public static bool DisableWood = false;
        public static bool DisableCoal = false;
        public static bool DisableOnePlate = false;
        public static bool DisableFireExtinguisher = false;
        public static bool DisableBellows = false;

        // <UnlockerLevelId, LockedLevelId>
        public static Dictionary<int, int> LevelUnlockRequirements;
        public static Dictionary<int, int> LevelPurchaseRequirements;
        public static Dictionary<int, float> LeaderboardScoreScale = null;
        public static Dictionary<string, Dictionary<int, DlcIdAndLevelId>> CustomLevelOrder = null;
        
        public struct DlcIdAndLevelId
        {
            public int Dlc;
            public int LevelId;
        }

        public static void Awake()
        {
            /* Initialize Memory */
            LevelUnlockRequirements = new Dictionary<int, int>();
            LevelPurchaseRequirements = new Dictionary<int, int>();

            /* Initialize Standalone Config */
            InitCfg();

            /* Initialize API Config */
            if (JsonConfigPath == "")
            {
                InitJson("OC2Modding.json");
            }
            if (JsonConfigPath != "")
            {
                InitJson(JsonConfigPath);
            }
        }

        /* Create OC2Modding.cfg if it doesn't exist and populate it 
           with all possible config options. Load the file and set all */
        private static void InitCfg()
        {
            ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "OC2Modding.cfg"), true);

            ConfigEntry<bool> configDisableAllMods = configFile.Bind(
                "_DisableAllMods_",
                "DisableAllMods",
                false,
                "Set to true to completely return the game back to it's original state"
            );
            DisableAllMods = configDisableAllMods.Value;

            ConfigEntry<bool> configAlwaysServeOldestOrder = configFile.Bind(
                "GameModifications", // Config Category
                "AlwaysServeOldestOrder", // Config key name
                false, // Default Config value
                "When an order expires in the base game, it tries to 'help' the player(s) by making it so that the next dish server of that type goes to highest scoring ticket, rather than the one which would let the player(s) dig out of a broken tip combo. Set this to true to make the game always serve the oldest ticket." // Friendly description
            );
            AlwaysServeOldestOrder = configAlwaysServeOldestOrder.Value;

            ConfigEntry<float> configCustomOrderLifetime = configFile.Bind(
                "GameModifications", // Config Category
                "CustomOrderLifetime", // Config key name
                100.0f, // Default Config value
                "Customize how long orders take before expiring" // Friendly description
            );
            CustomOrderLifetime = configCustomOrderLifetime.Value;

            ConfigEntry<bool> configPreserveCookingProgress = configFile.Bind(
                "GameModifications", // Config Category
                "PreserveCookingProgress", // Config key name
                false, // Default Config value
                "In the base game, certain cooking vessels reset their cooking progress to 0% or 50% when a new item is added. Enabling this option makes the behavior consistent across all vessels (Adding uncooked items to cooked ones preserves always preserves cooked progress)" // Friendly description
            );
            PreserveCookingProgress = configPreserveCookingProgress.Value;

            ConfigEntry<bool> configTimerAlwaysStarts = configFile.Bind(
                "GameModifications", // Config Category
                "TimerAlwaysStarts", // Config key name
                false, // Default Config value
                "Set to true to make levels which normally have \"Prep Time\" start immediately" // Friendly description
            );
            TimerAlwaysStarts = configTimerAlwaysStarts.Value;

            ConfigEntry<bool> configPurchaseAllLevels = configFile.Bind(
                "GameModifications", // Config Category
                "PurchaseAllLevels", // Config key name
                false, // Default Config value
                "Set to true to remove the requirement for purchasing levels before playing them" // Friendly description
            );
            PurchaseAllLevels = configPurchaseAllLevels.Value;

            if (CHEATS_ALLOWED)
            {
                ConfigEntry<bool> configCheatsEnabled = configFile.Bind(
                    "GameModifications", // Config Category
                    "CheatsEnabled", // Config key name
                    false, // Default Config value
                    "Enables some not very fun cheats for debug purposes" // Friendly description
                );
                CheatsEnabled = configCheatsEnabled.Value;
            }

            ConfigEntry<bool> configDisplayLeaderboardScores = configFile.Bind(
                "QualityOfLife", // Config Category
                "DisplayLeaderboardScores", // Config key name
                true, // Default Config value
                "Set to true to show the top 5 leaderboard scores when previewing a level" // Friendly description
            );
            DisplayLeaderboardScores = configDisplayLeaderboardScores.Value;

            ConfigEntry<bool> configDisplayFPS = configFile.Bind(
                "QualityOfLife", // Config Category
                "DisplayLeaderboardScores", // Config key name
                true, // Default Config value
                "Set to true to show FPS when HOME is pressed (hide with END)" // Friendly description
            );
            DisplayFPS = configDisplayFPS.Value;

            ConfigEntry<bool> configSkipTutorialPopups = configFile.Bind(
                "QualityOfLife", // Config Category
                "SkipTutorialPopups", // Config key name
                true, // Default Config value
                "Set to true to skip showing tutorial popups before starting a level" // Friendly description
            );
            SkipTutorialPopups = configSkipTutorialPopups.Value;

            ConfigEntry<bool> configUnlockAllChefs = configFile.Bind(
                "QualityOfLife", // Config Category
                "UnlockAllChefs", // Config key name
                false, // Default Config value
                "Set to true to show all Chefs on the Chef selection screen" // Friendly description
            );
            UnlockAllChefs = configUnlockAllChefs.Value;

            ConfigEntry<bool> configUnlockAllDLC = configFile.Bind(
                "QualityOfLife", // Config Category
                "UnlockAllDLC", // Config key name
                true, // Default Config value
                "Set to true to unlock all DLC, I don't know if this works, because I own all DLC -toasterparty" // Friendly description
            );
            UnlockAllDLC = configUnlockAllDLC.Value;

            ConfigEntry<bool> configRevealAllLevels = configFile.Bind(
                "QualityOfLife", // Config Category
                "RevealAllLevels", // Config key name
                true, // Default Config value
                "Set to true to immediately flip all hidden tiles on the overworld" // Friendly description
            );
            RevealAllLevels = configRevealAllLevels.Value;

            ConfigEntry<bool> configSkipTutorial = configFile.Bind(
                "QualityOfLife", // Config Category
                "SkipTutorial", // Config key name
                false, // Default Config value
                "Set to true to skip the mandatory tutorial when starting a new game" // Friendly description
            );
            SkipTutorial = configSkipTutorial.Value;

            ConfigEntry<bool> configFixDoubleServing = configFile.Bind(
                "Bugfixes", // Config Category
                "FixDoubleServing", // Config key name
                true, // Default Config value
                "Set to true to fix a bug which ruins competitive play" // Friendly description
            );
            FixDoubleServing = configFixDoubleServing.Value;

            ConfigEntry<bool> configFixSinkBug = configFile.Bind(
                "Bugfixes", // Config Category
                "FixSinkBug", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where sinks can have reduced usability for the rest of the level" // Friendly description
            );
            FixSinkBug = configFixSinkBug.Value;

            ConfigEntry<bool> configFixControlStickThrowBug = configFile.Bind(
                "Bugfixes", // Config Category
                "FixControlStickThrowBug", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where cancelling out of a platform control stick in a specific way would eat the next throw input" // Friendly description
            );
            FixControlStickThrowBug = configFixControlStickThrowBug.Value;

            ConfigEntry<bool> configFixEmptyBurnerThrow = configFile.Bind(
                "Bugfixes", // Config Category
                "FixEmptyBurnerThrow", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where you cannot throw items when standing directly over a burner/mixer with no pan/bowl" // Friendly description
            );
            FixEmptyBurnerThrow = configFixEmptyBurnerThrow.Value;
        }
    
        private static void InitJson(string filename)
        {
            try 
            {
                string json;
                using (StreamReader reader = new StreamReader(filename))
                {
                    json = reader.ReadToEnd();
                }

                var config = SimpleJSON.JSON.Parse(json);

                try { if (config.HasKey("DisableAllMods"           )) DisableAllMods           = config["DisableAllMods"           ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableAllMods'"           ); }
                try { if (config.HasKey("DisplayLeaderboardScores" )) DisplayLeaderboardScores = config["DisplayLeaderboardScores" ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisplayLeaderboardScores'" ); }
                try { if (config.HasKey("AlwaysServeOldestOrder"   )) AlwaysServeOldestOrder   = config["AlwaysServeOldestOrder"   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'AlwaysServeOldestOrder'"   ); }
                try { if (config.HasKey("CustomOrderLifetime"      )) CustomOrderLifetime      = config["CustomOrderLifetime"      ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CustomOrderLifetime'"      ); }
                try { if (config.HasKey("Custom66TimerScale"       )) Custom66TimerScale       = config["Custom66TimerScale"       ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'Custom66TimerScale'"       ); }
                try { if (config.HasKey("DisplayFPS"               )) DisplayFPS               = config["DisplayFPS"               ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisplayFPS'"               ); }
                try { if (config.HasKey("FixDoubleServing"         )) FixDoubleServing         = config["FixDoubleServing"         ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixDoubleServing'"         ); }
                try { if (config.HasKey("FixSinkBug"               )) FixSinkBug               = config["FixSinkBug"               ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixSinkBug'"               ); }
                try { if (config.HasKey("FixControlStickThrowBug"  )) FixControlStickThrowBug  = config["FixControlStickThrowBug"  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixControlStickThrowBug'"  ); }
                try { if (config.HasKey("FixEmptyBurnerThrow"      )) FixEmptyBurnerThrow      = config["FixEmptyBurnerThrow"      ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixEmptyBurnerThrow'"      ); }
                try { if (config.HasKey("PreserveCookingProgress"  )) PreserveCookingProgress  = config["PreserveCookingProgress"  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PreserveCookingProgress'"  ); }
                try { if (config.HasKey("SkipTutorialPopups"       )) SkipTutorialPopups       = config["SkipTutorialPopups"       ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorialPopups'"       ); }
                try { if (config.HasKey("TimerAlwaysStarts"        )) TimerAlwaysStarts        = config["TimerAlwaysStarts"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'TimerAlwaysStarts'"        ); }
                try { if (config.HasKey("UnlockAllChefs"           )) UnlockAllChefs           = config["UnlockAllChefs"           ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllChefs'"           ); }
                try { if (config.HasKey("UnlockAllDLC"             )) UnlockAllDLC             = config["UnlockAllDLC"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllDLC'"             ); }
                try { if (config.HasKey("RevealAllLevels"          )) RevealAllLevels          = config["RevealAllLevels"          ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'RevealAllLevels'"          ); }
                try { if (config.HasKey("PurchaseAllLevels"        )) PurchaseAllLevels        = config["PurchaseAllLevels"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PurchaseAllLevels'"        ); }
                try { if (config.HasKey("SkipTutorial"             )) SkipTutorial             = config["SkipTutorial"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorial'"             ); }
                try { if (config.HasKey("CheatsEnabled"            )) CheatsEnabled            = config["CheatsEnabled"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CheatsEnabled'"            ); }
                try { if (config.HasKey("JsonConfigPath"           )) JsonConfigPath           = config["JsonConfigPath"           ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'JsonConfigPath'"           ); }
                try { if (config.HasKey("DisableWood"              )) DisableWood              = config["DisableWood"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableWood'"              ); }
                try { if (config.HasKey("DisableCoal"              )) DisableCoal              = config["DisableCoal"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableCoal'"              ); }
                try { if (config.HasKey("DisableOnePlate"          )) DisableOnePlate          = config["DisableOnePlate"          ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableOnePlate'"          ); }
                try { if (config.HasKey("DisableFireExtinguisher"  )) DisableFireExtinguisher  = config["DisableFireExtinguisher"  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableFireExtinguisher'"  ); }
                try { if (config.HasKey("DisableBellows"           )) DisableBellows           = config["DisableBellows"           ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableBellows'"           ); }

                try
                {
                    Dictionary<int, int> tempDict = new Dictionary<int, int>();
                    if (config.HasKey("LevelUnlockRequirements"))
                    {
                        foreach (KeyValuePair<string, SimpleJSON.JSONNode> kvp in config["LevelUnlockRequirements"].AsObject)
                        {
                            tempDict.Add(int.Parse(kvp.Key), kvp.Value);
                        }

                        LevelUnlockRequirements.Clear();
                        LevelUnlockRequirements = tempDict;
                    }
                }
                catch
                {
                    OC2Modding.Log.LogWarning($"Failed to parse key 'LevelUnlockRequirements'");
                }

                try
                {
                    if (config.HasKey("LevelPurchaseRequirements"))
                    {
                        Dictionary<int, int> tempDict = new Dictionary<int, int>();

                        foreach (KeyValuePair<string, SimpleJSON.JSONNode> kvp in config["LevelPurchaseRequirements"].AsObject)
                        {
                            tempDict.Add(int.Parse(kvp.Key), kvp.Value);
                        }

                        LevelPurchaseRequirements.Clear();
                        LevelPurchaseRequirements = tempDict;
                    }
                }
                catch
                {
                    OC2Modding.Log.LogWarning($"Failed to parse key 'LevelPurchaseRequirements'");
                }

                try
                {
                    if (config.HasKey("LeaderboardScoreScale"))
                    {
                        Dictionary<int, float> tempDict = new Dictionary<int, float>();

                        tempDict[1] = config["LeaderboardScoreScale"]["OneStar"];
                        tempDict[2] = config["LeaderboardScoreScale"]["TwoStars"];
                        tempDict[3] = config["LeaderboardScoreScale"]["ThreeStars"];
                        tempDict[4] = config["LeaderboardScoreScale"]["FourStars"];

                        if (tempDict[1] > tempDict[2] || tempDict[1] > tempDict[3] || tempDict[1] > tempDict[4])
                        {
                            throw new System.Exception("");
                        }

                        if (LeaderboardScoreScale != null)
                        {
                            LeaderboardScoreScale.Clear();
                        }

                        LeaderboardScoreScale = tempDict;
                    }
                }
                catch
                {
                    OC2Modding.Log.LogWarning($"Failed to parse key 'LeaderboardScoreScale'");
                }

                try
                {
                    if (config.HasKey("CustomLevelOrder"))
                    {
                        Dictionary<string, Dictionary<int, DlcIdAndLevelId>> tempDict = new Dictionary<string, Dictionary<int, DlcIdAndLevelId>>();

                        if (config["CustomLevelOrder"].HasKey("Story"))
                        {
                            Dictionary<int, DlcIdAndLevelId> tempDictDlc = new Dictionary<int, DlcIdAndLevelId>();
                            foreach (KeyValuePair<string, SimpleJSON.JSONNode> kvp in config["CustomLevelOrder"].AsObject["Story"].AsObject)
                            {
                                DlcIdAndLevelId dlcIdAndLevelId = new DlcIdAndLevelId();
                                dlcIdAndLevelId.Dlc = OC2Helpers.DLCFromString(kvp.Value.AsObject["DLC"]);
                                dlcIdAndLevelId.LevelId = kvp.Value.AsObject["LevelID"];
                                tempDictDlc.Add(int.Parse(kvp.Key), dlcIdAndLevelId);
                            }
                            tempDict.Add("Story", tempDictDlc);
                        }

                        CustomLevelOrder = tempDict;
                    }
                }
                catch
                {
                    OC2Modding.Log.LogWarning($"Failed to parse key 'CustomLevelOrder'");
                }
            }
            catch {}
        }
    }
}
