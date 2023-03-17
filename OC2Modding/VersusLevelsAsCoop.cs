using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;
using HarmonyLib;

using Team17.Online;

namespace OC2Modding
{
    public static class VersusLevelsAsCoop
    {
        public static void Awake()
        {
            if (OC2Config.Config.VersusLevelsAsCoop)
            {
                Harmony.CreateAndPatchAll(typeof(VersusLevelsAsCoop));
                Harmony.CreateAndPatchAll(typeof(OnCouchPlayClicked_Patch));
            }

            // TODO: this doesn't work for JSON files loaded late
        }

        // TODO: only patch what we need

        [HarmonyPatch(typeof(ServerKitchenFlowControllerBase), "OnOrderAdded")]
        [HarmonyPrefix]
        private static void OnOrderAdded(ref TeamID _teamID)
        {
            _teamID = TeamID.One;
        }

        [HarmonyPatch(typeof(ServerKitchenFlowControllerBase), "OnOrderExpired")]
        [HarmonyPrefix]
        private static void OnOrderExpired(ref TeamID _teamID)
        {
            _teamID = TeamID.One;
        }

        [HarmonyPatch(typeof(ServerPlateStation), nameof(ServerPlateStation.GetTeamID))]
        [HarmonyPostfix]
        private static void GetTeamID(ref TeamID __result)
        {
            __result = TeamID.One;
        }

        [HarmonyPatch(typeof(ServerCompetitiveFlowController), nameof(ServerCompetitiveFlowController.GetMonitorForTeam))]
        [HarmonyPrefix]
        private static void GetMonitorForTeam_ServerCompetitiveFlowController(ref TeamID _team)
        {
            _team = TeamID.One;   
        }

        [HarmonyPatch(typeof(KitchenFlowMessage), nameof(KitchenFlowMessage.Initialise_DeliverySuccess))]
        [HarmonyPrefix]
        private static void Initialise_DeliverySuccess(ref TeamID _teamID)
        {
            _teamID = TeamID.One;
        }

        [HarmonyPatch(typeof(KitchenFlowMessage), nameof(KitchenFlowMessage.Initialise_DeliveryFailed))]
        [HarmonyPrefix]
        private static void Initialise_DeliveryFailed(ref TeamID _teamID)
        {
            _teamID = TeamID.One;
        }

        [HarmonyPatch(typeof(KitchenFlowMessage), nameof(KitchenFlowMessage.Serialise))]
        [HarmonyPrefix]
        private static void Serialise(ref TeamID ___m_teamID)
        {
            ___m_teamID = TeamID.One;
        }

        [HarmonyPatch(typeof(KitchenFlowMessage), nameof(KitchenFlowMessage.Deserialise))]
        [HarmonyPrefix]
        private static void Deserialise(ref TeamID ___m_teamID)
        {
            ___m_teamID = TeamID.One;
        }

        private static void ChangeTeam(int _chefIndex, ref LobbyFlowController m_lobbyFlow)
        {
            if (!(_chefIndex > -1 && _chefIndex < ServerUserSystem.m_Users.Count))
            {
                return;
            }

            User user = ServerUserSystem.m_Users._items[_chefIndex];
            if (user == null)
            {
                return;
            }

            int oneCount = 0;
            int twoCount = 0;
            for (int i = 0; i < ServerUserSystem.m_Users.Count; i++)
            {
                if (ServerUserSystem.m_Users._items[i].Team == TeamID.One)
                {
                    oneCount++;
                }
                else if (ServerUserSystem.m_Users._items[i].Team == TeamID.Two)
                {
                    twoCount++;
                }
            }

            if (oneCount <= twoCount)
            {
                user.Team = TeamID.One;
                user.Colour = m_lobbyFlow.m_redTeamColourIndex;
            }
            else
            {
                user.Team = TeamID.Two;
                user.Colour = m_lobbyFlow.m_blueTeamColourIndex;
            }
        }

        [HarmonyPatch(typeof(ServerLobbyFlowController), "AssignUsersToTeams")]
        [HarmonyPostfix]
        private static void AssignUsersToTeams(ref LobbyFlowController ___m_lobbyFlow)
        {
            for (int i = 0; i < ServerUserSystem.m_Users.Count; i++)
            {
                ServerUserSystem.m_Users._items[i].Team = TeamID.None;
            }

            for (int i = 0; i < ServerUserSystem.m_Users.Count; i++)
            {
                ChangeTeam(i, ref ___m_lobbyFlow);
            }
        }

        private static bool InPickLevel = false;

        [HarmonyPatch(typeof(ServerLobbyFlowController), "PickLevel")]
        [HarmonyPrefix]
        private static void PickLevel(ref bool ___m_bIsCoop)
        {
            InPickLevel = true;
        }

        [HarmonyPatch(typeof(SceneDirectoryData.SceneDirectoryEntry), nameof(SceneDirectoryData.SceneDirectoryEntry.GetSceneVarient))]
        [HarmonyPostfix]
        private static void GetSceneVarient(ref SceneDirectoryData.SceneDirectoryEntry __instance, ref SceneDirectoryData.PerPlayerCountDirectoryEntry __result, ref int _playerCount)
        {
            if (__result != null || _playerCount > 1 || !InPickLevel)
            {
                return;
            }

            var result = __instance.GetSceneVarient(_playerCount + 1);
            if (result != null)
            {
                OC2Modding.Log.LogWarning($"Promoted SceneVariant to 2P from 1P becuase no 1P variant exists");
                __result = result;
            }
        }

        [HarmonyPatch(typeof(ServerLobbyFlowController), "AllUsersSelected")]
        [HarmonyPostfix]
        private static void AllUsersSelected(ref LobbyFlowController ___m_lobbyFlow, ref LobbyFlowController.LobbyState ___m_state, ref bool __result)
        {
            if (!__result && ServerUserSystem.m_Users.Count == 1 && ___m_lobbyFlow.IsLocalState(___m_state))
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(ServerLobbyFlowController), "PickLevel")]
        [HarmonyPostfix]
        private static void PickLevel_Postfix()
        {
            InPickLevel = false;
        }

        [HarmonyPatch(typeof(FrontendVersusTabOptions))]
        [HarmonyPatch(nameof(FrontendVersusTabOptions.OnCouchPlayClicked))]
        private static class OnCouchPlayClicked_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                // find where we want to patch
                int injectionIndex = -1;
                for (int i = 3; i < code.Count - 1; i++)
                {
                    if (
                        code[i+1].opcode == OpCodes.Ret &&
                        code[i].opcode == OpCodes.Call &&
                        code[i-1].opcode == OpCodes.Bne_Un &&
                        code[i-2].opcode == OpCodes.Ldc_I4_1 &&
                        code[i-3].opcode == OpCodes.Callvirt
                        )
                    {
                        injectionIndex = i;
                        break;
                    }
                }

                if (injectionIndex == -1)
                {
                    OC2Modding.Log.LogError("Could not find injection spot for UpdateJoiningLobby transpilation");
                    return code;
                }

                // Instead of showing a "more users required" dialog, do nothing
                code[injectionIndex] = new CodeInstruction(OpCodes.Nop);

                // Instead of returning, do nothing
                code[injectionIndex+1] = new CodeInstruction(OpCodes.Nop);

                OC2Modding.Log.LogInfo("Successfully transpiled OnCouchPlayClicked to allow for 1P");

                return code;
            }
        }

        [HarmonyPatch(typeof(ScoreUIController), "Awake")]
        [HarmonyPostfix]
        private static void ScoreUIController_Awake(ref ScoreUIController __instance)
        {
            if (__instance.gameObject.name.Contains("Two"))
            {
                __instance.gameObject.DestroyChildren();
                __instance.gameObject.Destroy();
            }
        }

        private static bool InOnUpdateInRound = false;
        private static bool SkipOrderControllerUpdate = false;
        private static bool RemoveSpareChefs = false;

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            RemoveSpareChefs = true;
        }

        [HarmonyPatch(typeof(ServerCompetitiveFlowController), "OnUpdateInRound")]
        [HarmonyPrefix]
        private static void OnUpdateInRound_Prefix()
        {
            InOnUpdateInRound = true;
            SkipOrderControllerUpdate = false;

            if ((ServerUserSystem.m_Users.Count == 2 || ServerUserSystem.m_Users.Count == 3) && RemoveSpareChefs)
            {
                RemoveSpareChefs = false;
                string teamOneToDelete = "";
                string teamTwoToDelete = "";
                string teamTwoToTransform = "";
                Transform teamTwoTransform = null;
                
                foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
                {
                    var name = player.gameObject.name;
                    if (!name.StartsWith("Player "))
                    {
                        continue;
                    }

                    try
                    {
                        var playerID = player.RequestComponent<PlayerIDProvider>();
                        if (ServerUserSystem.m_Users.Count == 2)
                        {
                            if (playerID.GetTeam() == TeamID.One && teamOneToDelete == "")
                            {
                                teamOneToDelete = name;
                                teamTwoTransform = player.gameObject.transform;
                                continue;
                            }

                            if (playerID.GetTeam() == TeamID.Two && teamTwoToTransform == "")
                            {
                                teamTwoToTransform = name;
                                continue;
                            }
                        }

                        if (playerID.GetTeam() == TeamID.Two && teamTwoToDelete == "")
                        {
                            teamTwoToDelete = name;
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        OC2Modding.Log.LogError($"Error when checking {name}:\n{e}");
                    }
                }

                foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
                {
                    var name = player.gameObject.name;
                    if (!name.StartsWith("Player "))
                    {
                        continue;
                    }

                    try
                    {
                        var playerID = player.RequestComponent<PlayerIDProvider>();
                        
                        if (name == teamOneToDelete || name == teamTwoToDelete)
                        {
                            OC2Modding.Log.LogInfo($"Removing {name}");
                            var body = player.gameObject.RequestComponent<Rigidbody>();
                            body.detectCollisions = false;
                            player.SetActive(false);
                        }

                        if (name == teamTwoToTransform)
                        {
                            if (teamTwoTransform == null)
                            {
                                OC2Modding.Log.LogError($"Can't move {name} because no transform was collected from red team");
                            }
                            else
                            {
                                player.gameObject.transform.localPosition = teamTwoTransform.localPosition;
                                player.gameObject.transform.position = teamTwoTransform.position;
                                player.gameObject.transform.localRotation = teamTwoTransform.localRotation;
                                player.gameObject.transform.rotation = teamTwoTransform.rotation;
                                OC2Modding.Log.LogInfo($"Moved {name}");
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        [HarmonyPatch(typeof(WorkableItem), nameof(WorkableItem.GetChopTimeMultiplier))]
        [HarmonyPostfix]
        private static void GetChopTimeMultiplier(ref int __result)
        {
            if (ServerUserSystem.m_Users.Count >= 2)
            {
                __result = 1;
            }
        }

        public class AvatarSet
        {
            public int ActiveAvatar
            {
                get
                {
                    return this.m_activeAvatar;
                }
                set
                {
                    this.m_activeAvatar = value;
                    for (int i = 0; i < this.Avatars.Length; i++)
                    {
                        PlayerControls playerControls = this.Avatars[i].RequireComponent<PlayerControls>();
                        playerControls.SetDirectlyUnderPlayerControl(i == this.m_activeAvatar);
                    }
                }
            }

            public PlayerControls SelectedAvatar
            {
                get
                {
                    if (this.ActiveAvatar != -1 && this.ActiveAvatar < this.Avatars.Length)
                    {
                        return this.Avatars[this.ActiveAvatar].RequireComponent<PlayerControls>();
                    }
                    return null;
                }
            }

            private int m_activeAvatar = -1;
            public GameObject[] Avatars;
            public ILogicalButton[] SwitchButtons;
            public PlayerInputLookup.Player PlayerId;
        }

        [HarmonyPatch(typeof(PlayerSwitchingManager), "SwitchAvatars")]
        [HarmonyPrefix]
        private static bool SwitchAvatars(ref AvatarSet _set, ref int _avatarId)
        {
            if (ServerUserSystem.m_Users.Count == 1)
            {
                return true;
            }

            var player = _set.Avatars[_avatarId];
            var body = player.gameObject.RequestComponent<Rigidbody>();
            if (body.detectCollisions)
            {
                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(ServerOrderControllerBase), nameof(ServerOrderControllerBase.Update))]
        [HarmonyPrefix]
        private static bool Update()
        {
            if (!InOnUpdateInRound)
            {
                return true;
            }

            if (SkipOrderControllerUpdate)
            {
                SkipOrderControllerUpdate = false;
                return false;
            }

            SkipOrderControllerUpdate = true;
            return true;
        }

        [HarmonyPatch(typeof(ServerCompetitiveFlowController), "OnUpdateInRound")]
        [HarmonyPostfix]
        private static void OnUpdateInRound_Postfix()
        {
            InOnUpdateInRound = false;
            SkipOrderControllerUpdate = false;
        }
    }
}
