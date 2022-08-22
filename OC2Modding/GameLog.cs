using System;
using System.Collections;
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
        private static Rect buttonRect;
        private static GUIStyle textStyle = new GUIStyle();
        private static string scrollText = "";
        private static bool isHidden = true;
        private const int MAX_LOG_LINES = 20;

        public static void Awake()
        {
            UpdateWindow();
            Harmony.CreateAndPatchAll(typeof(GameLog)); // TODO call update on game resolution changed
        }

        public static void LogMessage(string logText)
        {
            if (logLines.Count == MAX_LOG_LINES)
            {
                logLines.RemoveAt(0);
            }
            logLines.Add(logText);
            UpdateWindow();
        }

        public static void OnGUI()
        {
            if (logLines.Count == 0)
            {
                return; // Do not display if nothing is logged
            }

            scrollViewVector = GUI.BeginScrollView(windowRect, scrollViewVector, scrollRect);
            GUI.Box(textRect, "");
            GUI.Box(textRect, scrollText, textStyle);
            GUI.EndScrollView();

            if (GUI.Button(buttonRect, isHidden ? "Show" : "Hide"))
            {
                isHidden = isHidden ? false : true;
                UpdateWindow();
            }
        }

        private static void UpdateWindow()
        {
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
                height = (int)((float)Screen.height*0.2f);
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
                textStyle.fontSize = (int)((float)Screen.height * 0.02f);
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

            buttonRect = new Rect((Screen.width / 2) + (width / 2) + (buttonWidth / 3), Screen.height*0.004f, buttonWidth, buttonHeight);
        }
    }
}
