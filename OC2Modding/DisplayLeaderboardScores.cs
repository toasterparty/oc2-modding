using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace OC2Modding
{
    public class DisplayLeaderboardScores
    {
        private static ConfigEntry<bool> configDisplayLeaderboardScores;

        public static void Awake()
        {
            /* Setup Configuration */
            configDisplayLeaderboardScores = OC2Modding.configFile.Bind(
                "QualityOfLife", // Config Category
                "DisplayLeaderboardScores", // Config key name
                true, // Default Config value
                "Set to true to show the top 5 leaderboard scores when previewing a level" // Friendly description
            );

            /* Setup */
            downloadLeaderboardFile();

            /* Inject Mod */
            if (configDisplayLeaderboardScores.Value)
            {
                Harmony.CreateAndPatchAll(typeof(DisplayLeaderboardScores));
            }
        }

        public static bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void downloadLeaderboardFile()
        {
            Process downloadProcess = new Process();

            try
            {
                downloadProcess.StartInfo.UseShellExecute = false;
                downloadProcess.StartInfo.FileName = ".\\curl\\curl.exe";
                downloadProcess.StartInfo.Arguments = "https://overcooked.greeny.dev/assets/data/data.csv --output leaderboard_scores.csv";
                downloadProcess.StartInfo.CreateNoWindow = true;
                downloadProcess.Start();
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to download leaderboard-scores.csv: {e.ToString()}");
            }
        }

        private static List<int> getScoresFromLeaderboard(string game, string dlc, string level, uint playerCount, uint numScores)
        {
            List<int> scores = new List<int>();

            try
            {
                IEnumerable<string> lines = File.ReadAllLines("leaderboard_scores.csv");
                foreach (string line in lines)
                {
                    try
                    {
                        string[] values = line.Split(',');
                        if (values[0] != game || values[1] != dlc || values[2] != level || UInt32.Parse(values[3]) != playerCount)
                        {
                            continue; // not the level (or player count) we are looking for
                        }

                        int place = Int32.Parse(values[4]);
                        if (place > numScores)
                        {
                            continue; // The score isn't good enough to return
                        }

                        // Return this score
                        int score = Int32.Parse(values[6]);
                        scores.Add(score);

                        if (scores.Count >= numScores)
                        {
                            break; // we found all the scores we need
                        }
                    }
                    catch
                    {
                        // pass
                    }
                }
            }
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Failed to lookup scores in leaderboard-scores.csv: {e}");
                return scores;
            }

            scores.Sort();
            scores.Reverse();

            return scores;
        }
    }
}
