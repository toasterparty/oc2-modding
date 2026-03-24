using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC2Modding
{
    public static class ArchipelagoCommandGUI
    {
        public static bool HasPendingCommand
        {
            get
            {
                return CommandText != "";
            }
        }

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

        private static bool IsEnterEvent(Event currentEvent)
        {
            return currentEvent != null
                && (currentEvent.type == EventType.KeyDown || currentEvent.type == EventType.KeyUp)
                && (currentEvent.keyCode == KeyCode.Return
                    || currentEvent.keyCode == KeyCode.KeypadEnter
                    || currentEvent.character == '\n'
                    || currentEvent.character == '\r');
        }

        private static void UpdateTextStyles()
        {
            int desiredFontSize = Mathf.Max(16, Mathf.RoundToInt((float)Screen.height * 0.020f));
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
            int safeMarginY = Mathf.RoundToInt((float)Screen.height * 0.01f);

            int width = GameLog.GetSharedOverlayWidth();

            int xPos = (Screen.width - width) / 2;
            int yPos = Mathf.RoundToInt((float)Screen.height * 0.34f);

            int height = Mathf.RoundToInt((float)Screen.height * 0.033f);
            height = Mathf.Max(34, height);

            CommandTextRect = new Rect(xPos, yPos, width, height);

            int sendButtonWidth = Mathf.RoundToInt((float)Screen.width * 0.065f);
            sendButtonWidth = Mathf.Clamp(sendButtonWidth, 96, 180);

            yPos += height + safeMarginY;
            yPos = Mathf.Min(yPos, Screen.height - safeMarginY - height);
            SendButtonRect = new Rect(xPos, yPos, sendButtonWidth, height);
        }

        public static void OnGUI()
        {
            if (!ShouldShow)
            {
                return;
            }

            UpdateTextStyles();

            CommandText = GUI.TextField(CommandTextRect, CommandText, textFieldStyle);

            Event currentEvent = Event.current;
            bool pressedEnter = IsEnterEvent(currentEvent) && currentEvent.type == EventType.KeyUp;
            bool shouldSend = CommandText != "" && (GUI.Button(SendButtonRect, "Send", buttonStyle) || pressedEnter);

            if (shouldSend)
            {
                ArchipelagoClient.SendMessage(CommandText);
                CommandText = "";

                if (pressedEnter)
                {
                    currentEvent.Use();
                }
            }
        }
    }
}


