using System.IO;
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
        }

        public static Cache cache = new Cache();

        public static void Awake()
        {
            Read();
            Harmony.CreateAndPatchAll(typeof(OC2ModdingCache));
        }

        private static bool applied = false;

        public static void Update()
        {
            if (applied) return;
            ApplyCachedVolumes();
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

        public static void Flush()
        {
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
