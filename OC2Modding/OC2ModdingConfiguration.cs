using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace OC2Modding
{
    public static class OC2Config
    {
        /* Globally Accessible Config Values */
        public static bool DisableAllMods;
        public static bool DisplayLeaderboardScores;
        public static bool AlwaysServeOldestOrder;
        public static float CustomOrderLifetime;
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

        public static void Awake()
        {
            InitCfg();
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
    }
}
