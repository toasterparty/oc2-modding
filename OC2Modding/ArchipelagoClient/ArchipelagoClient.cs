using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;

namespace OC2Modding
{
    public static class ArchipelagoClient
    {
        private static ArchipelagoSession session = null;

        static string serverUrl = "";
        static string userName = "";
        static string password = "";

        static LoginResult cachedConnectionResult;

        public static bool IsConnected;

        public static Permissions ForfeitPermissions => session.RoomState.ForfeitPermissions;
        public static Permissions CollectPermissions => session.RoomState.CollectPermissions;

        public static string ConnectionId => session.ConnectionInfo.Uuid;

        public static string SeedString => session.RoomState.Seed;

        public static string GetCurrentPlayerName() => session.Players.GetPlayerAliasAndName(session.ConnectionInfo.Slot);

        public static LocationCheckHelper LocationCheckHelper => session.Locations;

        public static DataStorageHelper DataStorage => session.DataStorage;

        private enum Oc2Item
        {
            Wood = 1,
            CoalBucket = 2,
            SparePlate = 3,
            FireExtinguisher = 4,
            Bellows = 5,
            CleanDishes = 6,
            LargerTipJar = 7,
            ProgressiveDash = 8,
            Throw = 9,
            Catch = 10,
            RemoteControlBatteries = 11,
            WokWheels = 12,
            DishScrubber = 13,
            BurnLeniency = 14,
            SharpKnife = 15,
            OrderLookahead = 16,
            LightweightBackpack = 17,
            FasterRespawnTime = 18,
            FasterCondimentDrinkSwitch = 19,
            GuestPatience = 20,
            Kevin1 = 21,
            Kevin2 = 22,
            Kevin3 = 23,
            Kevin4 = 24,
            Kevin5 = 25,
            Kevin6 = 26,
            Kevin7 = 27,
            Kevin8 = 28,
            CookingEmote = 29,
            CurseEmote = 30,
            ServingEmote = 31,
            PreparingEmote = 32,
            WashingUpEmote = 33,
            OkEmote = 34,
            RampButton = 35,
            BonusStar = 36,
        };

        private static float LastUpdateCheckTime = Time.time;

        public static void Update()
        {
            if (Time.time - LastUpdateCheckTime > 5)
            {
                LastUpdateCheckTime = Time.time;
                ThreadPool.QueueUserWorkItem((o) => ConnectionAttempt());
            }
        }

        private static void ConnectionAttempt()
        {
            if (IsConnected)
            {
                return;
            }

            var result = Connect("ws://192.168.0.108:38281", "toasterparty");

            LastUpdateCheckTime = Time.time;
            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                OC2Modding.Log.LogWarning("Failed to Connect to the Archipelago Server");
                foreach (string error in failure.Errors)
                {
                    OC2Modding.Log.LogWarning($"\t{error}");
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    OC2Modding.Log.LogWarning($"\t{error}");
                }
                return;
            }

            OC2Modding.Log.LogInfo("Successfully Connected to Archipelago Server");
        }

        private static Version CreateVersion()
        {
            string[] version_str = PluginInfo.PLUGIN_VERSION.Split('.');
            int major = Int32.Parse(version_str[0]);
            int minor = Int32.Parse(version_str[1]);
            int patch = Int32.Parse(version_str[2]);
            return new Version(major, minor, patch);
        }

        private static Version Version = CreateVersion();

        public static LoginResult Connect(string server, string user, string pass = null, string connectionId = null)
        {
            if (IsConnected && session.Socket.Connected && cachedConnectionResult != null)
            {
                if (serverUrl == server && userName == user && password == pass)
                    return cachedConnectionResult;

                Disconnect();
            }

            serverUrl = server;
            userName = user;
            password = pass;

            try
            {
                var uri = new Uri(server);
                session = ArchipelagoSessionFactory.CreateSession(uri);

                var result = session.TryConnectAndLogin(
                    "Overcooked! 2",
                    userName,
                    ItemsHandlingFlags.RemoteItems,
                    Version,
                    tags: new string[0],
                    uuid: null,
                    password: password
                );

                session.MessageLog.OnMessageReceived += OnMessageReceived;
                session.Items.ItemReceived += (receivedItemsHelper) =>
                {
                    OnItemReceived(receivedItemsHelper);
                };

                IsConnected = result.Successful;
                cachedConnectionResult = result;
            }
            catch (Exception e)
            {
                IsConnected = false;
                cachedConnectionResult = new LoginFailure(e.GetBaseException().Message);
            }

            if (IsConnected)
            {
                UpdateLocations();
            }

            return cachedConnectionResult;
        }

        public static void Disconnect()
        {
            session?.Socket?.DisconnectAsync();

            serverUrl = null;
            userName = null;
            password = null;

            IsConnected = false;

            session = null;

            cachedConnectionResult = null;
        }

        // public static NetworkItem? GetNextItem(int currentIndex) =>
        //     session.Items.AllItemsReceived.Count > currentIndex
        //         ? session.Items.AllItemsReceived[currentIndex]
        //         : default(NetworkItem?);

        // public static void SetStatus(ArchipelagoClientState status) => SendPacket(new StatusUpdatePacket { Status = status });

        // static void SendPacket(ArchipelagoPacketBase packet) => session?.Socket?.SendPacket(packet);

        // public static void Say(string message) => SendPacket(new SayPacket { Text = message });

        static bool IsMe(int slot) => slot == session.ConnectionInfo.Slot;

        private static List<long> VisitedLocations = new List<long>();

        public static void VisitLocation(long location)
        {
            if (location < 1 || location > 44)
            {
                return; // Don't care about this level
            }

            if (VisitedLocations.Contains(location))
            {
                return; // It's already been sent
            }

            ThreadPool.QueueUserWorkItem((o) => VisitLocationTask(location));
        }

        private static void VisitLocationTask(long location)
        {
            VisitedLocations.Add(location);
            OC2Modding.Log.LogInfo($"Adding Collected Location: {location}...");
            
            UpdateLocations();
        }

        private static void UpdateLocations()
        {
            OC2Modding.Log.LogInfo($"Syncing Collected Locations with remote...");

            ReconnectIfNeeded();
            session.Locations.CompleteLocationChecks(VisitedLocations.ToArray());
            // session.Locations.ScoutLocationsAsync(VisitedLocations.ToArray());
        }

        static void ReconnectIfNeeded()
        {
            if (IsConnected && session.Socket.Connected)
                return;

            Connect(serverUrl, userName, password, session.ConnectionInfo.Uuid);
        }

        private static void OnMessageReceived(LogMessage message)
        {
            GameLog.LogMessage(message.ToString());
        }

        private static void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
            NetworkItem item = receivedItemsHelper.DequeueItem();

            if (!IsMe(item.Player))
            {
                return;
            }

            long itemId = item.Item;
            itemId -= 59812623889202; // "oc2" in ascii

            GiveItem((int)itemId);
        }

        private static void GiveItem(int id)
        {
            OC2Modding.Log.LogInfo($"Received Item {id}");

            Oc2Item item = (Oc2Item)id;
            switch (item)
            {
                case Oc2Item.Wood:
                    {
                        OC2Config.DisableWood = false;
                        break;
                    }
                case Oc2Item.CoalBucket:
                    {
                        OC2Config.DisableCoal = false;
                        break;
                    }
                case Oc2Item.SparePlate:
                    {
                        OC2Config.DisableOnePlate = false;
                        break;
                    }
                case Oc2Item.FireExtinguisher:
                    {
                        OC2Config.DisableFireExtinguisher = false;
                        break;
                    }
                case Oc2Item.Bellows:
                    {
                        OC2Config.DisableBellows = false;
                        break;
                    }
                case Oc2Item.CleanDishes:
                    {
                        OC2Config.PlatesStartDirty = false;
                        break;
                    }
                case Oc2Item.LargerTipJar:
                    {
                        if (OC2Config.MaxTipCombo < 4)
                        {
                            OC2Config.MaxTipCombo++;
                        }
                        break;
                    }
                case Oc2Item.ProgressiveDash:
                    {
                        if (OC2Config.DisableDash)
                        {
                            OC2Config.DisableDash = false;
                        }
                        else
                        {
                            OC2Config.WeakDash = false;
                        }
                        break;
                    }
                case Oc2Item.Throw:
                    {
                        OC2Config.DisableThrow = false;
                        break;
                    }
                case Oc2Item.Catch:
                    {
                        OC2Config.DisableCatch = false;
                        break;
                    }
                case Oc2Item.RemoteControlBatteries:
                    {
                        OC2Config.DisableControlStick = false;
                        break;
                    }
                case Oc2Item.WokWheels:
                    {
                        OC2Config.DisableWokDrag = false;
                        break;
                    }
                case Oc2Item.DishScrubber:
                    {
                        OC2Config.WashTimeMultiplier = 1.0f;
                        break;
                    }
                case Oc2Item.BurnLeniency:
                    {
                        OC2Config.BurnSpeedMultiplier = 1.0f;
                        break;
                    }
                case Oc2Item.SharpKnife:
                    {
                        OC2Config.ChoppingTimeScale = 1.0f;
                        break;
                    }
                case Oc2Item.OrderLookahead:
                    {
                        if (OC2Config.MaxOrdersOnScreenOffset < 0)
                        {
                            OC2Config.MaxOrdersOnScreenOffset++;
                        }
                        break;
                    }
                case Oc2Item.LightweightBackpack:
                    {
                        OC2Config.BackpackMovementScale = 1.0f;
                        break;
                    }
                case Oc2Item.FasterRespawnTime:
                    {
                        OC2Config.RespawnTime = 5.0f;
                        break;
                    }
                case Oc2Item.FasterCondimentDrinkSwitch:
                    {
                        OC2Config.CarnivalDispenserRefactoryTime = 0.0f;
                        break;
                    }
                case Oc2Item.GuestPatience:
                    {
                        OC2Config.CustomOrderLifetime = 100.0f;
                        break;
                    }
                case Oc2Item.Kevin1:
                    {
                        UnlockLevel(37);
                        break;
                    }
                case Oc2Item.Kevin2:
                    {
                        UnlockLevel(38);
                        break;
                    }
                case Oc2Item.Kevin3:
                    {
                        UnlockLevel(39);
                        break;
                    }
                case Oc2Item.Kevin4:
                    {
                        UnlockLevel(40);
                        break;
                    }
                case Oc2Item.Kevin5:
                    {
                        UnlockLevel(41);
                        break;
                    }
                case Oc2Item.Kevin6:
                    {
                        UnlockLevel(42);
                        break;
                    }
                case Oc2Item.Kevin7:
                    {
                        UnlockLevel(43);
                        break;
                    }
                case Oc2Item.Kevin8:
                    {
                        UnlockLevel(44);
                        break;
                    }
                case Oc2Item.CookingEmote:
                    {
                        UnlockEmote(0);
                        break;
                    }
                case Oc2Item.CurseEmote:
                    {
                        UnlockEmote(1);
                        break;
                    }
                case Oc2Item.ServingEmote:
                    {
                        UnlockEmote(2);
                        break;
                    }
                case Oc2Item.PreparingEmote:
                    {
                        UnlockEmote(3);
                        break;
                    }
                case Oc2Item.WashingUpEmote:
                    {
                        UnlockEmote(4);
                        break;
                    }
                case Oc2Item.OkEmote:
                    {
                        UnlockEmote(5);
                        break;
                    }
                case Oc2Item.RampButton:
                    {
                        OC2Config.DisableRampButton = false;
                        break;
                    }
                case Oc2Item.BonusStar:
                    {
                        OC2Config.StarOffset++;
                        break;
                    }
            }

            OC2Config.FlushConfig();
        }

        private static void UnlockLevel(int id)
        {
            if (!OC2Config.LevelForceReveal.Contains(id))
            {
                OC2Config.LevelForceReveal.Add(id);
            }
        }

        private static void UnlockEmote(int id)
        {
            if (OC2Config.LockedEmotes.Contains(id))
            {
                OC2Config.LockedEmotes.Remove(id);
            }
        }
    }
}
