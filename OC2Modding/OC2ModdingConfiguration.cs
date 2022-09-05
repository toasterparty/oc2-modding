using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace OC2Modding
{
    public static class OC2Config
    {
        public const bool CHEATS_ALLOWED = true;

        private static string JsonConfigPath = "";
        public static string SaveFolderName = "";
        public static bool JsonMode = false;

        /* Globally Accessible Config Values */
        // QoL + Cheats
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
        public static bool SkipAllOnionKing = false;
        public static bool ImpossibleTutorial = false;
        public static float LevelTimerScale = 1.0f;

        // Unlockable Items
        public static bool DisableWood = false;
        public static bool DisableCoal = false;
        public static bool DisableOnePlate = false;
        public static bool DisableFireExtinguisher = false;
        public static bool DisableBellows = false;
        public static bool PlatesStartDirty = false;
        public static int MaxTipCombo = 4;
        public static bool DisableDash = false;
        public static bool WeakDash = false;
        public static bool DisableThrow = false;
        public static bool DisableCatch = false;
        public static bool DisableControlStick = false;
        public static bool DisableWokDrag = false;
        public static float WashTimeMultiplier = 1.0f;
        public static float BurnSpeedMultiplier = 1.0f;
        public static int MaxOrdersOnScreenOffset = 0;
        public static float ChoppingTimeScale = 1.0f;
        public static float BackpackMovementScale = 1.0f;
        public static float RespawnTime = 5.0f;
        public static float CarnivalDispenserRefactoryTime = 0.0f;
        public static int StarOffset = 0;
        public static bool DisableRampButton = false;
        // Pick up plate stacks 1 at a time
        // Squirt Gun Distance
        // Guitine Cooldown
        // Custom Knockback Force (Bellows, Squirt Gun, Dashing, Throwing)
        // Repair Hammer
        // Coin Bag
        // Calmer Unbread
        // 6-6 Timer Advantage (progressive?) 

        // <UnlockerLevelId, LockedLevelId>
        public static Dictionary<int, int> LevelUnlockRequirements;
        public static Dictionary<int, int> LevelPurchaseRequirements;
        public static List<int> LevelForceReveal;
        public static Dictionary<int, float> LeaderboardScoreScale = null;
        public static Dictionary<string, Dictionary<int, DlcIdAndLevelId>> CustomLevelOrder = null;
        public static List<int> LockedEmotes;
        public static Dictionary<int, List<OnLevelCompletedEvent>> OnLevelCompleted = new Dictionary<int, List<OnLevelCompletedEvent>>();
        public static List<string> RecievedItemIdentifiers = new List<string>();

        public struct OnLevelCompletedEvent
        {
            public string message;
            public string action;
            public string payload;
        }

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
            LevelForceReveal = new List<int>();
            LockedEmotes = new List<int>();

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
            if (SaveFolderName != "")
            {
                InitJson(OC2Helpers.getCustomSaveDirectory() + "/OC2Modding.json");
            }

            FlushConfig();

            Harmony.CreateAndPatchAll(typeof(OC2Config));
        }

        public static void FlushConfig()
        {
            if (!JsonMode)
            {
                return;
            }

            string save_dir = OC2Helpers.getCustomSaveDirectory();
            if (!Directory.Exists(save_dir))
            {
                Directory.CreateDirectory(save_dir);
            }
            string filepath = save_dir + "/OC2Modding.json";
            string text = SerializeConfig();
            OC2Modding.Log.LogInfo($"Flushing config to '{filepath}'...");
            File.WriteAllText(filepath, text);
        }

        // Forgive me, for I have spaghetti code
        private static string SerializeConfig()
        {
            string data = "{";
            data += $"\"DisplayLeaderboardScores\":{DisplayLeaderboardScores},";
            data += $"\"AlwaysServeOldestOrder\":{AlwaysServeOldestOrder},";
            data += $"\"CustomOrderLifetime\":{CustomOrderLifetime},";
            data += $"\"Custom66TimerScale\":{Custom66TimerScale},";
            data += $"\"DisplayFPS\":{DisplayFPS},";
            data += $"\"FixDoubleServing\":{FixDoubleServing},";
            data += $"\"FixSinkBug\":{FixSinkBug},";
            data += $"\"FixControlStickThrowBug\":{FixControlStickThrowBug},";
            data += $"\"FixEmptyBurnerThrow\":{FixEmptyBurnerThrow},";
            data += $"\"PreserveCookingProgress\":{PreserveCookingProgress},";
            data += $"\"SkipTutorialPopups\":{SkipTutorialPopups},";
            data += $"\"TimerAlwaysStarts\":{TimerAlwaysStarts},";
            data += $"\"UnlockAllChefs\":{UnlockAllChefs},";
            data += $"\"UnlockAllDLC\":{UnlockAllDLC},";
            data += $"\"RevealAllLevels\":{RevealAllLevels},";
            data += $"\"PurchaseAllLevels\":{PurchaseAllLevels},";
            data += $"\"SkipTutorial\":{SkipTutorial},";
            data += $"\"CheatsEnabled\":{CheatsEnabled},";
            data += $"\"SkipAllOnionKing\":{SkipAllOnionKing},";
            data += $"\"DisableWood\":{DisableWood},";
            data += $"\"DisableCoal\":{DisableCoal},";
            data += $"\"DisableOnePlate\":{DisableOnePlate},";
            data += $"\"DisableFireExtinguisher\":{DisableFireExtinguisher},";
            data += $"\"DisableBellows\":{DisableBellows},";
            data += $"\"PlatesStartDirty\":{PlatesStartDirty},";
            data += $"\"MaxTipCombo\":{MaxTipCombo},";
            data += $"\"DisableDash\":{DisableDash},";
            data += $"\"WeakDash\":{WeakDash},";
            data += $"\"DisableThrow\":{DisableThrow},";
            data += $"\"DisableCatch\":{DisableCatch},";
            data += $"\"DisableControlStick\":{DisableControlStick},";
            data += $"\"DisableWokDrag\":{DisableWokDrag},";
            data += $"\"WashTimeMultiplier\":{WashTimeMultiplier},";
            data += $"\"BurnSpeedMultiplier\":{BurnSpeedMultiplier},";
            data += $"\"MaxOrdersOnScreenOffset\":{MaxOrdersOnScreenOffset},";
            data += $"\"ChoppingTimeScale\":{ChoppingTimeScale},";
            data += $"\"BackpackMovementScale\":{BackpackMovementScale},";
            data += $"\"RespawnTime\":{RespawnTime},";
            data += $"\"CarnivalDispenserRefactoryTime\":{CarnivalDispenserRefactoryTime},";
            data += $"\"StarOffset\":{StarOffset},";
            data += $"\"DisableRampButton\":{DisableRampButton},";
            data += $"\"LevelTimerScale\":{LevelTimerScale},";
            data += $"\"ImpossibleTutorial\":{ImpossibleTutorial},";
            

            data += $"\"LevelUnlockRequirements\":{{";
            bool first = true;
            foreach (KeyValuePair<int, int> kvp in LevelUnlockRequirements)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    data += ",";
                }
                data += $"\"{kvp.Key}\":{kvp.Value}";
            }
            data += "},";

            data += $"\"LevelPurchaseRequirements\":{{";
            first = true;
            foreach (KeyValuePair<int, int> kvp in LevelPurchaseRequirements)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    data += ",";
                }
                data += $"\"{kvp.Key}\":{kvp.Value}";
            }
            data += "},";

            data += $"\"LevelForceReveal\":[";
            first = true;
            foreach (int value in LevelForceReveal)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    data += ",";
                }
                data += $"{value}";
            }
            data += "],";

            if (LeaderboardScoreScale != null)
            {
                data += $"\"LeaderboardScoreScale\":{{";
                first = true;
                foreach (KeyValuePair<int, float> kvp in LeaderboardScoreScale)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        data += ",";
                    }

                    string key;
                    switch (kvp.Key)
                    {
                        case 1:
                            {
                                key = "OneStar";
                                break;
                            }
                        case 2:
                            {
                                key = "TwoStars";
                                break;
                            }
                        case 3:
                            {
                                key = "ThreeStars";
                                break;
                            }
                        case 4:
                            {
                                key = "FourStars";
                                break;
                            }
                        default:
                            {
                                key = "something went terrible wrong";
                                break;
                            }
                    }

                    data += $"\"{key}\":{kvp.Value.ToString("F4")}";
                }
                data += "},";
            }

            if (CustomLevelOrder != null)
            {
                data += $"\"CustomLevelOrder\":{{";
                first = true;
                foreach (KeyValuePair<string, Dictionary<int, DlcIdAndLevelId>> kvp in CustomLevelOrder)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        data += ",";
                    }
                    data += $"\"{kvp.Key}\":";
                    data += "{";

                    bool first2 = true;
                    foreach (KeyValuePair<int, DlcIdAndLevelId> kvp2 in kvp.Value)
                    {
                        if (first2)
                        {
                            first2 = false;
                        }
                        else
                        {
                            data += ",";
                        }

                        string DLC = OC2Helpers.DLCFromDLCID(kvp2.Value.Dlc);
                        int LevelID = kvp2.Value.LevelId;
                        data += $"\"{kvp2.Key}\":";
                        data += "{";
                        data += $"\"DLC\":\"{DLC}\",";
                        data += $"\"LevelID\":{LevelID}";
                        data += "}";
                    }

                    data += "}";
                }
                data += "},";
            }

            data += $"\"RecievedItemIdentifiers\":[";
            first = true;
            foreach (string value in RecievedItemIdentifiers)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    data += ",";
                }
                data += $"{value}";
            }
            data += "]";

            data += $"\"LockedEmotes\":[";
            first = true;
            foreach (int value in LockedEmotes)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    data += ",";
                }
                data += $"{value}";
            }
            data += "]";

            data += "}";

            data = data.Replace("True", "true");
            data = data.Replace("False", "false");

            return data;
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
                OC2Modding.Log.LogInfo($"Loading config from '{filename}'...");
                string json;
                using (StreamReader reader = new StreamReader(filename))
                {
                    json = reader.ReadToEnd();
                }

                JsonMode = true;
                UpdateConfig(json);
            }
            catch
            {
                OC2Modding.Log.LogError($"Failed to parse json from {filename}");
            }
        }

        public static void UpdateConfig(string text)
        {
            OC2Modding.Log.LogInfo($"Applying Config:\n{text}");

            var config = SimpleJSON.JSON.Parse(text);

            try { if (config.HasKey("DisableAllMods"                 )) DisableAllMods                 = config["DisableAllMods"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableAllMods'"                 ); }
            try { if (config.HasKey("DisplayLeaderboardScores"       )) DisplayLeaderboardScores       = config["DisplayLeaderboardScores"       ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisplayLeaderboardScores'"       ); }
            try { if (config.HasKey("AlwaysServeOldestOrder"         )) AlwaysServeOldestOrder         = config["AlwaysServeOldestOrder"         ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'AlwaysServeOldestOrder'"         ); }
            try { if (config.HasKey("CustomOrderLifetime"            )) CustomOrderLifetime            = config["CustomOrderLifetime"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CustomOrderLifetime'"            ); }
            try { if (config.HasKey("Custom66TimerScale"             )) Custom66TimerScale             = config["Custom66TimerScale"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'Custom66TimerScale'"             ); }
            try { if (config.HasKey("DisplayFPS"                     )) DisplayFPS                     = config["DisplayFPS"                     ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisplayFPS'"                     ); }
            try { if (config.HasKey("FixDoubleServing"               )) FixDoubleServing               = config["FixDoubleServing"               ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixDoubleServing'"               ); }
            try { if (config.HasKey("FixSinkBug"                     )) FixSinkBug                     = config["FixSinkBug"                     ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixSinkBug'"                     ); }
            try { if (config.HasKey("FixControlStickThrowBug"        )) FixControlStickThrowBug        = config["FixControlStickThrowBug"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixControlStickThrowBug'"        ); }
            try { if (config.HasKey("FixEmptyBurnerThrow"            )) FixEmptyBurnerThrow            = config["FixEmptyBurnerThrow"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'FixEmptyBurnerThrow'"            ); }
            try { if (config.HasKey("PreserveCookingProgress"        )) PreserveCookingProgress        = config["PreserveCookingProgress"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PreserveCookingProgress'"        ); }
            try { if (config.HasKey("SkipTutorialPopups"             )) SkipTutorialPopups             = config["SkipTutorialPopups"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorialPopups'"             ); }
            try { if (config.HasKey("TimerAlwaysStarts"              )) TimerAlwaysStarts              = config["TimerAlwaysStarts"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'TimerAlwaysStarts'"              ); }
            try { if (config.HasKey("UnlockAllChefs"                 )) UnlockAllChefs                 = config["UnlockAllChefs"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllChefs'"                 ); }
            try { if (config.HasKey("UnlockAllDLC"                   )) UnlockAllDLC                   = config["UnlockAllDLC"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllDLC'"                   ); }
            try { if (config.HasKey("RevealAllLevels"                )) RevealAllLevels                = config["RevealAllLevels"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'RevealAllLevels'"                ); }
            try { if (config.HasKey("PurchaseAllLevels"              )) PurchaseAllLevels              = config["PurchaseAllLevels"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PurchaseAllLevels'"              ); }
            try { if (config.HasKey("SkipTutorial"                   )) SkipTutorial                   = config["SkipTutorial"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorial'"                   ); }
            try { if (config.HasKey("CheatsEnabled"                  )) CheatsEnabled                  = config["CheatsEnabled"                  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CheatsEnabled'"                  ); }
            try { if (config.HasKey("SaveFolderName"                 )) SaveFolderName                 = config["SaveFolderName"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SaveFolderName'"                 ); }
            try { if (config.HasKey("JsonConfigPath"                 )) JsonConfigPath                 = config["JsonConfigPath"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'JsonConfigPath'"                 ); }
            try { if (config.HasKey("DisableWood"                    )) DisableWood                    = config["DisableWood"                    ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableWood'"                    ); }
            try { if (config.HasKey("DisableCoal"                    )) DisableCoal                    = config["DisableCoal"                    ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableCoal'"                    ); }
            try { if (config.HasKey("DisableOnePlate"                )) DisableOnePlate                = config["DisableOnePlate"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableOnePlate'"                ); }
            try { if (config.HasKey("DisableFireExtinguisher"        )) DisableFireExtinguisher        = config["DisableFireExtinguisher"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableFireExtinguisher'"        ); }
            try { if (config.HasKey("DisableBellows"                 )) DisableBellows                 = config["DisableBellows"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableBellows'"                 ); }
            try { if (config.HasKey("PlatesStartDirty"               )) PlatesStartDirty               = config["PlatesStartDirty"               ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PlatesStartDirty'"               ); }
            try { if (config.HasKey("MaxTipCombo"                    )) MaxTipCombo                    = config["MaxTipCombo"                    ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'MaxTipCombo'"                    ); }
            try { if (config.HasKey("DisableDash"                    )) DisableDash                    = config["DisableDash"                    ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableDash'"                    ); }
            try { if (config.HasKey("WeakDash"                       )) WeakDash                       = config["WeakDash"                       ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'WeakDash'"                       ); }
            try { if (config.HasKey("DisableThrow"                   )) DisableThrow                   = config["DisableThrow"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableThrow'"                   ); }
            try { if (config.HasKey("DisableCatch"                   )) DisableCatch                   = config["DisableCatch"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableCatch'"                   ); }
            try { if (config.HasKey("DisableControlStick"            )) DisableControlStick            = config["DisableControlStick"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableControlStick'"            ); }
            try { if (config.HasKey("DisableWokDrag"                 )) DisableWokDrag                 = config["DisableWokDrag"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableWokDrag'"                 ); }
            try { if (config.HasKey("WashTimeMultiplier"             )) WashTimeMultiplier             = config["WashTimeMultiplier"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'WashTimeMultiplier'"             ); }
            try { if (config.HasKey("BurnSpeedMultiplier"            )) BurnSpeedMultiplier            = config["BurnSpeedMultiplier"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'BurnSpeedMultiplier'"            ); }
            try { if (config.HasKey("MaxOrdersOnScreenOffset"        )) MaxOrdersOnScreenOffset        = config["MaxOrdersOnScreenOffset"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'MaxOrdersOnScreenOffset'"        ); }
            try { if (config.HasKey("SkipAllOnionKing"               )) SkipAllOnionKing               = config["SkipAllOnionKing"               ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipAllOnionKing'"               ); }
            try { if (config.HasKey("ChoppingTimeScale"              )) ChoppingTimeScale              = config["ChoppingTimeScale"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'ChoppingTimeScale'"              ); }
            try { if (config.HasKey("BackpackMovementScale"          )) BackpackMovementScale          = config["BackpackMovementScale"          ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'BackpackMovementScale'"          ); }
            try { if (config.HasKey("RespawnTime"                    )) RespawnTime                    = config["RespawnTime"                    ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'RespawnTime'"                    ); }
            try { if (config.HasKey("CarnivalDispenserRefactoryTime" )) CarnivalDispenserRefactoryTime = config["CarnivalDispenserRefactoryTime" ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CarnivalDispenserRefactoryTime'" ); }
            try { if (config.HasKey("StarOffset"                     )) StarOffset                     = config["StarOffset"                     ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'StarOffset'"                     ); }
            try { if (config.HasKey("ImpossibleTutorial"             )) ImpossibleTutorial             = config["ImpossibleTutorial"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'ImpossibleTutorial'"             ); }
            try { if (config.HasKey("DisableRampButton"              )) DisableRampButton              = config["DisableRampButton"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableRampButton'"              ); }
            try { if (config.HasKey("LevelTimerScale"                )) LevelTimerScale                = config["LevelTimerScale"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'LevelTimerScale'"                ); }

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

                    if (CustomLevelOrder != null)
                    {
                        CustomLevelOrder.Clear();
                    }

                    CustomLevelOrder = tempDict;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'CustomLevelOrder'");
            }

            try
            {
                if (config.HasKey("LevelForceReveal"))
                {
                    List<int> temp = new List<int>();

                    foreach (int levelId in config["LevelForceReveal"].AsArray.Values)
                    {
                        temp.Add(levelId);
                    }

                    LevelForceReveal.Clear();
                    LevelForceReveal = temp;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'LevelForceReveal'");
            }

            try
            {
                if (config.HasKey("LockedEmotes"))
                {
                    List<int> temp = new List<int>();

                    foreach (int levelId in config["LockedEmotes"].AsArray.Values)
                    {
                        temp.Add(levelId);
                    }

                    LockedEmotes.Clear();
                    LockedEmotes = temp;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'LockedEmotes'");
            }

            try
            {
                if (config.HasKey("RecievedItemIdentifiers"))
                {
                    List<string> temp = new List<string>();

                    foreach (string identifier in config["RecievedItemIdentifiers"].AsArray.Values)
                    {
                        temp.Add(identifier);
                    }

                    RecievedItemIdentifiers.Clear();
                    RecievedItemIdentifiers = temp;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'RecievedItemIdentifiers'");
            }

            try
            {
                if (config.HasKey("OnLevelCompleted"))
                {
                    Dictionary<int, List<OnLevelCompletedEvent>> temp = new Dictionary<int, List<OnLevelCompletedEvent>>();

                    foreach (KeyValuePair<string, SimpleJSON.JSONNode> kvp in config["OnLevelCompleted"].AsObject)
                    {
                        List<OnLevelCompletedEvent> list = new List<OnLevelCompletedEvent>();
                        foreach (SimpleJSON.JSONNode value in kvp.Value.AsArray.Values)
                        {
                            try
                            {
                                var obj = value.AsObject;
                                OnLevelCompletedEvent e = new OnLevelCompletedEvent();
                                e.action = obj["action"].Value;
                                e.payload = obj["payload"].Value;
                                if (obj.HasKey("message"))
                                {
                                    e.message = obj["message"].Value;
                                }
                                else
                                {
                                    e.message = "";
                                }
                                list.Add(e);
                            }
                            catch
                            {
                                OC2Modding.Log.LogWarning($"Failed to parse key 'OnLevelCompleted'");
                            }
                        }

                        temp.Add(Int32.Parse(kvp.Key), list);
                    }

                    OnLevelCompleted.Clear();
                    OnLevelCompleted = temp;
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'OnLevelCompleted'\n:{e}");
            }
        }

        /* In the event that the player presses "New Game" try your hardest to reset the configuration */
        [HarmonyPatch(typeof(SaveSlotElement), "SaveNewGame")]
        [HarmonyPostfix]
        private static void SaveNewGame()
        {
            if (JsonMode)
            {
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

                RecievedItemIdentifiers.Clear(); // When starting a new game, reset the remote items that have been received
                ArchipelagoClient.UpdateInventory(); // and then immediately apply them all

                FlushConfig();
            }
        }

        [HarmonyPatch(typeof(SelectSaveDialog), "Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix(ref T17EventSystem ___m_eventSystem, ref SaveSlotElement[] ___m_saveElements)
        {
            // OC2Modding only allows a single save slot
            if (JsonMode)
            {
                ___m_saveElements[1].gameObject.SetActive(false);
                ___m_saveElements[2].gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(SelectSaveDialog), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(ref T17EventSystem ___m_eventSystem, ref SaveSlotElement[] ___m_saveElements)
        {
            // OC2Modding only allows a single save slot
            if (JsonMode)
            {
                ___m_saveElements[1].gameObject.SetActive(false);
                ___m_saveElements[2].gameObject.SetActive(false);
            }
        }
    }
}
