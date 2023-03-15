using System.Collections.Generic;
using UnityEngine;

using HarmonyLib;

namespace OC2Modding
{
    public static class FixedRNG
    {
        private static OnScreenDebugDisplayRng onScreenDisplay;
        private static RngDebugDisplay rngDisplay = null;

        public static void Awake()
        {
            onScreenDisplay = new OnScreenDebugDisplayRng();
            onScreenDisplay.Awake();
            Harmony.CreateAndPatchAll(typeof(FixedRNG));
        }

        public static void Update()
        {
            onScreenDisplay.Update();

            if ((Input.GetKeyDown(KeyCode.End) || !OC2Config.Config.FixedMenuRNG) && rngDisplay != null)
            {
                onScreenDisplay.RemoveDisplay(rngDisplay);
                rngDisplay.OnDestroy();
                rngDisplay = null;
            } else if ((Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Minus)) && OC2Config.Config.FixedMenuRNG)
            {
                if (rngDisplay == null)
                {
                    rngDisplay = new RngDebugDisplay();
                    onScreenDisplay.AddDisplay(rngDisplay);
                }

                if (Input.GetKeyDown(KeyCode.Equals))
                {
                    rngDisplay.seed++;
                }

                if (Input.GetKeyDown(KeyCode.Minus))
                {
                    rngDisplay.seed--;
                }

                rngDisplay.OnUpdate();
            }
        }

        public static void OnGUI()
        {
            onScreenDisplay.OnGUI();
        }

        private static int order_offset = 0;
        private static int new_seed = 0;

        private static void ForceRNG()
        {
            if (rngDisplay == null) {
                return;
            }

            OC2Modding.Log.LogInfo($"Forced order using seed #{rngDisplay.seed}");
            new_seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(rngDisplay.seed + order_offset);
        }

        private static void RestoreRNG()
        {
            if (rngDisplay == null) {
                return;
            }

            // seed the rng using a seed determined from before we fixed RNG
            UnityEngine.Random.InitState(new_seed);
        }

        [HarmonyPatch(typeof(RoundData), nameof(RoundData.InitialiseRound))]
        [HarmonyPrefix]
        private static void RoundData_InitialiseRound()
        {
            order_offset = 0;
        }

        [HarmonyPatch(typeof(RoundData), nameof(RoundData.GetNextRecipe))]
        [HarmonyPrefix]
        private static void RoundData_GetNextRecipe_Prefix()
        {
            ForceRNG();
        }

        [HarmonyPatch(typeof(RoundData), nameof(RoundData.GetNextRecipe))]
        [HarmonyPostfix]
        private static void RoundData_GetNextRecipe_Postfix()
        {
            RestoreRNG();
        }

        [HarmonyPatch(typeof(DynamicRoundData), nameof(DynamicRoundData.InitialiseRound))]
        [HarmonyPrefix]
        private static void DynamicRoundData_InitialiseRound()
        {
            order_offset = 0;
        }

        [HarmonyPatch(typeof(DynamicRoundData), nameof(DynamicRoundData.GetNextRecipe))]
        [HarmonyPrefix]
        private static void DynamicRoundData_GetNextRecipe_Prefix()
        {
            ForceRNG();
        }

        [HarmonyPatch(typeof(DynamicRoundData), nameof(DynamicRoundData.GetNextRecipe))]
        [HarmonyPostfix]
        private static void DynamicRoundData_GetNextRecipe_Postfix()
        {
            RestoreRNG();
        }

        [HarmonyPatch(typeof(BossRoundData), nameof(BossRoundData.InitialiseRound))]
        [HarmonyPrefix]
        private static void BossRoundData_InitialiseRound()
        {
            order_offset = 0;
        }

        [HarmonyPatch(typeof(BossRoundData), nameof(BossRoundData.GetNextRecipe))]
        [HarmonyPrefix]
        private static void BossRoundData_GetNextRecipe_Prefix()
        {
            ForceRNG();
        }

        [HarmonyPatch(typeof(BossRoundData), nameof(BossRoundData.GetNextRecipe))]
        [HarmonyPostfix]
        private static void BossRoundData_GetNextRecipe_Postfix()
        {
            RestoreRNG();
        }

        /* Adapted from FPSCounter */
        private class RngDebugDisplay : DebugDisplay
        {
            public int seed = 0;

            public override void OnSetUp()
            {
                seed = 0;
            }

            public override void OnUpdate()
            {
                m_Text = $"Current RNG Seed {seed}";
            }

            public override void OnDraw(ref Rect rect, GUIStyle style)
            {
                base.DrawText(ref rect, style, m_Text);
            }

            private string m_Text = string.Empty;
        }

        /* Adapted from OnScreenDebugDisplay */
        private class OnScreenDebugDisplayRng
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
                this.m_GUIStyle.fontSize = (int)((float)Screen.height * 0.03f);
                this.m_GUIStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);
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
                Rect rect = new Rect(0f, 0f, (float)Screen.width, (float)this.m_GUIStyle.fontSize);
                for (int i = 0; i < this.m_Displays.Count; i++)
                {
                    this.m_Displays[i].OnDraw(ref rect, this.m_GUIStyle);
                }
            }

            private List<DebugDisplay> m_Displays;
            private GUIStyle m_GUIStyle;
        }
    }
}
