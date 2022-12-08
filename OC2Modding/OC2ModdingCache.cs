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
            public int lastMusicVolume = -1;
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
            ApplyCachedMusicVolume();
        }

        private static void ApplyCachedMusicVolume()
        {
            if (cache.lastMusicVolume == -1)
            {
                applied = true;
                return;
            }

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

            OC2Modding.Log.LogInfo($"Set cached music volume to {cache.lastMusicVolume}");
            float vol = 80.0f * ((float)cache.lastMusicVolume)/10.0f; // scale from 0-10 to 0-80
            vol -= 80.0f; // offset to negative dB
            mixer.SetFloat("MusicVolume", vol);
            applied = true;
        }

        private static void ApplyCachedMusicVolume(ref IOption[] ___m_options)
        {
            if (___m_options[4] != null && cache.lastMusicVolume != -1)
            {
                ___m_options[4].SetOption(cache.lastMusicVolume);
                ___m_options[4].Commit();
            }
        }

        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.LoadFromSave))]
        [HarmonyPostfix]
        private static void LoadFromSave(ref IOption[] ___m_options)
        {
            ApplyCachedMusicVolume(ref ___m_options);
        }

        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.AddToSave))]
        [HarmonyPostfix]
        private static void AddToSave(ref IOption[] ___m_options)
        {
            cache.lastMusicVolume = ___m_options[4].GetOption();
            OC2Modding.Log.LogInfo($"Saved music volume ({cache.lastMusicVolume})");
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
