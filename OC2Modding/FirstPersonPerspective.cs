using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class FirstPersonPerspective
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(FirstPersonPerspective));
        }

        public static PlayerInputLookup.Player CurrentPlayer = PlayerInputLookup.Player.One;
        private static float LastUpdate = 0.0f;
        private static Vector3 Pos = new Vector3();
        private static Quaternion Rot = new Quaternion();

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "Update_Impl")]
        [HarmonyPostfix]
        private static void Update_Impl(ref GameObject ___m_controlObject, ref PlayerIDProvider ___m_playerIDProvider)
        {
            var prop = ___m_playerIDProvider.GetType().GetField("m_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var player = (PlayerInputLookup.Player)prop.GetValue(___m_playerIDProvider);

            if (player != CurrentPlayer)
            {
                return;
            }

            Pos = ___m_controlObject.transform.position;
            Rot = ___m_controlObject.transform.rotation;
            LastUpdate = Time.fixedTime;
        }

        [HarmonyPatch(typeof(MultiplayerCamera), "FixedUpdate")]
        [HarmonyPostfix]
        private static void FixedUpdate(ref MultiplayerCamera __instance)
        {
            if (Time.fixedTime - LastUpdate > 0.6f)
            {
                return; // no fresh camera data in a while, go back to normal
            }

            var pos = Pos;
            pos.y += 1.1f;
            __instance.gameObject.transform.position = pos;
            __instance.gameObject.transform.rotation = Rot;
        }
    }
}
