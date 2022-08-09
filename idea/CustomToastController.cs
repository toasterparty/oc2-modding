using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Team17.Online.Multiplayer.Messaging;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

namespace OC2Modding
{
    public static class CustomToastController
    {
        public static ClientDialogueController clientDialogueController = null;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomToastController));
        }

        public static void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                ClientDialogueController clientDialogueController = GameObject.FindObjectOfType<ClientDialogueController>();
                if (clientDialogueController == null)
                {
                    OC2Modding.Log.LogInfo("clientDialogueController is null");
                }
                else
                {
                    OC2Modding.Log.LogInfo("clientDialogueController is active!");
                }
            }
        }
    }
}
