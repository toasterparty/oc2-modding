using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace OC2Modding
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Overcooked2.exe")]
    public class OC2Modding : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            /* Setup Logging */
            OC2Modding.Log = base.Logger;
            OC2Modding.Log.LogInfo($"OC2Modding is alive!");

            /* Initialize Configuration */
            OC2Config.Awake();
            if (OC2Config.Config.DisableAllMods) {
                OC2Modding.Log.LogInfo($"All mods DISABLED!");
                return;
            }

            /* Inject Mods */
            OC2Helpers.Awake(); // This MUST go first

            // Architecture Mods
            GameLog.Awake();
            OC2ModdingCache.Awake();
            CustomSaveDirectory.Awake();
            ArchipelagoLoginGUI.Awake();
            ArchipelagoCommandGUI.Awake();
            CoopHandshake.Awake();
            OnLevelCompleted.Awake();
            LevelProgression.Awake();
            CustomLevelOrder.Awake();
            
            // Visual Mods
            LeaderboardMod.Awake();
            DisplayFPS.Awake();
            
            // Gameplay Mods
            FixBugs.Awake();
            UnlockAllChefs.Awake();
            UnlockAllDLC.Awake();
            TimerAlwaysStarts.Awake();
            SkipTutorialPopups.Awake();
            PreserveCookProgress.Awake();
            CustomOrderLifetime.Awake();
            AlwaysServeOldestOrder.Awake();
            Cheats.Awake();
            IngredientCrates.Awake();
            Nerfs.Awake();
            AggressiveHorde.Awake();
            CustomOrderTimeoutPenalty.Awake();
            IncreasedKnockback.Awake();
            ServerTickRate.Awake();

            DisplayModsOnResultsScreen.Awake();
        }

        private void Update()
        {
            if (OC2Config.Config.DisableAllMods) return;
            OC2Config.Update();
            OC2ModdingCache.Update();
            LeaderboardMod.Update();
            DisplayFPS.Update();
            DisplayModsOnResultsScreen.Update();
            Cheats.Update();
            AutoCompleteLevel.Update();
            ArchipelagoClient.Update();
            ArchipelagoLoginGUI.Update();
        }

        private void FixedUpdate()
        {
            Time.fixedDeltaTime = OC2Config.Config.FixedDeltaTime;
        }

        private void OnGUI()
        {
            if (OC2Config.Config.DisableAllMods) return;
            DisplayFPS.OnGUI();
            DisplayModsOnResultsScreen.OnGUI();
            GameLog.OnGUI();
            ArchipelagoLoginGUI.OnGUI();
            ArchipelagoCommandGUI.OnGUI();
        }
    }
}
