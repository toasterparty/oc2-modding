using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace OC2Modding
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Overcooked2.exe")]
    public class OC2Modding : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static ConfigFile configFile;
        private static ConfigEntry<bool> configDisableAllMods;

        private void Awake()
        {
            /* Setup Logging */
            OC2Modding.Log = base.Logger;
            OC2Modding.Log.LogInfo($"OC2Modding is alive!");

            /* Initialize Configuration */
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "OC2Modding.cfg"), true);
            configDisableAllMods = OC2Modding.configFile.Bind(
                "_DisableAllMods_",
                "DisableAllMods",
                false,
                "Set to true to completely return the game back to it's original state"
            );

            if (configDisableAllMods.Value)
            {
                OC2Modding.Log.LogInfo($"All mods DISABLED!");
                return;
            }

            /* Inject Mods */
            FixBugs.Awake();
            UnlockAllChefs.Awake();
            UnlockAllLevels.Awake();
            TimerAlwaysStarts.Awake();
            SkipTutorialPopups.Awake();
            PreserveCookProgress.Awake();
        }
    }
}
