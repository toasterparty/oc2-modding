using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Team17.Online.Multiplayer.Messaging;
using OrderController;

namespace OC2Modding
{
    public static class Nerfs
    {
        static int removedPlates = 0;
        static bool finishedFirstPass = false;
        static bool finishedFirstPassServer = false;
        static float originalWashTime = 0.0f;
        static List<PlayerInputLookup.Player> PlayersWearingBackpacks = new List<PlayerInputLookup.Player>();

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Nerfs));
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            removedPlates = 0;
            finishedFirstPass = false;
            finishedFirstPassServer = false;
            PlayersWearingBackpacks.Clear();
            originalWashTime = 0.0f;
        }

        private static bool ShouldPlace(ref GameObject _objectToPlace, bool isServer=false)
        {
            string objectName = _objectToPlace.name;

            if (OC2Config.DisableFireExtinguisher && objectName.Contains("utensil_fire_extinguisher"))
            {
                return false;
            }

            if (!isServer && finishedFirstPass)
            {
                return true;
            }
            else if (isServer && finishedFirstPassServer)
            {
                return true;
            }

            if (OC2Config.DisableCoal && objectName == "utensil_coalbucket_01")
            {
                return false;
            }

            bool isPlate =
                objectName.StartsWith("Plate ") ||
                objectName.Contains("equipment_plate") ||
                objectName.Contains("equipment_mug") ||
                objectName.Contains("equipment_tray") ||
                objectName.Contains("equipment_glass");

            if (isPlate)
            {
                if (OC2Config.PlatesStartDirty)
                {
                    string levelName = GameUtils.GetGameSession().LevelSettings.SceneDirectoryVarientEntry.LevelConfig.name;

                    if (
                        levelName.StartsWith("Summer_1_5") ||
                        levelName.StartsWith("Beach_3_2") ||
                        levelName.StartsWith("Beach_Special")
                    ) {
                        // certain levels have both plates and cups and
                        // on these levels, moving them to the serving window
                        // turns cups into plates, so we just don't bother
                        return true;
                    }

                    if (isServer)
                    {
                        removedPlates++;
                    }
                    return false;
                }
                else if (OC2Config.DisableOnePlate && removedPlates == 0 && isServer)
                {
                    removedPlates++;
                    return false;
                }
            }

            if (OC2Config.DisableBellows && objectName == "utensil_bellows_01")
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(ServerAttachStation), "OnItemPlaced")]
        [HarmonyPrefix]
        private static void OnItemPlaced_Server_Prefix(ref GameObject _objectToPlace, ref bool __state)
        {
            // OC2Modding.Log.LogInfo($"Placed '{_objectToPlace.name} (server)'");

            __state = ShouldPlace(ref _objectToPlace, isServer:true);
        }

        [HarmonyPatch(typeof(ServerAttachStation), "OnItemPlaced")]
        [HarmonyPostfix]
        private static void OnItemPlaced_Server_Postfix(ref GameObject _objectToPlace, ref ServerAttachStation __instance, ref bool __state)
        {
            if (!__state)
            {
                MethodInfo dynMethod = __instance.GetType().GetMethod("OnItemTaken", BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(__instance, new object[] { });
            }
        }

        [HarmonyPatch(typeof(ClientAttachStation), "OnItemPlaced")]
        [HarmonyPrefix]
        private static bool OnItemPlaced_Client(ref IClientAttachment _item)
        {
            GameObject _objectToPlace = _item.AccessGameObject();
            // OC2Modding.Log.LogInfo($"Placed '{_objectToPlace.name} (client)'");

            bool shouldPlace = ShouldPlace(ref _objectToPlace);
            if (!shouldPlace)
            {
                _objectToPlace.Destroy();
            }

            return shouldPlace;
        }

        [HarmonyPatch(typeof(ServerPlateReturnStation), nameof(ServerPlateReturnStation.UpdateSynchronising))]
        [HarmonyPrefix]
        private static void UpdateSynchronising(ref PlateReturnStation ___m_returnStation)
        {
            if (___m_returnStation.m_startingPlateNumber != 0 || removedPlates == 0 || finishedFirstPassServer)
            {
                return; // We already did this patch
            }

            if (___m_returnStation.name.Contains("rying"))
            {
                return; // This is the drying station for a sink
            }

            ___m_returnStation.m_startingPlateNumber = removedPlates;

            if (OC2Config.DisableOnePlate)
            {
                ___m_returnStation.m_startingPlateNumber -= 1;
            }

            finishedFirstPassServer = true;

            OC2Modding.Log.LogInfo($"Added {___m_returnStation.m_startingPlateNumber} plates to the serving window");
        }

        [HarmonyPatch(typeof(ClientCarryableItem), nameof(ClientCarryableItem.CanHandlePickup))]
        [HarmonyPrefix]
        private static void CanHandlePickup()
        {
            finishedFirstPass = true;
        }

        [HarmonyPatch(typeof(ServerKitchenFlowControllerBase), "OnSuccessfulDelivery")]
        [HarmonyPostfix]
        private static void OnSuccessfulDelivery(ref ServerKitchenFlowControllerBase __instance)
        {
            var monitor = __instance.GetMonitorForTeam(0);
            if (monitor.Score.TotalMultiplier > OC2Config.MaxTipCombo)
            {
                monitor.Score.TotalMultiplier = OC2Config.MaxTipCombo;
            }
        }

        /* Edit the message sent to clients to also show the new limit */
        [HarmonyPatch(typeof(KitchenFlowMessage), nameof(KitchenFlowMessage.SetScoreData))]
        [HarmonyPostfix]
        private static void SetScoreData(ref TeamMonitor.TeamScoreStats ___m_teamScore)
        {
            if (___m_teamScore.TotalMultiplier > OC2Config.MaxTipCombo)
            {
                ___m_teamScore.TotalMultiplier = OC2Config.MaxTipCombo;
            }
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), nameof(ClientPlayerControlsImpl_Default.ApplyServerEvent))]
        [HarmonyPrefix]
        private static bool ApplyServerEvent(ref Serialisable serialisable)
        {
            InputEventMessage inputEventMessage = (InputEventMessage)serialisable;
            InputEventMessage.InputEventType inputEventType = inputEventMessage.inputEventType;

            switch (inputEventType)
            {
                case InputEventMessage.InputEventType.Dash:
                case InputEventMessage.InputEventType.DashCollision:
                    {
                        return !OC2Config.DisableDash;
                    }
                case InputEventMessage.InputEventType.Catch:
                    {
                        return !OC2Config.DisableCatch;
                    }
                case InputEventMessage.InputEventType.EndThrow:
                    {
                        return !OC2Config.DisableThrow;
                    }
                default:
                    {
                        break;
                    }
            }

            return true;
        }

        private static bool InReceiveThrowEvent = false;

        [HarmonyPatch(typeof(ServerPlayerControlsImpl_Default), nameof(ServerPlayerControlsImpl_Default.ReceiveThrowEvent))]
        [HarmonyPrefix]
        private static void ReceiveThrowEventPrefix()
        {
            InReceiveThrowEvent = true;
        }

        [HarmonyPatch(typeof(ServerPlayerControlsImpl_Default), nameof(ServerPlayerControlsImpl_Default.ReceiveThrowEvent))]
        [HarmonyPostfix]
        private static void ReceiveThrowEventPostfix()
        {
            InReceiveThrowEvent = false;
        }

        [HarmonyPatch(typeof(ServerThrowableItem), nameof(ServerThrowableItem.CanHandleThrow))]
        [HarmonyPostfix]
        private static void CanHandleThrow(ref bool __result)
        {
            if (OC2Config.DisableThrow && __result)
            {
                if (InReceiveThrowEvent)
                {
                    // Only play sfx when in the handler for received throw events
                    // this is needed because for some reason teleporters are the only other
                    // scripting in the game which check this function
                    OC2Helpers.PlayErrorSfx();
                }
                
                __result = false;
            }
        }

        [HarmonyPatch(typeof(ServerPilotMovement), "Update_Movement")]
        [HarmonyPrefix]
        private static bool Update_Movement(ref ServerThrowableItem __instance)
        {
            if (__instance.gameObject.name.Contains("Pushable_Object") && OC2Config.DisableWokDrag)
            {
                return false;
            }

            if (__instance.gameObject.name == "MovingSection" && OC2Config.DisableControlStick)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(PlayerControls), nameof(PlayerControls.ScanForCatch))]
        [HarmonyPostfix]
        private static void ScanForCatch(ref ICatchable __result, ref PlayerControls __instance)
        {
            if (__result != null && OC2Config.DisableCatch)
            {
                __result = null;
            }
        }

        [HarmonyPatch(typeof(ServerPlayerControlsImpl_Default), nameof(ServerPlayerControlsImpl_Default.StartDash))]
        [HarmonyPrefix]
        private static bool StartDash()
        {
            return !OC2Config.DisableDash;
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "Update_Movement")]
        [HarmonyPrefix]
        private static void Update_Movement(ref float ___m_dashTimer)
        {
            if (___m_dashTimer > 0f && OC2Config.DisableDash)
            {
                ___m_dashTimer = 0f;
                OC2Helpers.PlayErrorSfx();
            }
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), nameof(ClientPlayerControlsImpl_Default.Init))]
        [HarmonyPostfix]
        private static void Init(ref PlayerControls ___m_controls)
        {
            if (!OC2Config.WeakDash)
            {
                return;
            }

            var m_movement_prop = ___m_controls.GetType().GetField("m_movement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            PlayerControls.MovementData m_movement = ((PlayerControls.MovementData)m_movement_prop.GetValue(___m_controls));
            
            float dashSpeedScale = 0.75f;
            float dashCooldownScale = 1.4f;

            m_movement.DashSpeed *= dashSpeedScale;
            m_movement.DashCooldown *= dashCooldownScale;

            m_movement_prop.SetValue(___m_controls, m_movement);
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "DoDash")]
        [HarmonyPrefix]
        private static bool DoDash()
        {
            return !OC2Config.DisableDash;
        }

        [HarmonyPatch(typeof(ServerMapAvatarControls), "Update_Movement")]
        [HarmonyPrefix]
        private static void Update_Movement__Prefix(ref ILogicalButton ___m_dashButton, ref ILogicalButton __state)
        {
            __state = ___m_dashButton;
            if (OC2Config.DisableDash)
            {
                ___m_dashButton = null;
            }
        }

        [HarmonyPatch(typeof(ServerMapAvatarControls), "Update_Movement")]
        [HarmonyPostfix]
        private static void Update_Movement_Postfix(ref ILogicalButton ___m_dashButton, ref ILogicalButton __state)
        {
            ___m_dashButton = __state; // Restore, just in case
        }

        [HarmonyPatch(typeof(ServerWashingStation), nameof(ServerWashable.UpdateSynchronising))]
        [HarmonyPrefix]
        private static void UpdateSynchronising_Server(ref WashingStation ___m_washingStation)
        {
            if (originalWashTime == 0.0f)
            {
                originalWashTime = ___m_washingStation.m_cleanPlateTime;
            }

            ___m_washingStation.m_cleanPlateTime = originalWashTime * OC2Config.WashTimeMultiplier;
        }

        [HarmonyPatch(typeof(ClientWashingStation), nameof(ClientWashingStation.UpdateSynchronising))]
        [HarmonyPrefix]
        private static void UpdateSynchronising_Client(ref WashingStation ___m_washingStation)
        {
            if (originalWashTime == 0.0f)
            {
                originalWashTime = ___m_washingStation.m_cleanPlateTime;
            }

            ___m_washingStation.m_cleanPlateTime = originalWashTime * OC2Config.WashTimeMultiplier;
        }

        [HarmonyPatch(typeof(ServerCookingHandler), nameof(ServerCookingHandler.Cook))]
        [HarmonyPrefix]
        private static void Cook(ref float _cookingDeltatTime, ref bool __result, ref CookingStateMessage ___m_ServerData, ref ServerCookingHandler __instance)
        {
            if (___m_ServerData.m_cookingState != CookingUIController.State.Idle && ___m_ServerData.m_cookingState != CookingUIController.State.Progressing)
            {
                _cookingDeltatTime *= OC2Config.BurnSpeedMultiplier;
            }
        }

        [HarmonyPatch(typeof(ServerOrderControllerBase), "IsFull")]
        [HarmonyPostfix]
        private static void IsFull(ref bool __result, ref List<ServerOrderData> ___m_activeOrders, ref int ___m_maxOrdersAllowed)
        {
            __result = ___m_activeOrders.Count >= ___m_maxOrdersAllowed + OC2Config.MaxOrdersOnScreenOffset;
        }

        private static int GetStages()
        {
            return Math.Max((int)(8.0f * OC2Config.ChoppingTimeScale), 2);
        }

        [HarmonyPatch(typeof(ServerWorkableItem), nameof(ServerWorkableItem.DoWork))]
        [HarmonyPatch(new Type[] { typeof(ServerAttachStation), typeof(GameObject) })]
        [HarmonyPrefix]
        private static void DoWork_Server(ref WorkableItem ___m_workable)
        {
            ___m_workable.m_stages = GetStages();
        }

        [HarmonyPatch(typeof(ClientWorkableItem), nameof(ClientWorkableItem.DoWork))]
        [HarmonyPatch(new Type[] { typeof(ClientAttachStation), typeof(GameObject), typeof(int) })]
        [HarmonyPrefix]
        private static void DoWork_Client(ref WorkableItem ___m_workable)
        {
            ___m_workable.m_stages = GetStages();
        }

        [HarmonyPatch(typeof(ServerEmoteWheel), "StartEmote")]
        [HarmonyPrefix]
        private static bool StartEmote(ref ServerEmoteWheel __instance, ref EmoteWheelMessage _message)
        {
            if (OC2Config.LockedEmotes.Contains(_message.m_emoteIdx))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientEmoteWheel), nameof(ClientEmoteWheel.StartEmote))]
        [HarmonyPrefix]
        private static bool StartEmoteClient(ref ClientEmoteWheel __instance, ref int _emoteIdx)
        {
            if (OC2Config.LockedEmotes.Contains(_emoteIdx))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientEmoteWheel), nameof(ClientEmoteWheel.RequestEmoteStart))]
        [HarmonyPrefix]
        private static bool RequestEmoteStart(ref int _emoteIdx)
        {
            if (OC2Config.LockedEmotes.Contains(_emoteIdx))
            {
                OC2Helpers.PlayErrorSfx();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "OnCarriedItemChanged")]
        [HarmonyPostfix]
        private static void CanHandlePickup(ref GameObject _before, ref GameObject _after, ref PlayerIDProvider ___m_controlsPlayer)
        {
            PlayerInputLookup.Player player = ___m_controlsPlayer.GetID();

            if (_before == null && _after != null && _after.name.Contains("ackpack"))
            {
                // pickup
                OC2Modding.Log.LogMessage($"Player {player} picked up a backpack");
                PlayersWearingBackpacks.Add(player);
            }
            else if (_before != null && _after == null && _before.name.Contains("ackpack"))
            {
                // putdown
                OC2Modding.Log.LogMessage($"Player {player} set down a backpack");
                PlayersWearingBackpacks.Remove(player);
            }
        }

        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "Update_Movement")]
        [HarmonyPrefix]
        private static void Update_Movement(ref PlayerControls ___m_controls, ref PlayerIDProvider ___m_controlsPlayer)
        {
            PlayerInputLookup.Player player = ___m_controlsPlayer.GetID();
            bool wearingBackpack = PlayersWearingBackpacks.Contains(player) || (OC2Helpers.GetCurrentPlayerCount() == 1 && PlayersWearingBackpacks.Count > 0);

            if (wearingBackpack)
            {
                if (OC2Config.BackpackMovementScale != 1.0f && ___m_controls.MovementScale != OC2Config.BackpackMovementScale)
                {
                    OC2Modding.Log.LogInfo($"Player {player}'s speed set to {OC2Config.BackpackMovementScale}");
                    ___m_controls.SetMovementScale(OC2Config.BackpackMovementScale);
                }
            }
            else if (___m_controls.MovementScale != 1.0f)
            {
                OC2Modding.Log.LogInfo($"Restored {player}'s speed to normal");
                ___m_controls.SetMovementScale(1.0f);
            }
        }

        [HarmonyPatch(typeof(ServerPlayerRespawnBehaviour), nameof(ServerPlayerRespawnBehaviour.StartSynchronising))]
        [HarmonyPostfix]
        private static void StartSynchronising(ref PlayerRespawnBehaviour ___m_PlayerRespawnBehaviour)
        {
            ___m_PlayerRespawnBehaviour.m_respawnTime = OC2Config.RespawnTime;
        }

        [HarmonyPatch(typeof(ClientPlayerRespawnBehaviour), nameof(ClientPlayerRespawnBehaviour.StartSynchronising))]
        [HarmonyPostfix]
        private static void StartSynchronisingClient(ref PlayerRespawnBehaviour ___m_PlayerRespawnBehaviour)
        {
            ___m_PlayerRespawnBehaviour.m_respawnTime = OC2Config.RespawnTime;
        }

        private static bool inReceiveTriggerInteractEvent = false;
        [HarmonyPatch(typeof(ServerPlayerControlsImpl_Default), nameof(ServerPlayerControlsImpl_Default.ReceiveTriggerInteractEvent))]
        [HarmonyPrefix]
        private static void ReceiveTriggerInteractEvent_Prefix()
        {
            inReceiveTriggerInteractEvent = true;
        }

        [HarmonyPatch(typeof(ServerPlayerControlsImpl_Default), nameof(ServerPlayerControlsImpl_Default.ReceiveTriggerInteractEvent))]
        [HarmonyPostfix]
        private static void ReceiveTriggerInteractEvent_Postfix()
        {
            inReceiveTriggerInteractEvent = false;
        }

        private static double previousDrinkTime = 0;
        [HarmonyPatch(typeof(ServerInteractable), nameof(ServerInteractable.CanInteract))]
        [HarmonyPostfix]
        private static void CanInteract(ref ServerInteractable __instance, ref bool __result)
        {
            if (OC2Config.CarnivalDispenserRefactoryTime <= 0.001f)
            {
                return; // This patch would have no effect
            }

            if (!__result)
            {
                return; // It's already not interactable
            }

            if (!inReceiveTriggerInteractEvent)
            {
                return; // It's an interaction check not pertaining to a player/drink switch
            }

            // OC2Modding.Log.LogInfo($"Can Interact With '{__instance.gameObject.name}'?");

            if (
                __instance.gameObject.name != "p_dlc08_button_Drinks" &&
                __instance.gameObject.name != "p_dlc08_button_Condiments" &&
                __instance.gameObject.name != "Switch" // TODO: this is for SoBo drinks, it probably conflicts with a lot of other things
            )
            {
                return; // It's not an interactable we care about
            }

            float checkTime = Time.time;
            if (checkTime > previousDrinkTime && checkTime - previousDrinkTime > OC2Config.CarnivalDispenserRefactoryTime)
            {
                // It has been beyond the cooldown time
                previousDrinkTime = checkTime;
                return;
            }

            // Reject the button push
            OC2Helpers.PlayErrorSfx();
            __result = false;
        }

        [HarmonyPatch(typeof(WorldMapSwitch), nameof(WorldMapSwitch.CanBePressed))]
        [HarmonyPostfix]
        private static void CanBePressed(ref bool __result)
        {
            if (OC2Config.DisableRampButton && __result)
            {
                // Reject the button push
                OC2Helpers.PlayErrorSfx();
                __result = false;
            }
        }

        [HarmonyPatch(typeof(GameModes.Horde.TeamScoreStats), nameof(GameModes.Horde.TeamScoreStats.GetTotalMoney))]
        [HarmonyPostfix]
        private static void GetTotalMoney(ref int __result, ref int ___TotalMoneyEarned)
        {
            if (OC2Config.DisableEarnHordeMoney)
            {
                ___TotalMoneyEarned = 0;
                __result = 0;
            }
        }

        /*** Experiments Below ***/

        // [HarmonyPatch(typeof(ServerDirtyPlateStack), nameof(ServerDirtyPlateStack.OnSurfaceDeplacement))]
        // [HarmonyPostfix]
        // private static void OnSurfaceDeplacement(ref ServerDirtyPlateStack __instance, ref ServerStack ___m_stack, ref ServerAttachStation ___m_attachStation)
        // {
        //     OC2Modding.Log.LogInfo("OnSurfaceDeplacement");
        //     // ___m_attachStation.AddItem(, Vector2.up);

        //     GameObject gameObject = ___m_stack.RemoveFromStack();
        //     ServerHandlePickupReferral serverHandlePickupReferral = gameObject.RequestComponent<ServerHandlePickupReferral>();
        //     if (serverHandlePickupReferral != null && serverHandlePickupReferral.GetHandlePickupReferree() == __instance)
        //     {
        //         serverHandlePickupReferral.SetHandlePickupReferree(null);
        //     }
        //     ServerHandlePlacementReferral serverHandlePlacementReferral = gameObject.RequestComponent<ServerHandlePlacementReferral>();
        //     if (serverHandlePlacementReferral != null && serverHandlePlacementReferral.GetHandlePlacementReferree() == __instance)
        //     {
        //         serverHandlePlacementReferral.SetHandlePlacementReferree(null);
        //     }
            
        //     ___m_attachStation.AddItem(gameObject, Vector2.up);
        // }

        // [HarmonyPatch(typeof(ServerTriggerTimer), nameof(ServerTriggerTimer.StartSynchronising))]
        // [HarmonyPostfix]
        // private static void StartSynchronising(ref TriggerTimer ___m_triggerTimer)
        // {
        //     GameLog.LogMessage($"start={___m_triggerTimer.m_startTrigger}, end={___m_triggerTimer.m_completeTrigger}");
        // }
        
        // private static bool scaleTime = false;
        // [HarmonyPatch(typeof(ServerTriggerTimer), nameof(ServerTriggerTimer.UpdateSynchronising))]
        // [HarmonyPrefix]
        // private static void ServerTriggerTimer_UpdateSynchronising_Prefix()
        // {
        //     scaleTime = true;
        // }

        // [HarmonyPatch(typeof(ServerTriggerTimer), nameof(ServerTriggerTimer.UpdateSynchronising))]
        // [HarmonyPostfix]
        // private static void ServerTriggerTimer_UpdateSynchronising_Postfix()
        // {
        //     scaleTime = false;
        // }

        // [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.GetDeltaTime))]
        // [HarmonyPatch(new Type[] { typeof(GameObject) })]
        // [HarmonyPostfix]
        // private static void ServerTriggerTimer_UpdateSynchronising_Postfix(ref float __result)
        // {
        //     if (scaleTime)
        //     {
        //         __result *= 10.0f;
        //     }
        // }
    }
}
