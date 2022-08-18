using BepInEx;
using BepInEx.Logging;

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
            if (OC2Config.DisableAllMods) return;

            /* Inject Mods */
            OC2Helpers.Awake(); // This MUST go first

            FixBugs.Awake();
            UnlockAllChefs.Awake();
            LevelProgression.Awake();
            UnlockAllDLC.Awake();
            TimerAlwaysStarts.Awake();
            SkipTutorialPopups.Awake();
            PreserveCookProgress.Awake();
            LeaderboardMod.Awake();
            DisplayFPS.Awake();
            CustomOrderLifetime.Awake();
            AlwaysServeOldestOrder.Awake();
            Cheats.Awake();
            CustomLevelOrder.Awake();
            IngredientCrates.Awake();

            DisplayModsOnResultsScreen.Awake(); // This MUST go last
        }

        private void Update()
        {
            if (OC2Config.DisableAllMods) return;
            LeaderboardMod.Update();
            DisplayFPS.Update();
            DisplayModsOnResultsScreen.Update();
            Cheats.Update();
        }

        private void OnGUI()
        {
            if (OC2Config.DisableAllMods) return;
            DisplayFPS.OnGUI();
            DisplayModsOnResultsScreen.OnGUI();
        }
    }
}
