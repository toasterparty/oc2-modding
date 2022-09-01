using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Linq;

namespace OC2Modding
{
    public static class GameLog
    {
        private static List<string> logLines = new List<string>();
        private static Vector2 scrollViewVector;
        private static Rect windowRect;
        private static Rect scrollRect;
        private static Rect textRect;
        private static Rect hideShowbuttonRect;
        private static Rect autoCompleteButtonRect;
        private static string autoCompleteButtonText = "";
        public enum AutoCompleteMode
        {
            AUTO_COMPLETE_DISABLED = 0,
            AUTO_COMPLETE_ONE_STAR,
            AUTO_COMPLETE_TWO_STAR,
            AUTO_COMPLETE_THREE_STAR,
            AUTO_COMPLETE_FOUR_STAR,
        }
        public static AutoCompleteMode autoCompleteMode = AutoCompleteMode.AUTO_COMPLETE_DISABLED;
        private static GUIStyle textStyle = new GUIStyle();
        private static string scrollText = "";
        private static bool isHidden = true;
        private static float lastUpdateTime = Time.time;
        private const int MAX_LOG_LINES = 40;
        private const float HIDDEN_TIMEOUT_S = 15f;

        static string serverUrl = "archipelago.gg";
        static string userName = "";
        static string password = "";

        public static void Awake()
        {
            UpdateWindow();
            Harmony.CreateAndPatchAll(typeof(GameLog)); // TODO call update on game resolution changed
            LogMessage($"OC2Modding v{PluginInfo.PLUGIN_VERSION} started");
        }

        public static void LogMessage(string logText)
        {
            if (logLines.Count == MAX_LOG_LINES)
            {
                logLines.RemoveAt(0);
            }
            logLines.Add(logText);
            lastUpdateTime = Time.time;
            UpdateWindow();
        }

        public static void OnGUI()
        {
            if (logLines.Count == 0)
            {
                return; // Do not display if nothing is logged
            }

            if (!isHidden || Time.time - lastUpdateTime < HIDDEN_TIMEOUT_S)
            {
                scrollViewVector = GUI.BeginScrollView(windowRect, scrollViewVector, scrollRect);
                GUI.Box(textRect, "");
                GUI.Box(textRect, scrollText, textStyle);
                GUI.EndScrollView();
            }

            if (GUI.Button(hideShowbuttonRect, isHidden ? "Show" : "Hide"))
            {
                isHidden = isHidden ? false : true;
                UpdateWindow();
            }

            if (!isHidden && GUI.Button(autoCompleteButtonRect, autoCompleteButtonText))
            {
                switch (autoCompleteMode)
                {
                    case AutoCompleteMode.AUTO_COMPLETE_DISABLED:
                    {
                        autoCompleteMode = AutoCompleteMode.AUTO_COMPLETE_ONE_STAR;
                        break;
                    }
                    case AutoCompleteMode.AUTO_COMPLETE_ONE_STAR:
                    {
                        autoCompleteMode = AutoCompleteMode.AUTO_COMPLETE_TWO_STAR;
                        break;
                    }
                    case AutoCompleteMode.AUTO_COMPLETE_TWO_STAR:
                    {
                        autoCompleteMode = AutoCompleteMode.AUTO_COMPLETE_THREE_STAR;
                        break;
                    }
                    case AutoCompleteMode.AUTO_COMPLETE_THREE_STAR:
                    {
                        autoCompleteMode = AutoCompleteMode.AUTO_COMPLETE_DISABLED;
                        break;
                    }
                    // case AutoCompleteMode.AUTO_COMPLETE_FOUR_STAR:
                    // {
                    //     autoCompleteButtonText = "Auto-Complete (4-Star)";
                    //     break;
                    // }
                }

                UpdateWindow();
            }

            
            if (!isHidden)
            {
                if (ArchipelagoClient.IsConnected)
                {
                    GUI.Label(new Rect(16, 16, 300, 20), "Archipelago v0.3.4 Status: Connected");
                }
                else
                {
                    GUI.Label(new Rect(16, 16, 300, 20), "Archipelago v0.3.4 Status: Not Connected");
                    GUI.Label(new Rect(16, 36, 150, 20), "Host: ");
                    GUI.Label(new Rect(16, 56, 150, 20), "Player Name: ");
                    GUI.Label(new Rect(16, 76, 150, 20), "Password: ");

                    serverUrl =
                        GUI.TextField(new Rect(150 + 16 + 8, 36, 150, 20), serverUrl);
                    userName =
                        GUI.TextField(new Rect(150 + 16 + 8, 56, 150, 20), userName);
                    password =
                        GUI.TextField(new Rect(150 + 16 + 8, 76, 150, 20), password);

                    if (!ArchipelagoClient.IsConnecting)
                    {
                        if (GUI.Button(new Rect(16, 96, 100, 20), "Connect"))
                        {
                            ArchipelagoClient.Connect(serverUrl, userName, password);
                        }
                    }
                    else
                    {
                        GUI.Label(new Rect(16, 96, 100, 20), "Connecting...");
                    }
                }
            }
        }

        private static void UpdateWindow()
        {
            if (!isHidden)
            {
                lastUpdateTime = 0;
            }

            scrollText = "";

            if (isHidden)
            {
                if (logLines.Count > 0)
                {
                    scrollText = logLines[logLines.Count-1];
                }
            }
            else
            {
                for (int i = 0; i < logLines.Count; i++)
                {
                    scrollText += "> ";
                    scrollText += logLines.ElementAt(i);
                    if (i < logLines.Count-1)
                    {
                        scrollText += $"\n\n";
                    }
                }
            }

            int width = (int)((float)Screen.width*0.4f);
            int height;
            int scrollDepth;

            if (isHidden)
            {
                height = (int)((float)Screen.height*0.03f);
                scrollDepth = height;
            }
            else
            {
                height = (int)((float)Screen.height*0.4f);
                scrollDepth = height*10;
            }

            windowRect = new Rect((Screen.width / 2) - (width / 2), 0, width, height);
            scrollRect = new Rect(0, 0, width * 0.9f, scrollDepth);
            scrollViewVector = new Vector2(0.0f, scrollDepth);
            textRect = new Rect(0, 0, width, scrollDepth);

            textStyle.alignment = TextAnchor.LowerLeft;
            if (isHidden)
            {
                textStyle.fontSize = (int)((float)Screen.height * 0.0165f);
            }
            else
            {
                textStyle.fontSize = (int)((float)Screen.height * 0.0185f);
            }
            textStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            textStyle.wordWrap = !isHidden;

            int xPadding = (int)((float)Screen.width*0.01f);
            int yPadding = (int)((float)Screen.height*0.01f);
            if(isHidden)
            {
                textStyle.padding = new RectOffset(xPadding/2, xPadding/2, yPadding/2, yPadding/2);
            }
            else
            {
                textStyle.padding = new RectOffset(xPadding, xPadding, yPadding, yPadding);
            }

            int buttonWidth = (int)((float)Screen.width*0.03f);
            int buttonHeight = (int)((float)Screen.height*0.03f);

            hideShowbuttonRect = new Rect((Screen.width / 2) + (width / 2) + (buttonWidth / 3), Screen.height*0.004f, buttonWidth, buttonHeight);

            buttonWidth = (int)((float)Screen.width*0.12f);
            buttonHeight = (int)((float)Screen.height*0.03f);
            autoCompleteButtonRect = new Rect((Screen.width / 2) + (width / 2) + (buttonWidth / 2), Screen.height*0.010f, buttonWidth, buttonHeight);
            switch (autoCompleteMode)
            {
                case AutoCompleteMode.AUTO_COMPLETE_DISABLED:
                {
                    autoCompleteButtonText = "Auto-Complete Disabled";
                    break;
                }
                case AutoCompleteMode.AUTO_COMPLETE_ONE_STAR:
                {
                    autoCompleteButtonText = "Auto-Complete (1-Star)";
                    break;
                }
                case AutoCompleteMode.AUTO_COMPLETE_TWO_STAR:
                {
                    autoCompleteButtonText = "Auto-Complete (2-Star)";
                    break;
                }
                case AutoCompleteMode.AUTO_COMPLETE_THREE_STAR:
                {
                    autoCompleteButtonText = "Auto-Complete (3-Star)";
                    break;
                }
                case AutoCompleteMode.AUTO_COMPLETE_FOUR_STAR:
                {
                    autoCompleteButtonText = "Auto-Complete (4-Star)";
                    break;
                }
            }
        }
    }
}
