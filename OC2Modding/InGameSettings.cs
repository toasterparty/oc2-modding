using System;

using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class InGameSettings
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(InGameSettings));
        }

        [HarmonyPatch(typeof(FrontendMenuBehaviour), nameof(FrontendMenuBehaviour.Show))]
        [HarmonyPostfix]
        private static void FrontendMenuBehaviour_Show(ref FrontendMenuBehaviour __instance, ref GameObject parent, ref bool __result)
        {
            string parent_name = parent?.name ?? "NULL";
            string instance_name = __instance?.name ?? "NULL";

            if (!__result || parent_name != "RootTab" || instance_name != "SettingsOptions")
            {
                return;
            }

            // for (int i = 0; i < (componentsInChildren?.Length ?? 0); i++)
            // {
            //     var name = componentsInChildren[i].name;
            //     var obj = componentsInChildren[i];
            //     GameLog.LogMessage($"{name}");
            // }

            Animator[] componentsInChildren = __instance.gameObject.GetComponentsInChildren<Animator>(true);
            if (componentsInChildren.Length > 4)
            {
                return;
            }

            var child = componentsInChildren[1];
            var newChild = GameObject.Instantiate(child);
            newChild.transform.SetParent(child.transform.parent);
            newChild.name = "Mod Settings";
            newChild.transform.SetSiblingIndex(0); // Mod Settings go first in list

            // var button = newChild.gameObject.RequestComponent<T17Button>();
            // var textInfo = button.GetType().GetField("m_ButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // var text = (T17Text)textInfo.GetValue(button);
            // text.text = "TEST";
            // textInfo.SetValue(button, text);
        }
    }
}
