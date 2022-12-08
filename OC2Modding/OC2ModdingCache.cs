using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace OC2Modding
{
    public static class OC2ModdingCache
    {
        public class Cache
        {
            public string lastLoginHost = "";
            public string lastLoginUser = "";
            public string lastLoginPass = "";
        }
        
        public static Cache cache = new Cache();

        public static void Awake()
        {
            Read();
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
                OC2Modding.Log.LogInfo($"Loading config from '{CachePath}'...");
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
