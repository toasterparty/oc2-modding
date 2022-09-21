using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OC2Modding
{
    public static class ArchipelagoClient
    {
        public const bool REMOTE_INVENTORY = true;

        private static ArchipelagoSession session = null;

        static string serverUrl = "archipelago.gg";
        static string userName = "";
        static string password = "";

        static LoginResult cachedConnectionResult;

        public static bool IsConnected = false;
        public static bool IsConnecting = false;

        // must follow: https://github.com/toasterparty/Archipelago/blob/overcooked2/worlds/overcooked2/Items.py
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
            ProgressiveThrowCatch = 9,
            CoinPurse = 10,
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
            AggressiveHorde = 37,
        };

        private static int LastVisitedLocationsCount = 0;

        private static bool PendingItemUpdate()
        {
            return OC2Config.ItemIndex < session.Items.AllItemsReceived.Count;
        }

        public static void Update()
        {
            if (!IsConnected)
            {
                return;
            }

            if (PendingLocationUpdate || PendingPseudoSaveUpdate || PendingSendCompletion || PendingItemUpdate())
            {
                ThreadPool.QueueUserWorkItem((o) => UpdateTask());
            }
        }

        private static Mutex UpdateMut = new Mutex();

        private static void UpdateTask()
        {
            if (!UpdateMut.WaitOne(millisecondsTimeout: 0))
            {
                return; // mutex was taken
            }

            UpdateItems();
            UpdateVisitedLocations();
            UpdatePseudoSave();
            UpdateCompletion();

            UpdateMut.ReleaseMutex();
        }

        private static void UpdateItems()
        {
            bool doFlush = false;
            while (PendingItemUpdate())
            {
                try
                {
                    doFlush = true;
                    NetworkItem item = session.Items.AllItemsReceived[OC2Config.ItemIndex];
                    OC2Config.ItemIndex++;

                    long itemId = item.Item - 59812623889202; // "oc2" in ascii
                    if (!GiveItem((int)itemId))
                    {
                        OC2Modding.Log.LogError("Archipelago sent an item which goes above our inventory limits");
                    }
                }
                catch
                {
                    GameLog.LogMessage($"Error when receiving item at network index {OC2Config.ItemIndex}");
                    OC2Config.ItemIndex++;
                }
            }

            if (doFlush)
            {
                OC2Config.FlushConfig();
            }
        }

        private static void UpdateVisitedLocations()
        {
            if (!PendingLocationUpdate || !OC2Helpers.IsHostPlayer())
            {
                return;
            }

            PendingLocationUpdate = false;

            int count = VisitedLocations.Count;
            if (count == 0 || count == LastVisitedLocationsCount)
            {
                return; // no new data to send
            }

            LastVisitedLocationsCount = count;
            OC2Modding.Log.LogInfo($"Syncing Collected Locations with remote...");
            try
            {
                session.Locations.CompleteLocationChecks(VisitedLocations.ToArray());
            }
            catch
            {
                OC2Modding.Log.LogError("CompleteLocationChecks failed");
            }
        }

        private static void UpdateCompletion()
        {
            if (!PendingSendCompletion || !OC2Helpers.IsHostPlayer())
            {
                return;
            }

            try
            {
                var statusUpdatePacket = new StatusUpdatePacket();
                statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
                session.Socket.SendPacket(statusUpdatePacket);
            }
            catch
            {
                GameLog.LogMessage("Error: Failed to send game completion!");
            }
        }

        /* Apply to end of save directory to garuntee uniqueness across rooms of the same seed */
        public static string SaveDirSuffix()
        {
            try
            {
                int value = session.DataStorage["SaveDirSuffix"];
                return $"-{value}";
            }
            catch
            {
                if (IsConnected)
                {
                    OC2Modding.Log.LogWarning("Error when reading SaveDirSuffix");
                }
            }

            return "";
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
            if (IsConnected) return;

            ThreadPool.QueueUserWorkItem((o) => ConnectTask(server, user, pass));
        }

        private static bool PendingSendCompletion = false;
        public static void SendCompletion()
        {
            if (!IsConnected) return;
            PendingSendCompletion = true;
        }

        private static bool PendingPseudoSaveUpdate = false;
        public static void SendPseudoSave()
        {
            if (!IsConnected) return;
            PendingPseudoSaveUpdate = true;
        }

        private static void UpdatePseudoSave()
        {
            if (!PendingPseudoSaveUpdate)
            {
                return;
            }

            PendingPseudoSaveUpdate = false;

            // Fetch the existing
            Dictionary<int, int> PseudoSave = session.DataStorage["PseudoSave"].To<Dictionary<int, int>>();

            // Update us
            MergeDicts(OC2Config.PseudoSave, PseudoSave);

            if (!OC2Helpers.IsHostPlayer())
            {
                return;
            }

            // Update them
            bool updateRemote = MergeDicts(PseudoSave, OC2Config.PseudoSave);

            // Write back
            if (updateRemote)
            {
                session.DataStorage["PseudoSave"] = JObject.FromObject(PseudoSave);
            }
        }

        private static void OnPseudoSaveChanged(Dictionary<int, int> PseudoSave)
        {
            MergeDicts(OC2Config.PseudoSave, PseudoSave);
        }

        private static bool MergeDicts(Dictionary<int, int> main, Dictionary<int, int> update)
        {
            bool changed = false;
            foreach (KeyValuePair<int, int> kvp in update)
            {
                if (!main.ContainsKey(kvp.Key) || kvp.Value > main[kvp.Key])
                {
                    changed = true;
                    main[kvp.Key] = kvp.Value;
                }
            }

            return changed;
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


                var result = session.TryConnectAndLogin(
                    "Overcooked! 2",
                    userName,
                    REMOTE_INVENTORY ? ItemsHandlingFlags.AllItems : ItemsHandlingFlags.RemoteItems,
                    Version,
                    tags: new string[0],
                    uuid: null,
                    password: password
                );

                if (result is LoginSuccessful loginSuccess)
                {
                    /* Login was successful */

                    // non-seeded random number unique to this instance of this seed + slot
                    session.DataStorage["SaveDirSuffix"].Initialize((new Random()).Next(minValue: 0, maxValue: 10000));

                    if (loginSuccess.SlotData.TryGetValue("SaveFolderName", out var saveFolder))
                    {
                        /* SlotData contains a save folder directory */

                        string json = JsonConvert.SerializeObject(loginSuccess.SlotData);
                        OC2Config.SaveFolderName = (string)saveFolder;

                        string saveDirectory = OC2Helpers.getCustomSaveDirectory();
                        if (!Directory.Exists(saveDirectory))
                        {
                            Directory.CreateDirectory(saveDirectory);
                            OC2Modding.Log.LogInfo($"Created directory {saveDirectory}");
                        }

                        string saveName = saveDirectory + "/OC2Modding-INIT.json";
                        if (!File.Exists(saveName))
                        {
                            File.WriteAllText(saveName, json);
                            OC2Modding.Log.LogInfo($"Flushed to {saveName}");
                        }

                        OC2Config.InitConfig(false); // init starting inventory -> saved inventory
                    }
                    else
                    {
                        GameLog.LogMessage("Error: Archipelago did not provide seed identifier");
                    }
                }

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
                try
                {
                    // Set of completed levels and their completed star counts
                    session.DataStorage["PseudoSave"].Initialize(JObject.FromObject(new Dictionary<int, int>()));
                    session.DataStorage["PseudoSave"].OnValueChanged += (_old, _new) =>
                    {
                        OnPseudoSaveChanged(_new.ToObject<Dictionary<int, int>>());
                    };
                }
                catch (Exception e)
                {
                    GameLog.LogMessage($"Failed to initialize psuedo cloud save: {e.Message}");
                }

                PendingPseudoSaveUpdate = true;
                PendingLocationUpdate = true;
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

        private static bool PendingLocationUpdate = false;

        private static void UpdateLocations()
        {
            if (!OC2Helpers.IsHostPlayer())
            {
                return;
            }

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
            try
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
            catch (Exception e)
            {
                OC2Modding.Log.LogError($"Error when parsing received packet {e}");
            }
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
                case Oc2Item.ProgressiveThrowCatch:
                    {
                        if (OC2Config.DisableThrow)
                        {
                            OC2Config.DisableThrow = false;
                        }
                        else if (OC2Config.DisableCatch)
                        {
                            OC2Config.DisableCatch = false;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }
                case Oc2Item.CoinPurse:
                    {
                        if (!OC2Config.DisableEarnHordeMoney) return false;
                        OC2Config.DisableEarnHordeMoney = false;
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
                case Oc2Item.AggressiveHorde:
                    {
                        if (!OC2Config.AggressiveHorde) return false;
                        OC2Config.AggressiveHorde = false;
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
