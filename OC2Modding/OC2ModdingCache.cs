using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using HarmonyLib;

namespace OC2Modding
{
    public static class OC2ModdingCache
    {
        public class Cache
        {
            public string lastLoginHost = "";
            public string lastLoginUser = "";
            public string lastLoginPass = "";
            public int lastResolution = -1;
            public int lastVSync = -1;
            public int lastWindowed = -1;
            public int lastQuality = -1;
            public int lastMusicVolume = -1;
            public int lastSfxVolume = -1;
            public Dictionary<string, Dictionary<string, string>> bindings = new Dictionary<string, Dictionary<string, string>>();
        }

        public static Cache cache = new Cache();

        public static void Awake()
        {
            Read();
            Harmony.CreateAndPatchAll(typeof(OC2ModdingCache));
            Harmony.CreateAndPatchAll(typeof(GlobalSave_Get_Patch));
        }

        private static bool applied = false;

        public static void Update()
        {
            if (!applied) {
                ApplyCachedVolumes();
            }

            if (FlushPending) {
                DoFlush();
            }
        }

        private static float VolumeTodB(int vol)
        {
            return (80.0f * ((float)vol)/10.0f) - 80.0f;
        }

        private static void ApplyCachedVolumes()
        {
            var audioManager = GameUtils.RequireManager<AudioManager>();
            if (audioManager == null)
            {
                return;
            }

            var mixer = audioManager.m_audioMixer;
            if (mixer == null)
            {
                return;
            }

            if (cache.lastMusicVolume != -1)
            {
                OC2Modding.Log.LogInfo($"Set music volume to {cache.lastMusicVolume}");
                mixer.SetFloat("MusicVolume", VolumeTodB(cache.lastMusicVolume));
            }

            if (cache.lastSfxVolume != -1)
            {
                OC2Modding.Log.LogInfo($"Set sfx volume to {cache.lastSfxVolume}");
                mixer.SetFloat("SFXVolume", VolumeTodB(cache.lastSfxVolume));
            }

            applied = true;
        }

        private static void SetOptionHelper(ref IOption[] ___m_options, int option, int value)
        {
            if (___m_options[option] != null && value != -1)
            {
                ___m_options[option].SetOption(value);
                ___m_options[option].Commit();
            }
        }

        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.LoadFromSave))]
        [HarmonyPostfix]
        private static void LoadFromSave(ref IOption[] ___m_options)
        {
            // This is called when saving/loading settings
            SetOptionHelper(ref ___m_options, 4, cache.lastMusicVolume);
            SetOptionHelper(ref ___m_options, 5, cache.lastSfxVolume);
        }

        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.AddToSave))]
        [HarmonyPostfix]
        private static void AddToSave(ref IOption[] ___m_options)
        {
            // This is called when saving settings
            cache.lastResolution  = ___m_options[0].GetOption();
            cache.lastVSync       = ___m_options[2].GetOption();
            cache.lastWindowed    = ___m_options[1].GetOption();
            cache.lastQuality     = ___m_options[3].GetOption();
            cache.lastMusicVolume = ___m_options[4].GetOption();
            cache.lastSfxVolume   = ___m_options[5].GetOption();

            Flush();
        }

        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.Unload))]
        [HarmonyPostfix]
        private static void Unload(ref IOption[] ___m_options)
        {
            // This is called only on new file creation
            SetOptionHelper(ref ___m_options, 0, cache.lastResolution);
            SetOptionHelper(ref ___m_options, 2, cache.lastVSync);
            SetOptionHelper(ref ___m_options, 1, cache.lastWindowed);
            SetOptionHelper(ref ___m_options, 3, cache.lastQuality);
            SetOptionHelper(ref ___m_options, 4, cache.lastMusicVolume);
            SetOptionHelper(ref ___m_options, 5, cache.lastSfxVolume);
            SetOptionHelper(ref ___m_options, 8, 1);
        }

        // Need to know about this function because it writes defaults which we want to avoid caching
        private static bool InByteSave = false;
        [HarmonyPatch(typeof(MetaGameProgress), nameof(MetaGameProgress.ByteSave))]
        [HarmonyPrefix]
        private static void ByteSave_Prefix()
        {
            InByteSave = true;
        }
        [HarmonyPatch(typeof(MetaGameProgress), nameof(MetaGameProgress.ByteSave))]
        [HarmonyPostfix]
        private static void ByteSave_Postfix()
        {
            InByteSave = false;
        }

        // Intercept keybind load to pull from cache
        [HarmonyPatch]
        public static class GlobalSave_Get_Patch
        {
            static MethodBase TargetMethod()
            {
                Type[] parameterTypes = { typeof(string), typeof(Dictionary<string, string>).MakeByRefType(), typeof(Dictionary<string, string>) };
                return AccessTools.Method(typeof(GlobalSave), nameof(GlobalSave.Get), parameterTypes);
            }

            static void Postfix(string key, ref Dictionary<string, string> value, ref bool __result)
            {
                if (cache.bindings.ContainsKey(key))
                {
                    value = cache.bindings[key];
                    __result = true;
                }
            }
        }

        // Add updated keybinds to cache
        [HarmonyPatch(typeof(GlobalSave), nameof(GlobalSave.Set))]
        [HarmonyPatch(new Type[] { typeof(string), typeof(Dictionary<string, string>) })]
        [HarmonyPostfix]
        private static void GlobalSave_Set(string key, ref Dictionary<string, string> value)
        {
            if (!key.StartsWith("UserKeyBindings_")) {
                return; // we only care about keybinds
            }

            if (InByteSave) {
                return; // ByteSave will trigger a save before we've had a chance to load from the cache
            }

            // Set new binding to use across all save files
            cache.bindings[key] = value;
            Flush();
        }

        private static string CachePath
        {
            get
            {
                string dir = Application.persistentDataPath + "/OC2Modding/";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir + "/cache.json";
            }
        }

        private static void Read()
        {
            try
            {
                OC2Modding.Log.LogInfo($"Loading cache from '{CachePath}'...");
                using (StreamReader reader = new StreamReader(CachePath))
                {
                    string text = reader.ReadToEnd();
                    cache = JsonConvert.DeserializeObject<Cache>(text);
                }
            }
            catch
            {
                OC2Modding.Log.LogWarning($"Failed to parse json from {CachePath}");
            }
        }

        private static bool FlushPending = false;
        public static void Flush()
        {
            FlushPending = true;
        }

        private static void DoFlush()
        {
            FlushPending = false;

            try
            {
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(cache));
                OC2Modding.Log.LogInfo($"Flushed cache to '{CachePath}'...");
            }
            catch
            {
                OC2Modding.Log.LogError("Failed to flush cache");
            }
        }
    }
}
