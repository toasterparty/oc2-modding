
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Diagnostics;

namespace OC2Modding
{
    public class LeaderboardScoresRepository
    {
        public static LeaderboardScoresRepository Instance = new LeaderboardScoresRepository();

        public class OneScore
        {
            public string Name { get; set; }
            public string Score { get; set; }
            public string Place { get; set; }
        }

        public class OneLevelOneSizeScore
        {
            public readonly List<OneScore> Scores = new List<OneScore>();
        }

        public class OneLevelScore
        {
            public readonly Dictionary<int, OneLevelOneSizeScore> SizeScores = new Dictionary<int, OneLevelOneSizeScore>();
            public DateTime UpdateTime { get; set; } = DateTime.MinValue;
            public bool LevelIsSupported { get; set; }
        }

        private static TimeSpan CACHE_INVALIDATION_TIMEOUT = new TimeSpan(0, 10, 0);
        private static TimeSpan MAXIMUM_REQUEST_FREQUENCY = new TimeSpan(0, 0, 3);
        private static string API_URL = "https://overcooked.greeny.dev/api/v1/overcooked-2/scores/";

        private Dictionary<string, OneLevelScore> levelScores = new Dictionary<string, OneLevelScore>();
        public DateTime LastQuery { get; set; } = DateTime.MinValue;

        private object SyncRoot = new object();

        public OneLevelScore GetOrStartRequestForLevel(string level)
        {
            bool needsRequest = false;
            OneLevelScore result;
            lock (SyncRoot)
            {
                var now = DateTime.Now;
                if (!levelScores.ContainsKey(level) || levelScores[level].UpdateTime + CACHE_INVALIDATION_TIMEOUT < now)
                {
                    if (LastQuery + MAXIMUM_REQUEST_FREQUENCY < now)
                    {
                        LastQuery = now;
                        needsRequest = true;
                    }
                }
                result = levelScores.ContainsKey(level) ? levelScores[level] : null;
            }
            if (needsRequest)
            {
                ThreadPool.QueueUserWorkItem((_) => MakeRequestForLevel(level));
            }
            return result;
        }

        private void MakeRequestForLevel(string level)
        {
            var url = API_URL + level;
            Console.WriteLine("[OCLeaderboardMod] Making API request to " + url);
            string json = "";

            try
            {
                try
                {
                    string curlPath = OC2Helpers.AssemblyDirectory + "\\..\\..\\curl\\curl.exe";
                    Process downloadProcess = new Process();
                    downloadProcess.StartInfo.UseShellExecute = false;
                    downloadProcess.StartInfo.FileName = curlPath;
                    downloadProcess.StartInfo.Arguments = $"{API_URL}/{level} --output leaderboard_scores.json";
                    downloadProcess.StartInfo.CreateNoWindow = true;
                    downloadProcess.Start();
                    downloadProcess.WaitForExit();
                }
                catch (Exception e)
                {
                    OC2Modding.Log.LogError($"Failed to download leaderboard_scores.json: {e.ToString()}");
                    return;
                }

                using (StreamReader reader = new StreamReader("leaderboard_scores.json"))
                {
                    json = reader.ReadToEnd();
                }

                var response = SimpleJSON.JSON.Parse(json);

                lock (SyncRoot)
                {
                    if (response["status"].AsInt == 404)
                    {
                        lock (SyncRoot)
                        {
                            levelScores[level] = new OneLevelScore
                            {
                                UpdateTime = DateTime.Now,
                                LevelIsSupported = false
                            };
                        }
                    }
                    else
                    {
                        foreach (var outerItem in response["data"]["levels"].AsArray.Children)
                        {
                            var outputData = new OneLevelScore()
                            {
                                UpdateTime = DateTime.Now,
                                LevelIsSupported = true,
                            };
                            var levelName = (string)outerItem["level"];
                            foreach (var item in outerItem["players"].AsArray.Children)
                            {
                                var playerCount = item["playerCount"].AsInt;
                                var outputRecord = new OneLevelOneSizeScore();
                                foreach (var record in item["scores"].AsArray.Children)
                                {
                                    outputRecord.Scores.Add(new OneScore
                                    {
                                        Name = (string)record["name"],
                                        Score = (string)record["score"],
                                        Place = (string)record["place"],
                                    });
                                }
                                outputData.SizeScores[playerCount] = outputRecord;
                            }
                            levelScores[levelName] = outputData;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[OCLeaderboardMod] Could not fetch json from API " + url + ": " + e);
            }
        }

        public bool MyRemoteCertificateValidationCallback(System.Object sender,
            X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        isOk = false;
                        break;
                    }
                }
            }
            return isOk;
        }
    }
}
