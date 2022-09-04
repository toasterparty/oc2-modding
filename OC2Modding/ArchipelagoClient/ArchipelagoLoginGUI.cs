using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class ArchipelagoLoginGUI
    {
        public static bool ContinueWithoutArchipelago = false;

        private static bool Unlocked = false;
        private static bool ReachedTitleScreen = false;

        private static string serverUrl = "archipelago.gg";
        private static string userName = "";
        private static string password = "";

        private static Rect bgRect = new Rect(5, 5, 350, 143);
        private static Rect bgRectMini = new Rect(5, 5, 250, 45);

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(ArchipelagoLoginGUI));
        }

        public static void Update()
        {
            if (ArchipelagoClient.IsConnected || ContinueWithoutArchipelago)
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

            GUI.Box(bgRect, "");
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

        [HarmonyPatch(typeof(StartScreenFlow), "Update")]
        [HarmonyPrefix]
        private static bool StartScreenFlow_Update(ref StartScreenFlow __instance)
        {
            if (Unlocked)
            {
                return true;
            }

            if (!ReachedTitleScreen)
            {
                GameLog.LogMessage("Please either sign in to the Archipelago Multiworld Server, or Continue Without Archipelago");
                ReachedTitleScreen = true;
            }
            return false;
        }
    }
}
