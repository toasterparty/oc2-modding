using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using BitStream;

namespace OC2Modding
{
    public static class DisplayModsOnResultsScreen
    {
        private static MyOnScreenDebugDisplay onScreenDebugDisplay;
        private static ModsDisplay modsDisplay = null;
        private static bool shouldDisplay = false;

        public static void Awake()
        {
            /* Setup */
            onScreenDebugDisplay = new MyOnScreenDebugDisplay();
            onScreenDebugDisplay.Awake();
            modsDisplay = new ModsDisplay();
            onScreenDebugDisplay.AddDisplay(modsDisplay);

            modsDisplay.m_Text = $"OC2 Modding v{PluginInfo.PLUGIN_VERSION}";

            if (OC2Config.TimerAlwaysStarts)
            {
                modsDisplay.m_Text += "\nTimer Always Starts";
            }
            if (OC2Config.PreserveCookingProgress)
            {
                modsDisplay.m_Text += "\nPreserve Cooking Progress";
            }
            if (OC2Config.CustomOrderLifetime != 100.0)
            {
                modsDisplay.m_Text += $"\n{(int)OC2Config.CustomOrderLifetime}s Order Lifetime";
            }
            if (OC2Config.AlwaysServeOldestOrder)
            {
                modsDisplay.m_Text += "\nAlways Serve Oldest Order";
            }
            if (OC2Config.FixDoubleServing)
            {
                modsDisplay.m_Text += "\nDouble Servering Bugfix";
            }
            if (OC2Config.FixSinkBug)
            {
                modsDisplay.m_Text += "\nSink Bugfix";
            }
            if (OC2Config.FixEmptyBurnerThrow)
            {
                modsDisplay.m_Text += "\nEmpty Burner/Mixer Throw Bugfix";
            }
            if (OC2Config.FixControlStickThrowBug)
            {
                modsDisplay.m_Text += "\nControl Stick Throw Cooldown Bugfix";
            }

            /* Inject Mod */
            Harmony.CreateAndPatchAll(typeof(DisplayModsOnResultsScreen));
        }

        public static void Update()
        {
            onScreenDebugDisplay.Update();
        }

        public static void OnGUI()
        {
            onScreenDebugDisplay.OnGUI();
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
                this.m_GUIStyle.fontSize = (int)((float)Screen.height * 0.02f);
                this.m_GUIStyle.normal.textColor = new Color(0.91f, 0.426f, 0.074f, 1f);
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
                Rect rect = new Rect(0f, (float)Screen.height * 0.65f, (float)Screen.width * 0.99f, (float)this.m_GUIStyle.fontSize);
                for (int i = 0; i < this.m_Displays.Count; i++)
                {
                    this.m_Displays[i].OnDraw(ref rect, this.m_GUIStyle);
                }
            }

            private List<DebugDisplay> m_Displays;
            private GUIStyle m_GUIStyle;
        }

        public class ModsDisplay : DebugDisplay
        {
            public override void OnSetUp()
            {
            }

            public override void OnUpdate()
            {
            }

            public override void OnDraw(ref Rect rect, GUIStyle style)
            {
                if (shouldDisplay)
                {
                    base.DrawText(ref rect, style, this.m_Text);
                }
            }

            public string m_Text = string.Empty;
        }

        public interface Serialisable
        {
            void Serialise(BitStreamWriter writer);
            bool Deserialise(BitStreamReader reader);
        }

        [HarmonyPatch(typeof(GameModes.ClientCampaignMode), "OnOutro")]
        [HarmonyPostfix]
        private static void OnOutro()
        {
            shouldDisplay = true;
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            shouldDisplay = false;
        }
    }
}
