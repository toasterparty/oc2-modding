using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace OC2Modding
{
    public static class ArchipelagoClient
    {
        private static ArchipelagoSession session = null;

        static string serverUrl = "archipelago.gg";
        static string userName = "";
        static string password = "";

        static LoginResult cachedConnectionResult;

        public static bool IsConnected = false;
        public static bool IsConnecting = false;

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

        private static int AllItemsReceivedCount = 0;
        public static void Update()
        {
            if (IsConnected && AllItemsReceivedCount != session.Items.AllItemsReceived.Count)
            {
                UpdateInventory();
            }
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

        public static void Connect(string server, string user, string pass)
        {
            if (IsConnected)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem((o) => ConnectTask(server, user, pass));
        }

        private static void ConnectTask(string server, string user, string pass)
        {
            var result = ConnectionAttempt(server, user, pass);
            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = "Failed to Connect to the Archipelago Server";
                foreach (string error in failure.Errors)
                {
                    errorMessage += $"\n    {error}";
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    errorMessage += $"\n    {error}";
                }
                GameLog.LogMessage(errorMessage);
                return;
            }

            GameLog.LogMessage("Successfully Connected to Archipelago Server");
        }

        private static LoginResult ConnectionAttempt(string server, string user, string pass = null, string connectionId = null)
        {
            if (IsConnected && session.Socket.Connected && cachedConnectionResult != null)
            {
                if (serverUrl == server && userName == user && password == pass)
                    return cachedConnectionResult;

                Disconnect();
            }

            while (IsConnecting)
            {
                // This is bad programming, but our GUI flow should never let us get here, so it's good system redundancy design :)
                Thread.Sleep(100);
            }

            IsConnecting = true;

            try
            {                
                // Derrive the full uri without breaking it
                serverUrl = server.Replace("ws://", "");
                if (!serverUrl.Contains(":"))
                {
                    serverUrl += ":38281"; // default port
                }
                serverUrl = "ws://" + serverUrl;
                serverUrl = serverUrl.Replace(" ", "");

                userName = user;
                password = pass;

                var uri = new Uri(serverUrl);
                session = ArchipelagoSessionFactory.CreateSession(uri);

                session.Socket.PacketReceived += OnPacketReceived;
                session.Items.ItemReceived += (receivedItemsHelper) =>
                {
                    OnItemReceived(receivedItemsHelper);
                };

                var result = session.TryConnectAndLogin(
                    "Overcooked! 2",
                    userName,
                    ItemsHandlingFlags.RemoteItems,
                    Version,
                    tags: new string[0],
                    uuid: null,
                    password: password
                );

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
                UpdateInventory();
            }

            IsConnecting = false;

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

            // TODO: check hash of last location array that was sent

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

            ConnectionAttempt(serverUrl, userName, password, session.ConnectionInfo.Uuid);
        }

        public static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Print:
                    {
                        var p = packet as PrintPacket;
                        GameLog.LogMessage(p.Text);
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var p = packet as PrintJsonPacket;
                        string text = "";
                        foreach (var messagePart in p.Data)
                        {
                            switch (messagePart.Type)
                            {
                                case JsonMessagePartType.PlayerId:
                                    text += int.TryParse(messagePart.Text, out var PlayerSlot)
                                        ? session.Players.GetPlayerAlias(PlayerSlot) ?? $"Slot: {PlayerSlot}"
                                        : messagePart.Text;
                                    break;
                                case JsonMessagePartType.ItemId:
                                    text += int.TryParse(messagePart.Text, out var itemID)
                                        ? session.Items.GetItemName(itemID) ?? $"Item: {itemID}" : messagePart.Text;
                                    break;
                                case JsonMessagePartType.LocationId:
                                    text += int.TryParse(messagePart.Text, out var locationID)
                                        ? session.Locations.GetLocationNameFromId(locationID) ?? $"Location: {locationID}"
                                        : messagePart.Text;
                                    break;
                                default:
                                    text +=  messagePart.Text;
                                    break;
                            }
                        }

                        GameLog.LogMessage(text);
                    }

                    break;
            }
        }

        private static void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
            UpdateInventory();
        }

        private static void UpdateInventory()
        {
            var items = session.Items.AllItemsReceived;
            AllItemsReceivedCount = items.Count;
            foreach (NetworkItem item in session.Items.AllItemsReceived)
            {
                long itemId = item.Item;
                itemId -= 59812623889202; // "oc2" in ascii
                GiveItem((int)itemId);
            }

            OC2Config.FlushConfig();
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
