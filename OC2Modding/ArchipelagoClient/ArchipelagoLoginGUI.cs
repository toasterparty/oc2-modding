using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace OC2Modding
{
    public static class ArchipelagoLoginGUI
    {
        public static bool ContinueWithoutArchipelago = false;

        public static bool Unlocked = false;
        private static bool ReachedTitleScreen = false;
        private static string StatusText = "";

        private static StartScreenFlow instance = null;
        private static GamepadUser gamepadUser = null;

        private static string serverUrl = "archipelago.gg";
        private static string userName = "";
        private static string password = "";

        private const float REFERENCE_WIDTH = 1920f;
        private const float REFERENCE_HEIGHT = 1080f;
        private const float LOGIN_UI_SCALE = 1.3f;

        private static Rect bgRect = new Rect();
        private static Rect bgRectMini = new Rect();
        private static Rect ScreenRect = new Rect();
        private static Rect statusRect = new Rect();
        private static Rect hostLabelRect = new Rect();
        private static Rect playerNameLabelRect = new Rect();
        private static Rect passwordLabelRect = new Rect();
        private static Rect serverFieldRect = new Rect();
        private static Rect userFieldRect = new Rect();
        private static Rect passwordFieldRect = new Rect();
        private static Rect connectButtonRect = new Rect();
        private static Rect connectingLabelRect = new Rect();
        private static Rect continueButtonRect = new Rect();

        private static GUIStyle labelStyle = null;
        private static GUIStyle textFieldStyle = null;
        private static GUIStyle buttonStyle = null;
        private static int currentFontSize = -1;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(ArchipelagoLoginGUI));

            if (OC2ModdingCache.cache.lastLoginHost != "")
            {
                serverUrl = OC2ModdingCache.cache.lastLoginHost;
                userName = OC2ModdingCache.cache.lastLoginPass;
                password = OC2ModdingCache.cache.lastLoginUser;
            }
        }

        public static void Update()
        {
            if (OC2Config.Config.DisableArchipelagoLogin)
            {
                ContinueWithoutArchipelago = true;
            }

            if (ArchipelagoClient.IsConnected || ContinueWithoutArchipelago)
            {
                Unlocked = true;
            }

            if (!Unlocked && ReachedTitleScreen && GameLog.isHidden)
            {
                GameLog.isHidden = false;
                GameLog.UpdateWindow();
            }
            else if (Unlocked && instance != null)
            {
                MethodInfo dynMethod = instance.GetType().GetMethod("OnEngagementFinished", BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(instance, new object[] { gamepadUser });
                instance = null;
                gamepadUser = null;
                GameLog.isHidden = true;
                GameLog.UpdateWindow();
            }
        }

        private static float ScaleX(float value)
        {
            return value * ((float)Screen.width / REFERENCE_WIDTH);
        }

        private static float ScaleY(float value)
        {
            return value * ((float)Screen.height / REFERENCE_HEIGHT);
        }

        private static Rect ScaledRect(float x, float y, float width, float height)
        {
            return new Rect(ScaleX(x), ScaleY(y), ScaleX(width), ScaleY(height));
        }

        private static Rect LoginRect(float x, float y, float width, float height)
        {
            const float anchorX = 5f;
            const float anchorY = 5f;

            float scaledX = anchorX + (x - anchorX) * LOGIN_UI_SCALE;
            float scaledY = anchorY + (y - anchorY) * LOGIN_UI_SCALE;
            float scaledWidth = width * LOGIN_UI_SCALE;
            float scaledHeight = height * LOGIN_UI_SCALE;

            return ScaledRect(scaledX, scaledY, scaledWidth, scaledHeight);
        }

        private static void UpdateTextStyles()
        {
            int desiredFontSize = Mathf.Max(12, Mathf.RoundToInt(ScaleY(16f)));
            if (desiredFontSize == currentFontSize && labelStyle != null && textFieldStyle != null && buttonStyle != null)
            {
                return;
            }

            currentFontSize = desiredFontSize;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = currentFontSize;

            textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.fontSize = currentFontSize;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = currentFontSize;
        }

        // TODO: call on client connect/disconnect
        public static void UpdateGUI()
        {
            StatusText = "Archipelago Status: ";
            if (ContinueWithoutArchipelago)
            {
                StatusText += "Skipped";
            }
            else
            {
                StatusText += $"Connected ";
                StatusText += OC2Helpers.IsHostPlayer() ? "[Host]" : "[Guest]";
            }

            bgRect = LoginRect(5f, 5f, 350f, 143f);
            bgRectMini = LoginRect(5f, 5f, 250f, 45f);
            ScreenRect = new Rect(0f, 0f, Screen.width, Screen.height);

            statusRect = LoginRect(16f, 16f, 300f, 20f);
            hostLabelRect = LoginRect(16f, 36f, 150f, 20f);
            playerNameLabelRect = LoginRect(16f, 56f, 150f, 20f);
            passwordLabelRect = LoginRect(16f, 76f, 150f, 20f);

            serverFieldRect = LoginRect(174f, 36f, 150f, 20f);
            userFieldRect = LoginRect(174f, 56f, 150f, 20f);
            passwordFieldRect = LoginRect(174f, 76f, 150f, 20f);

            connectButtonRect = LoginRect(17f, 110f, 100f, 30f);
            connectingLabelRect = LoginRect(17f, 115f, 110f, 30f);
            continueButtonRect = LoginRect(145f, 110f, 195f, 30f);
        }

        public static void OnGUI()
        {
            UpdateGUI();
            UpdateTextStyles();

            if (GameLog.isHidden || OC2Config.Config.DisableArchipelagoLogin)
            {
                return;
            }

            if (ArchipelagoClient.IsConnected || ContinueWithoutArchipelago)
            {
                GUI.Box(bgRectMini, "");
                GUI.Box(bgRectMini, "");
                GUI.Label(statusRect, StatusText, labelStyle);
                return;
            }

            if (!Unlocked)
            {
                GUI.Box(ScreenRect, ""); // Darken Screen
            }
            else
            {
                GUI.Box(bgRect, "");
            }

            GUI.Box(bgRect, "");

            GUI.Label(statusRect, "Archipelago Status: Not Connected", labelStyle);
            GUI.Label(hostLabelRect, "Host: ", labelStyle);
            GUI.Label(playerNameLabelRect, "Player Name: ", labelStyle);
            GUI.Label(passwordLabelRect, "Password: ", labelStyle);

            serverUrl = GUI.TextField(serverFieldRect, serverUrl, textFieldStyle);
            userName = GUI.TextField(userFieldRect, userName, textFieldStyle);
            password = GUI.TextField(passwordFieldRect, password, textFieldStyle);

            if (!ArchipelagoClient.IsConnecting)
            {
                if (GUI.Button(connectButtonRect, "Connect", buttonStyle))
                {
                    ArchipelagoClient.Connect(serverUrl, userName, password);
                }
            }
            else
            {
                GUI.Label(connectingLabelRect, "Connecting...", labelStyle);
            }

            if (!ContinueWithoutArchipelago && GUI.Button(continueButtonRect, "Continue Without Archipelago", buttonStyle))
            {
                ContinueWithoutArchipelago = true;
                GameLog.isHidden = true;
                GameLog.LogMessage("Continuing without Archipelago support...");
            }
        }

        [HarmonyPatch(typeof(StartScreenFlow), "OnEngagementFinished")]
        [HarmonyPrefix]
        private static bool OnEngagementFinished(ref StartScreenFlow __instance, ref GamepadUser _param1)
        {
            if (Unlocked)
            {
                return true;
            }

            if (!ReachedTitleScreen)
            {
                GameLog.LogMessage("Please either sign in to the Archipelago Multiworld Server or Continue Without Archipelago");
                ReachedTitleScreen = true;
                instance = __instance;
                gamepadUser = _param1;
            }

            return false;
        }
    }
}


