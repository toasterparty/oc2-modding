#if NET35
    #define STEAM
#elif NET46
    #define EPIC
#endif

#if STEAM
using System.Collections.Generic;
using HarmonyLib;
using Team17.Online;
using Steamworks;
using BitStream;
using System.Reflection;
#endif

namespace OC2Modding
{
    public static class CoopHandshake
    {
        public static void Awake()
        {
#if STEAM
            try
            {
                Harmony.CreateAndPatchAll(typeof(CoopHandshake));
                Harmony.CreateAndPatchAll(typeof(ReceiveJoinRequestMessage_Patch));
            }
            catch
            {
                OC2Modding.Log.LogError("Critically failed to patch for CoOp handshake");
            }
#endif
        }

#if STEAM
        [HarmonyPatch(typeof(JoinSessionBaseTask), nameof(JoinSessionBaseTask.Start))]
        [HarmonyPrefix]
        private static void Start()
        {
            ArchipelagoClient.SetCoopJoinRequest();
        }

        [HarmonyPatch(typeof(JoinSessionBaseTask), nameof(JoinSessionBaseTask.OnlineMultiplayerSessionJoinCallback))]
        [HarmonyPostfix]
        private static void OnlineMultiplayerSessionJoinCallback()
        {
            ArchipelagoClient.ClearCoopJoinRequest();
        }

        [HarmonyPatch(typeof(Team17.Online.SteamOnlineMultiplayerSessionCoordinator))]
        [HarmonyPatch("ReceiveJoinRequestMessage")]
        public class ReceiveJoinRequestMessage_Patch
        {
            /* Stolen from dnSpy */
            private enum TransportMessageTypes : byte
            {
                eNothing,
                eJoinRequest,
                eJoinRequestReply,
                eUserJoined,
                eUserDisconnected,
                eKeepalive
            }

            /* Intercept processing of client connections and pre-emptively reject if it's not
               the same kind of mod as the one we are running */
            private static bool Prefix(
                ref BitStreamReader ___m_transportBitStreamReader,
                ref Team17.Online.SteamOnlineMultiplayerSessionCoordinator __instance,
                ref CSteamID fromSteamId,
                ref FastList<byte> ___m_transportSendList,
                ref BitStreamWriter ___m_transportBitStreamWriter,
                ref SteamOnlineMultiplayerSessionTransportCoordinator ___m_transportCoordinator
            )
            {
                if (!OC2Config.ForceSingleSaveSlot)
                {
                    return true; // Just some QoL options
                }

                // Reject if not latest modded game version
                if (!ArchipelagoClient.CoopJoinRequest())
                {
                    GameLog.LogMessage("ERROR: Only players who are connected to the same AP slot can connect to your game");

                    // Send rejection letter
                    MethodInfo SetupOutgoingMessage = __instance.GetType().GetMethod("SetupOutgoingMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                    SetupOutgoingMessage.Invoke(__instance, new object[] { TransportMessageTypes.eJoinRequestReply });
                    ___m_transportBitStreamWriter.Write((byte)OnlineMultiplayerSessionJoinResult.eCodeVersionMismatch, 8);
                    ___m_transportCoordinator.SendData(fromSteamId, ___m_transportSendList._items, ___m_transportSendList.Count, true);

                    return false;
                }

                // TODO: add fromSteamId to list of known good IDs in network DataStorage

                if (ArchipelagoClient.IsConnected)
                {
                    OC2Modding.Log.LogInfo("Successfully handshook with another modded client");
                }

                return true;
            }
        }
#endif
    }
}
