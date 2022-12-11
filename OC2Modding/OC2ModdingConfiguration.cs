using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;

namespace OC2Modding
{
    public class ModConfig
    {
        public string SaveFolderName = "";
        public bool DisableArchipelagoLogin = false;

        /* Globally Accessible Config Values */
        // QoL + Cheats
        public bool DisableAllMods;
        public bool DisplayLeaderboardScores;
        public bool AlwaysServeOldestOrder;
        public float CustomOrderLifetime;
        public float Custom66TimerScale = 1.0f;
        public bool DisplayFPS;
        public bool FixDoubleServing;
        public bool FixSinkBug;
        public bool FixControlStickThrowBug;
        public bool FixEmptyBurnerThrow;
        public bool AlwaysPreserveCookingProgress;
        public bool SkipTutorialPopups;
        public bool TimerAlwaysStarts;
        public bool UnlockAllChefs;
        public bool UnlockAllDLC;
        public bool RevealAllLevels;
        public bool PurchaseAllLevels;
        public bool SkipTutorial;
        public float ServerTickRate;
        public float ServerTickRateUrgent;
        public float FixedDeltaTime;
        public bool CheatsEnabled = false;
        public bool SkipAllOnionKing = false;
        public bool ImpossibleTutorial = false;
        public float LevelTimerScale = 1.0f;

        // Unlockable Items
        public bool DisableWood = false;
        public bool DisableCoal = false;
        public bool DisableOnePlate = false;
        public bool DisableFireExtinguisher = false;
        public bool DisableBellows = false;
        public bool PlatesStartDirty = false;
        public int MaxTipCombo = 4;
        public bool DisableDash = false;
        public bool WeakDash = false;
        public bool DisableThrow = false;
        public bool DisableCatch = false;
        public bool DisableControlStick = false;
        public bool DisableWokDrag = false;
        public float WashTimeMultiplier = 1.0f;
        public float BurnSpeedMultiplier = 1.0f;
        public int MaxOrdersOnScreenOffset = 0;
        public float ChoppingTimeScale = 1.0f;
        public float BackpackMovementScale = 1.0f;
        public float RespawnTime = 5.0f;
        public float CarnivalDispenserRefactoryTime = 0.0f;
        public int StarOffset = 0;
        public bool DisableRampButton = false;
        public bool DisableEarnHordeMoney = false;
        public bool AggressiveHorde = false;
        public int CustomOrderTimeoutPenalty = -1;
        // Pick up plate stacks 1 at a time
        // Squirt Gun Distance
        // Guitine Cooldown
        // Custom Knockback Force (Bellows, Squirt Gun, Dashing, Throwing)
        // 6-6 Timer Advantage (progressive?) 

        // Randomizer requirements
        public bool ForbidDLC = false;
        public bool ForceSingleSaveSlot = false;
        public bool DisableNGP = false;

        // <UnlockerLevelId, LockedLevelId>
        public Dictionary<int, int> LevelUnlockRequirements;
        public Dictionary<int, int> LevelPurchaseRequirements;
        public List<int> LevelForceReveal;
        public List<int> LevelForceHide;
        public StarScales LeaderboardScoreScale = null;
        public Dictionary<string, Dictionary<int, DlcIdAndLevelId>> CustomLevelOrder = null;
        public List<int> LockedEmotes;
        public Dictionary<int, List<OnLevelCompletedEvent>> OnLevelCompleted = new Dictionary<int, List<OnLevelCompletedEvent>>();
        public Dictionary<int, int> PseudoSave = new Dictionary<int, int>(); // levelId, stars
        public int ItemIndex = 0;

        public struct OnLevelCompletedEvent
        {
            public string message;
            public string action;
            public string payload;
        }

        public struct DlcIdAndLevelId
        {
            public string DLC;
            public int LevelID;

            [JsonIgnore]
            public int dlc;
        }

        public class StarScales
        {
            public float FourStars;
            public float ThreeStars;
            public float TwoStars;
            public float OneStar;
        }

        /* Create OC2Modding.cfg if it doesn't exist and populate it 
           with all possible config options. Load the file and set all */
        public void InitConfigStandalone()
        {
            ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "OC2Modding.cfg"), true);

            ConfigEntry<bool> configDisableAllMods = configFile.Bind(
                "_DisableAllMods_",
                "DisableAllMods",
                false,
                "Set to true to completely return the game back to it's original state"
            );
            DisableAllMods = configDisableAllMods.Value;

            ConfigEntry<bool> configDisableArchipelagoLogin = configFile.Bind(
                "_DisableAllMods_",
                "DisableArchipelagoLogin",
                false,
                "Set to true to automatically bypass the login screen for Archipelago games"
            );
            DisableArchipelagoLogin = configDisableArchipelagoLogin.Value;

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
            AlwaysPreserveCookingProgress = configPreserveCookingProgress.Value;

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

            ConfigEntry<bool> configCheatsEnabled = configFile.Bind(
                "GameModifications", // Config Category
                "CheatsEnabled", // Config key name
                false, // Default Config value
                "Enables some not very fun cheats for debug purposes" // Friendly description
            );
            CheatsEnabled = configCheatsEnabled.Value;

            ConfigEntry<bool> configDisplayLeaderboardScores = configFile.Bind(
                "QualityOfLife", // Config Category
                "DisplayLeaderboardScores", // Config key name
                true, // Default Config value
                "Set to true to show the top 5 leaderboard scores when previewing a level" // Friendly description
            );
            DisplayLeaderboardScores = configDisplayLeaderboardScores.Value;

            ConfigEntry<bool> configDisplayFPS = configFile.Bind(
                "QualityOfLife", // Config Category
                "configDisplayFPS", // Config key name
                true, // Default Config value
                "Set to true to show FPS when HOME is pressed (hide with END)" // Friendly description
            );
            DisplayFPS = configDisplayFPS.Value;

            ConfigEntry<bool> configSkipTutorialPopups = configFile.Bind(
                "QualityOfLife", // Config Category
                "SkipTutorialPopups", // Config key name
                false, // Default Config value
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
                "Debug", // Config key name
                false, // Default Config value
                "Flag foR dEbugEring Debug-Like Code" // Friendly description
            );
            UnlockAllDLC = configUnlockAllDLC.Value;

            ConfigEntry<bool> configRevealAllLevels = configFile.Bind(
                "QualityOfLife", // Config Category
                "RevealAllLevels", // Config key name
                false, // Default Config value
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

            ConfigEntry<float> configServerTickRate = configFile.Bind(
                "Performance", // Config Category
                "ServerTickRate", // Config key name
                10.0f, // Default Config value
                "Server Tick Rate in Hz (If you are unsure as to what this is, do not touch!!!)" // Friendly description
            );
            ServerTickRate = configServerTickRate.Value;

            ConfigEntry<float> configServerTickRateUrgent = configFile.Bind(
                "Performance", // Config Category
                "ServerTickRateUrgent", // Config key name
                10.0f, // Default Config value
                "Server Tick Rate in Hz for 'urgent' synchronization requests (If you are unsure as to what this is, do not touch!!!)" // Friendly description
            );
            ServerTickRateUrgent = configServerTickRateUrgent.Value;

            ConfigEntry<float> configFixedDeltaTime = configFile.Bind(
                "Performance", // Config Category
                "FixedDeltaTime", // Config key name
                0.02f, // Default Config value
                "Physics Update Period in Seconds. Also the rate at which clients report their position to the server (If you are unsure as to what this is, do not touch!!!)" // Friendly description
            );
            FixedDeltaTime = configFixedDeltaTime.Value;

            ConfigEntry<bool> configFixDoubleServing = configFile.Bind(
                "Bugfixes", // Config Category
                "FixDoubleServing", // Config key name
                false, // Default Config value
                "Set to true to fix a bug which ruins competitive play" // Friendly description
            );
            FixDoubleServing = configFixDoubleServing.Value;

            ConfigEntry<bool> configFixSinkBug = configFile.Bind(
                "Bugfixes", // Config Category
                "FixSinkBug", // Config key name
                false, // Default Config value
                "Set to true to fix a bug where sinks can have reduced usability for the rest of the level" // Friendly description
            );
            FixSinkBug = configFixSinkBug.Value;

            ConfigEntry<bool> configFixControlStickThrowBug = configFile.Bind(
                "Bugfixes", // Config Category
                "FixControlStickThrowBug", // Config key name
                false, // Default Config value
                "Set to true to fix a bug where cancelling out of a platform control stick in a specific way would eat the next throw input" // Friendly description
            );
            FixControlStickThrowBug = configFixControlStickThrowBug.Value;

            ConfigEntry<bool> configFixEmptyBurnerThrow = configFile.Bind(
                "Bugfixes", // Config Category
                "FixEmptyBurnerThrow", // Config key name
                false, // Default Config value
                "Set to true to fix a bug where you cannot throw items when standing directly over a burner/mixer with no pan/bowl" // Friendly description
            );
            FixEmptyBurnerThrow = configFixEmptyBurnerThrow.Value;
        }

        public void UpdateConfig(string text)
        {
            // OC2Modding.Log.LogInfo($"Applying Config:\n{text}");

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
            try { if (config.HasKey("AlwaysPreserveCookingProgress"  )) AlwaysPreserveCookingProgress  = config["AlwaysPreserveCookingProgress"  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'AlwaysPreserveCookingProgress'"  ); }
            try { if (config.HasKey("SkipTutorialPopups"             )) SkipTutorialPopups             = config["SkipTutorialPopups"             ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorialPopups'"             ); }
            try { if (config.HasKey("TimerAlwaysStarts"              )) TimerAlwaysStarts              = config["TimerAlwaysStarts"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'TimerAlwaysStarts'"              ); }
            try { if (config.HasKey("UnlockAllChefs"                 )) UnlockAllChefs                 = config["UnlockAllChefs"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllChefs'"                 ); }
            try { if (config.HasKey("UnlockAllDLC"                   )) UnlockAllDLC                   = config["UnlockAllDLC"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'UnlockAllDLC'"                   ); }
            try { if (config.HasKey("RevealAllLevels"                )) RevealAllLevels                = config["RevealAllLevels"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'RevealAllLevels'"                ); }
            try { if (config.HasKey("PurchaseAllLevels"              )) PurchaseAllLevels              = config["PurchaseAllLevels"              ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'PurchaseAllLevels'"              ); }
            try { if (config.HasKey("SkipTutorial"                   )) SkipTutorial                   = config["SkipTutorial"                   ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SkipTutorial'"                   ); }
            try { if (config.HasKey("CheatsEnabled"                  )) CheatsEnabled                  = config["CheatsEnabled"                  ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CheatsEnabled'"                  ); }
            try { if (config.HasKey("SaveFolderName"                 )) SaveFolderName                 = config["SaveFolderName"                 ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'SaveFolderName'"                 ); }
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
            try { if (config.HasKey("DisableEarnHordeMoney"          )) DisableEarnHordeMoney          = config["DisableEarnHordeMoney"          ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableEarnHordeMoney'"          ); }
            try { if (config.HasKey("AggressiveHorde"                )) AggressiveHorde                = config["AggressiveHorde"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'AggressiveHorde'"                ); }
            try { if (config.HasKey("CustomOrderTimeoutPenalty"      )) CustomOrderTimeoutPenalty      = config["CustomOrderTimeoutPenalty"      ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'CustomOrderTimeoutPenalty'"      ); }
            try { if (config.HasKey("ItemIndex"                      )) ItemIndex                      = config["ItemIndex"                      ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'ItemIndex'"                      ); }
            try { if (config.HasKey("LevelTimerScale"                )) LevelTimerScale                = config["LevelTimerScale"                ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'LevelTimerScale'"                ); }
            try { if (config.HasKey("DisableArchipelagoLogin"        )) DisableArchipelagoLogin        = config["DisableArchipelagoLogin"        ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableArchipelagoLogin'"        ); }
            try { if (config.HasKey("ForbidDLC"                      )) ForbidDLC                      = config["ForbidDLC"                      ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'ForbidDLC'"                      ); }
            try { if (config.HasKey("ForceSingleSaveSlot"            )) ForceSingleSaveSlot            = config["ForceSingleSaveSlot"            ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'ForceSingleSaveSlot'"            ); }
            try { if (config.HasKey("DisableNGP"                     )) DisableNGP                     = config["DisableNGP"                     ]; } catch { OC2Modding.Log.LogWarning($"Failed to parse key 'DisableNGP'"                     ); }

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
                if (config.HasKey("PseudoSave"))
                {
                    Dictionary<int, int> tempDict = new Dictionary<int, int>();

                    foreach (KeyValuePair<string, SimpleJSON.JSONNode> kvp in config["PseudoSave"].AsObject)
                    {
                        tempDict.Add(int.Parse(kvp.Key), kvp.Value);
                    }

                    PseudoSave.Clear();
                    PseudoSave = tempDict;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'PseudoSave'");
            }

            try
            {
                if (config.HasKey("LeaderboardScoreScale"))
                {
                    var scales = new StarScales();
                    scales.OneStar    = config["LeaderboardScoreScale"]["OneStar"   ];
                    scales.TwoStars   = config["LeaderboardScoreScale"]["TwoStars"  ];
                    scales.ThreeStars = config["LeaderboardScoreScale"]["ThreeStars"];
                    scales.FourStars  = config["LeaderboardScoreScale"]["FourStars" ];

                    if (scales.OneStar > scales.TwoStars || scales.OneStar > scales.ThreeStars || scales.OneStar > scales.FourStars)
                    {
                        throw new System.Exception("Illegal star scaling");
                    }

                    LeaderboardScoreScale = scales;
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
                            dlcIdAndLevelId.DLC = kvp.Value.AsObject["DLC"];
                            dlcIdAndLevelId.dlc = OC2Helpers.DLCFromString(dlcIdAndLevelId.DLC);
                            dlcIdAndLevelId.LevelID = kvp.Value.AsObject["LevelID"];
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
                if (config.HasKey("LevelForceHide"))
                {
                    List<int> temp = new List<int>();

                    foreach (int levelId in config["LevelForceHide"].AsArray.Values)
                    {
                        temp.Add(levelId);
                    }

                    LevelForceHide.Clear();
                    LevelForceHide = temp;
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse key 'LevelForceHide'");
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
    }
    public static class OC2Config
    {
        public static ModConfig Config = new ModConfig();

        public static void Awake()
        {
            /* Initialize Memory */
            Config.LevelUnlockRequirements = new Dictionary<int, int>();
            Config.LevelPurchaseRequirements = new Dictionary<int, int>();
            Config.LevelForceReveal = new List<int>();
            Config.LevelForceHide = new List<int>();
            Config.LockedEmotes = new List<int>();

            InitConfig(false);

            Harmony.CreateAndPatchAll(typeof(OC2Config));
            Harmony.CreateAndPatchAll(typeof(DisableArcadeVersus));
        }

        public static void Update()
        {
            if (PendingFlushConfig)
            {
                PendingFlushConfig = false;
                FlushConfig(immediate: true);
            }
        }

        public static void InitConfig(bool newGame)
        {
            Update(); // flush config if needed

            // Everything gets update except for the SaveFolderName
            string oldSaveFolderName = Config.SaveFolderName;

            /* Initialize with standalone config */
            Config.InitConfigStandalone();

            /* short circuit if globally disabled */
            if (Config.DisableAllMods) return;

            /* Initialize using locally supplied configuration (optional) */
            InitJsonFile("OC2Modding.json");

            /* short circuit if globally disabled */
            if (Config.DisableAllMods) return;

            if (oldSaveFolderName != "")
            {
                Config.SaveFolderName = oldSaveFolderName;
            }

            if (Config.SaveFolderName != "")
            {
                /* Apply starting inventory */
                InitJsonFile(OC2Helpers.getCustomSaveDirectory() + "OC2Modding-INIT.json");

                /* Apply saved game inventory */
                if (!newGame)
                {
                    InitJsonFile(OC2Helpers.getCustomSaveDirectory() + "/OC2Modding.json");
                }
            }

            if (newGame)
            {
                Config.ItemIndex = 0; // When starting a new game, reset the remote items that have been received
                Config.PseudoSave.Clear(); // don't bring any completed levels over
                ArchipelagoClient.SendPseudoSave(); // Force an update of slot data (completed levels)
            }

            /* Ensure save game inventory is saved to disk */
            FlushConfig();
        }

        public static void UpdateConfig(string text)
        {
            Config.UpdateConfig(text);
        }

        public static void LevelCompleted(int level_id)
        {
            if (!Config.OnLevelCompleted.ContainsKey(level_id))
            {
                return;
            }

            foreach (ModConfig.OnLevelCompletedEvent e in Config.OnLevelCompleted[level_id])
            {
                try
                {
                    if (e.action == "SET_VALUE")
                    {
                        var tokens = e.payload.Split('=');
                        string payload = $"{{\"{tokens[0]}\":{tokens[1]}}}";
                        UpdateConfig(payload);
                    }
                    else if (e.action == "UNLOCK_LEVEL")
                    {
                        int id = Int32.Parse(e.payload);
                        if (!Config.LevelForceReveal.Contains(id))
                        {
                            Config.LevelForceReveal.Add(id);
                        }
                    }
                    else if (e.action == "UNLOCK_EMOTE")
                    {
                        int id = Int32.Parse(e.payload);
                        if (Config.LockedEmotes.Contains(id))
                        {
                            Config.LockedEmotes.Remove(id);
                        }
                    }
                    else if (e.action == "INC_TIP_COMBO")
                    {
                        if (Config.MaxTipCombo < 4)
                        {
                            Config.MaxTipCombo++;
                        }
                    }
                    else if (e.action == "INC_ORDERS_ON_SCREEN")
                    {
                        if (Config.MaxOrdersOnScreenOffset < 0)
                        {
                            Config.MaxOrdersOnScreenOffset++;
                        }
                    }
                    else if (e.action == "INC_STAR_COUNT")
                    {
                        int count = Int32.Parse(e.payload);
                        Config.StarOffset += count;
                    }
                    else if (e.action == "INC_DASH")
                    {
                        if (Config.DisableDash)
                        {
                            Config.DisableDash = false;
                        }
                        else
                        {
                            Config.WeakDash = false;
                        }
                    }
                    else if (e.action == "INC_THROW")
                    {
                        if (Config.DisableThrow)
                        {
                            Config.DisableThrow = false;
                        }
                        else
                        {
                            Config.DisableCatch = false;
                        }
                    }

                    if (e.message != "")
                    {
                        GameLog.LogMessage(e.message);
                    }
                }
                catch (Exception _e)
                {
                    OC2Modding.Log.LogError($"Failed to process post-complete event for level #{level_id}: action={e.action}, payload={e.payload}, message={e.message}\n{_e}");
                }
            }

            FlushConfig();
        }

        private static bool PendingFlushConfig = false;

        public static void FlushConfig(bool immediate = false)
        {
            if (!immediate)
            {
                PendingFlushConfig = true;
                return;
            }

            ThreadPool.QueueUserWorkItem((o) => FlushConfigTask());
        }

        private static Mutex FlushConfigMut = new Mutex();

        private static void FlushConfigTask()
        {
            FlushConfigMut.WaitOne();

            try
            {
                string save_dir = OC2Helpers.getCustomSaveDirectory();
                if (!Directory.Exists(save_dir))
                {
                    Directory.CreateDirectory(save_dir);
                }
                string filepath = save_dir + "/OC2Modding.json";
                string text = JsonConvert.SerializeObject(Config);
                File.WriteAllText(filepath, text);
                OC2Modding.Log.LogInfo($"Flushed config to '{filepath}'...");
            }
            catch
            {
                OC2Modding.Log.LogError("Failed to flush config");
            }

            FlushConfigMut.ReleaseMutex();
        }

        private static void InitJsonFile(string filename)
        {
            try
            {
                OC2Modding.Log.LogInfo($"Loading config from '{filename}'...");
                string json;
                using (StreamReader reader = new StreamReader(filename))
                {
                    json = reader.ReadToEnd();
                }

                UpdateConfig(json);
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse json from {filename}");
            }
        }

        /* In the event that the player presses "New Game" try your hardest to reset the configuration */
        [HarmonyPatch(typeof(SaveSlotElement), "SaveNewGame")]
        [HarmonyPostfix]
        private static void SaveNewGame()
        {
            InitConfig(true); // re-initialize without loading saved inventory state
        }

        private static void DisableExtraSaveSlots(ref SaveSlotElement[] ___m_saveElements)
        {
            if (!Config.ForceSingleSaveSlot)
            {
                return; // vanilla with QoL
            }

            /* Disable slots 2 and 3 */
            ___m_saveElements[1].gameObject.SetActive(false);
            ___m_saveElements[2].gameObject.SetActive(false);

            /* Disable slot 1 if the player is a guest */
            if (!OC2Helpers.IsHostPlayer()) // If this doesn't work, check `m_DontSaveButton == null`
            {
                ___m_saveElements[0].gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(SelectSaveDialog), "Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix(ref SaveSlotElement[] ___m_saveElements)
        {
            DisableExtraSaveSlots(ref ___m_saveElements);
        }

        [HarmonyPatch(typeof(SelectSaveDialog), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(ref SaveSlotElement[] ___m_saveElements)
        {
            DisableExtraSaveSlots(ref ___m_saveElements);
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsNGPEnabledForAnyLevel))]
        [HarmonyPostfix]
        private static void IsNGPEnabledForAnyLevel(ref bool __result)
        {
            if (Config.DisableNGP)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsNGPEnabledForLevel))]
        [HarmonyPostfix]
        private static void IsNGPEnabledForLevel(ref bool __result)
        {
            if (Config.DisableNGP)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(GameProgress), "CanUnlockNewGamePlusForChainEnd")]
        [HarmonyPostfix]
        private static void CanUnlockNewGamePlusForChainEnd(ref bool __result)
        {
            if (Config.DisableNGP)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(GameProgress), nameof(GameProgress.CanUnlockNewGamePlus))]
        [HarmonyPostfix]
        private static void CanUnlockNewGamePlus(ref bool __result)
        {
            if (Config.DisableNGP)
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(WorldMapKitchenLevelIconUI), nameof(WorldMapKitchenLevelIconUI.UpdateStarVisibility))]
        [HarmonyPrefix]
        private static void UpdateStarVisibility(ref GameProgress.GameProgressData.LevelProgress _levelProgress)
        {
            if (Config.DisableNGP)
            {
                _levelProgress.NGPEnabled = false;
            }
        }

        public static class DisableArcadeVersus
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(FrontendVersusTabOptions), nameof(FrontendVersusTabOptions.OnLocalPlayClicked));
                yield return AccessTools.Method(typeof(FrontendVersusTabOptions), nameof(FrontendVersusTabOptions.OnCouchPlayClicked));
                yield return AccessTools.Method(typeof(FrontendVersusTabOptions), nameof(FrontendVersusTabOptions.OnOnlinePrivateClicked));
                yield return AccessTools.Method(typeof(FrontendVersusTabOptions), nameof(FrontendVersusTabOptions.OnOnlinePublicClicked));
                yield return AccessTools.Method(typeof(FrontendCoopTabOptions), nameof(FrontendCoopTabOptions.OnLocalPlayClicked));
                yield return AccessTools.Method(typeof(FrontendCoopTabOptions), nameof(FrontendCoopTabOptions.OnCouchPlayClicked));
                yield return AccessTools.Method(typeof(FrontendCoopTabOptions), nameof(FrontendCoopTabOptions.OnOnlinePrivateClicked));
                yield return AccessTools.Method(typeof(FrontendCoopTabOptions), nameof(FrontendCoopTabOptions.OnOnlinePublicClicked));
            }

            [HarmonyPrefix]
            private static bool InterceptArcadeVersus()
            {
                if (Config.ForbidDLC)
                {
                    GameLog.LogMessage("Error: Arcade/Versus is not permitted when playing this mod");
                    return false;
                }

                return true;
            }
        }
    }
}
