using System.Collections.Generic;
using UnityEngine;

namespace OC2Modding
{
    public static class DisplayFPS
    {
        private static MyOnScreenDebugDisplayFPS onScreenDebugDisplayFPS;
        private static MyFPSCounter fPSCounter = null;

        public static void Awake()
        {
            onScreenDebugDisplayFPS = new MyOnScreenDebugDisplayFPS();
            onScreenDebugDisplayFPS.Awake();
        }

        public static void Update()
        {
            onScreenDebugDisplayFPS.Update();

            if ((!OC2Config.Config.DisplayFPS || Input.GetKeyDown(KeyCode.End)) && fPSCounter != null)
            {
                onScreenDebugDisplayFPS.RemoveDisplay(fPSCounter);
                fPSCounter.OnDestroy();
                fPSCounter = null;
            } else if (Input.GetKeyDown(KeyCode.Home) && fPSCounter == null)
            {
                fPSCounter = new MyFPSCounter();
                onScreenDebugDisplayFPS.AddDisplay(fPSCounter);
            }

            // if (Input.GetKeyDown(KeyCode.Insert))
            // {
            //     OC2Modding.Log.LogInfo($"We are in level={GameUtils.GetLevelID()}");
            // }
        }

        public static void OnGUI()
        {
            onScreenDebugDisplayFPS.OnGUI();
        }

        /* Adapted from FPSCounter */
        private class MyFPSCounter : DebugDisplay
        {
            public override void OnSetUp()
            {
                this.m_FPSCounter = new FPS_No_String_Allocs();
            }

            public override void OnUpdate()
            {
                this.m_FPSCounter.Update();
                float checkTime = Time.time;
                if (checkTime - m_LastDisplayTime > 0.25f)
                {
                    m_LastDisplayTime = checkTime;
                    m_Text = string.Format("FPS: {0:F1}", this.m_FPSCounter.AverageFPS());
                }
            }

            public override void OnDraw(ref Rect rect, GUIStyle style)
            {
                base.DrawText(ref rect, style, m_Text);
            }

            private string m_Text = string.Empty;
            private float m_LastDisplayTime = 0;
            private FPS_No_String_Allocs m_FPSCounter;
        }

        /* Adapted from OnScreenDebugDisplay */
        private class MyOnScreenDebugDisplayFPS
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
