using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC2Modding
{
    public static class ArchipelagoCommandGUI
    {
        private static bool ShouldShow
        {
            get
            {
                return !GameLog.isHidden && !OC2Config.Config.DisableArchipelagoLogin && ArchipelagoClient.IsConnected;
            }
        }

        public static void Awake()
        {
            UpdateGUI();
        }

        private static string CommandText = "!help";
        private static Rect CommandTextRect = new Rect();
        private static Rect SendButtonRect = new Rect();

        private static GUIStyle textFieldStyle = null;
        private static GUIStyle buttonStyle = null;
        private static int currentFontSize = -1;

        private static void UpdateTextStyles()
        {
            int desiredFontSize = Mathf.Max(12, Mathf.RoundToInt((float)Screen.height * 0.016f));
            if (desiredFontSize == currentFontSize && textFieldStyle != null && buttonStyle != null)
            {
                return;
            }

            currentFontSize = desiredFontSize;

            textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.fontSize = currentFontSize;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = currentFontSize;
        }

        public static void UpdateGUI()
        {
            int width = (int)((float)Screen.width * 0.4f);
            int yPos = (int)((float)Screen.height * 0.307f);
            int xPos = (int)(((float)Screen.width / 2.0f) - ((float)width / 2.0f));
            int height = (int)((float)Screen.height * 0.022f);

            CommandTextRect = new Rect(xPos, yPos, width, height);

            width = (int)((float)Screen.width * 0.035f);
            yPos += (int)((float)Screen.height * 0.03f);
            SendButtonRect = new Rect(xPos, yPos, width, height);
        }

        public static void OnGUI()
        {
            if (!ShouldShow)
            {
                return;
            }

            UpdateTextStyles();

            CommandText = GUI.TextField(CommandTextRect, CommandText, textFieldStyle);
            if (CommandText != "" && GUI.Button(SendButtonRect, "Send", buttonStyle))
            {
                ArchipelagoClient.SendMessage(CommandText);
                CommandText = "";
            }
        }
    }
}

