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

        private static StartScreenFlow instance = null;
        private static GamepadUser gamepadUser = null;

        private static string serverUrl = "archipelago.gg";
        private static string userName = "";
        private static string password = "";

        private static Rect bgRect = new Rect(5, 5, 350, 143);
        private static Rect bgRectMini = new Rect(5, 5, 250, 45);
        private static Rect ScreenRect = new Rect(0, 0, Screen.width, Screen.height);

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(ArchipelagoLoginGUI));
        }

        public static void Update()
        {
            if (Unlocked)
            {
                if (instance != null)
                {
                    MethodInfo dynMethod = instance.GetType().GetMethod("OnEngagementFinished", BindingFlags.NonPublic | BindingFlags.Instance);
                    dynMethod.Invoke(instance, new object[] { gamepadUser });
                    instance = null;
                    gamepadUser = null;
                }

                return;
            }

            if (ArchipelagoClient.IsConnected)
            {
                // TODO: One-time archipelago setup could be done here
                //       It could also be part of the async connect task
                Unlocked = true;
            }
            else if (ContinueWithoutArchipelago)
            {
                Unlocked = true;
            }

            if (!Unlocked && ReachedTitleScreen && GameLog.isHidden)
            {
                GameLog.isHidden = false;
                GameLog.UpdateWindow();
            }
        }

        public static void OnGUI()
        {
            if (GameLog.isHidden)
            {
                return;
            }

            if (ArchipelagoClient.IsConnected)
            {
                GUI.Box(bgRectMini, "");
                GUI.Box(bgRectMini, "");
                GUI.Label(new Rect(16, 16, 300, 20), "Archipelago Status: Connected");
                return;
            }

            if (ContinueWithoutArchipelago)
            {
                GUI.Box(bgRectMini, "");
                GUI.Box(bgRectMini, "");
                GUI.Label(new Rect(16, 16, 300, 20), "Archipelago Status: Skipped");
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

            GUI.Label(new Rect(16, 16, 300, 20), "Archipelago Status: Not Connected");
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
                if (GUI.Button(new Rect(17, 110, 100, 30), "Connect"))
                {
                    ArchipelagoClient.Connect(serverUrl, userName, password);
                }
            }
            else
            {
                GUI.Label(new Rect(17, 115, 110, 30), "Connecting...");
            }

            if (!ContinueWithoutArchipelago && GUI.Button(new Rect(145, 110, 195, 30), "Continue Without Archipelago"))
            {
                ContinueWithoutArchipelago = true;
                GameLog.isHidden = true;
                GameLog.UpdateWindow();
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
