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

        private static int LastVisitedLocationsCount = 0;
        private static int AllItemsReceivedCount = 0;
        
        public static void Update()
        {
            if (!IsConnected)
            {
                return;
            }

            int itemCount = session.Items.AllItemsReceived.Count;
            if (itemCount != 0 && itemCount != AllItemsReceivedCount)
            {
                AllItemsReceivedCount = itemCount;
                bool shouldFlush = false;
                foreach (NetworkItem item in session.Items.AllItemsReceived)
                {
                    long itemId = item.Item;
                    itemId -= 59812623889202; // "oc2" in ascii

                    string location = $"{item.Player}.{item.Location}.{itemId}"; // unique identifier for each location
                    if (OC2Config.RecievedItemIdentifiers.Contains(location))
                    {
                        OC2Modding.Log.LogInfo($"Not giving item #{itemId} as it was already received");
                        continue; // we've already processed this item
                    }
                    OC2Config.RecievedItemIdentifiers.Add(location);

                    if (GiveItem((int)itemId))
                    {
                        shouldFlush = true;
                    }
                }

                if (shouldFlush)
                {
                    OC2Config.FlushConfig();
                }
            }

            if (PendingLocationUpdate)
            {
                PendingLocationUpdate = false;

                int count = VisitedLocations.Count;
                if (count != 0 && count != LastVisitedLocationsCount)
                {
                    LastVisitedLocationsCount = count;
                    ThreadPool.QueueUserWorkItem((o) => UpdateLocationsTask());
                }
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

            OC2Modding.Log.LogInfo($"Adding Collected Location: {location}...");
            VisitedLocations.Add(location);
            UpdateLocations();
        }

        private static void UpdateLocationsTask()
        {
            OC2Modding.Log.LogInfo($"Syncing Collected Locations with remote...");
            ReconnectIfNeeded();
            session.Locations.CompleteLocationChecks(VisitedLocations.ToArray());
        }

        private static bool PendingLocationUpdate = false;

        private static void UpdateLocations()
        {
            PendingLocationUpdate = true;
        }

        static void ReconnectIfNeeded()
        {
            if (IsConnected && session.Socket.Connected)
                return;

            ConnectionAttempt(serverUrl, userName, password, session.ConnectionInfo.Uuid);
        }

        public static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            string text = "";
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Print:
                    {
                        var p = packet as PrintPacket;
                        text = p.Text;
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var p = packet as PrintJsonPacket;
                        foreach (var messagePart in p.Data)
                        {
                            switch (messagePart.Type)
                            {
                                case JsonMessagePartType.PlayerId:
                                    {
                                        text += Int32.TryParse(messagePart.Text, out var PlayerSlot)
                                            ? session.Players.GetPlayerAlias(PlayerSlot) ?? $"Slot: {PlayerSlot}"
                                            : messagePart.Text;
                                        break;
                                    }
                                case JsonMessagePartType.ItemId:
                                    {
                                        text += Int64.TryParse(messagePart.Text, out var itemID)
                                            ? session.Items.GetItemName(itemID) ?? $"Item: {itemID}" : messagePart.Text;
                                        break;
                                    }
                                case JsonMessagePartType.LocationId:
                                    {
                                        text += Int64.TryParse(messagePart.Text, out var locationID)
                                            ? session.Locations.GetLocationNameFromId(locationID) ?? $"Location: {locationID}"
                                            : messagePart.Text;
                                        break;
                                    }
                                default:
                                    {
                                        text += messagePart.Text;
                                        break;
                                    }
                            }
                        }

                        break;
                    }
            }

            GameLog.LogMessage(text);
        }

        private static void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
            UpdateInventory();
        }

        public static void UpdateInventory()
        {
            AllItemsReceivedCount = 0; // clear "cache"
        }

        /* Updates the current inventory with this item id.
           Also, prints a message and returns true if JSON
           should be flushed to disk */
        private static bool GiveItem(int id)
        {
            Oc2Item item = (Oc2Item)id;
            switch (item)
            {
                case Oc2Item.Wood:
                    {
                        if (!OC2Config.DisableWood) return false;
                        OC2Config.DisableWood = false;
                        break;
                    }
                case Oc2Item.CoalBucket:
                    {
                        if (!OC2Config.DisableCoal) return false;
                        OC2Config.DisableCoal = false;
                        break;
                    }
                case Oc2Item.SparePlate:
                    {
                        if (!OC2Config.DisableOnePlate) return false;
                        OC2Config.DisableOnePlate = false;
                        break;
                    }
                case Oc2Item.FireExtinguisher:
                    {
                        if (!OC2Config.DisableFireExtinguisher) return false;
                        OC2Config.DisableFireExtinguisher = false;
                        break;
                    }
                case Oc2Item.Bellows:
                    {
                        if (!OC2Config.DisableBellows) return false;
                        OC2Config.DisableBellows = false;
                        break;
                    }
                case Oc2Item.CleanDishes:
                    {
                        if (!OC2Config.PlatesStartDirty) return false;
                        OC2Config.PlatesStartDirty = false;
                        break;
                    }
                case Oc2Item.LargerTipJar:
                    {
                        if (OC2Config.MaxTipCombo < 4)
                        {
                            OC2Config.MaxTipCombo++;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                case Oc2Item.ProgressiveDash:
                    {
                        if (OC2Config.DisableDash)
                        {
                            OC2Config.DisableDash = false;
                        }
                        else if (OC2Config.WeakDash)
                        {
                            OC2Config.WeakDash = false;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }
                case Oc2Item.Throw:
                    {
                        if (!OC2Config.DisableThrow) return false;
                        OC2Config.DisableThrow = false;
                        break;
                    }
                case Oc2Item.Catch:
                    {
                        if (!OC2Config.DisableCatch) return false;
                        OC2Config.DisableCatch = false;
                        break;
                    }
                case Oc2Item.RemoteControlBatteries:
                    {
                        if (!OC2Config.DisableControlStick) return false;
                        OC2Config.DisableControlStick = false;
                        break;
                    }
                case Oc2Item.WokWheels:
                    {
                        if (!OC2Config.DisableWokDrag) return false;
                        OC2Config.DisableWokDrag = false;
                        break;
                    }
                case Oc2Item.DishScrubber:
                    {
                        if (OC2Config.WashTimeMultiplier == 1.0f) return false;
                        OC2Config.WashTimeMultiplier = 1.0f;
                        break;
                    }
                case Oc2Item.BurnLeniency:
                    {
                        if (OC2Config.BurnSpeedMultiplier == 1.0f) return false;
                        OC2Config.BurnSpeedMultiplier = 1.0f;
                        break;
                    }
                case Oc2Item.SharpKnife:
                    {
                        if (OC2Config.ChoppingTimeScale == 1.0f) return false;
                        OC2Config.ChoppingTimeScale = 1.0f;
                        break;
                    }
                case Oc2Item.OrderLookahead:
                    {
                        if (OC2Config.MaxOrdersOnScreenOffset < 0)
                        {
                            OC2Config.MaxOrdersOnScreenOffset++;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    }
                case Oc2Item.LightweightBackpack:
                    {
                        if (OC2Config.BackpackMovementScale == 1.0f) return false;
                        OC2Config.BackpackMovementScale = 1.0f;
                        break;
                    }
                case Oc2Item.FasterRespawnTime:
                    {
                        if (OC2Config.RespawnTime == 5.0f) return false;
                        OC2Config.RespawnTime = 5.0f;
                        break;
                    }
                case Oc2Item.FasterCondimentDrinkSwitch:
                    {
                        if (OC2Config.CarnivalDispenserRefactoryTime == 0.0f) return false;
                        OC2Config.CarnivalDispenserRefactoryTime = 0.0f;
                        break;
                    }
                case Oc2Item.GuestPatience:
                    {
                        if (OC2Config.CustomOrderLifetime == 100.0f) return false;
                        OC2Config.CustomOrderLifetime = 100.0f;
                        break;
                    }
                case Oc2Item.Kevin1:
                    {
                        if (!UnlockLevel(37)) return false;
                        break;
                    }
                case Oc2Item.Kevin2:
                    {
                        if (!UnlockLevel(38)) return false;
                        break;
                    }
                case Oc2Item.Kevin3:
                    {
                        if (!UnlockLevel(39)) return false;
                        break;
                    }
                case Oc2Item.Kevin4:
                    {
                        if (!UnlockLevel(40)) return false;
                        break;
                    }
                case Oc2Item.Kevin5:
                    {
                        if (!UnlockLevel(41)) return false;
                        break;
                    }
                case Oc2Item.Kevin6:
                    {
                        if (!UnlockLevel(42)) return false;
                        break;
                    }
                case Oc2Item.Kevin7:
                    {
                        if (!UnlockLevel(43)) return false;
                        break;
                    }
                case Oc2Item.Kevin8:
                    {
                        if (!UnlockLevel(44)) return false;
                        break;
                    }
                case Oc2Item.CookingEmote:
                    {
                        if (!UnlockEmote(0)) return false;
                        break;
                    }
                case Oc2Item.CurseEmote:
                    {
                        if (!UnlockEmote(1)) return false;
                        break;
                    }
                case Oc2Item.ServingEmote:
                    {
                        if (!UnlockEmote(2)) return false;
                        break;
                    }
                case Oc2Item.PreparingEmote:
                    {
                        if (!UnlockEmote(3)) return false;
                        break;
                    }
                case Oc2Item.WashingUpEmote:
                    {
                        if (!UnlockEmote(4)) return false;
                        break;
                    }
                case Oc2Item.OkEmote:
                    {
                        if (!UnlockEmote(5)) return false;
                        break;
                    }
                case Oc2Item.RampButton:
                    {
                        if (!OC2Config.DisableRampButton) return false;
                        OC2Config.DisableRampButton = false;
                        break;
                    }
                case Oc2Item.BonusStar:
                    {
                        OC2Config.StarOffset++;
                        break;
                    }
            }

            try
            {
                string itemName = session.Items.GetItemName(id + 59812623889202);
                OC2Modding.Log.LogInfo($"Received {itemName}");
                GameLog.LogMessage($"Received {itemName}");
            }
            catch
            {
                OC2Modding.Log.LogInfo($"Received Item #{id}");
            }

            return true;
        }

        private static bool UnlockLevel(int id)
        {
            if (!OC2Config.LevelForceReveal.Contains(id))
            {
                OC2Config.LevelForceReveal.Add(id);
                return true;
            }

            return false;
        }

        private static bool UnlockEmote(int id)
        {
            if (OC2Config.LockedEmotes.Contains(id))
            {
                OC2Config.LockedEmotes.Remove(id);
                return true;
            }

            return false;
        }
    }
}
