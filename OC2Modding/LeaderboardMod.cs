using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using Team17.Online;
using UnityEngine;
using UnityEngine.UI;

namespace OC2Modding
{
    public class LeaderboardMod
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

            if (!configDisplayLeaderboardScores.Value)
            {
                return;
            }
            Harmony.CreateAndPatchAll(typeof(LeaderboardMod));

            playerCountSwitchButton1 = new LogicalKeycodeButton(KeyCode.Alpha1, ControlPadInput.Button.Invalid);
            playerCountSwitchButton2 = new LogicalKeycodeButton(KeyCode.Alpha2, ControlPadInput.Button.Invalid);
            playerCountSwitchButton3 = new LogicalKeycodeButton(KeyCode.Alpha3, ControlPadInput.Button.Invalid);
            playerCountSwitchButton4 = new LogicalKeycodeButton(KeyCode.Alpha4, ControlPadInput.Button.Invalid);
            playerCountSwitchButtonCancel = new LogicalKeycodeButton(KeyCode.Alpha0, ControlPadInput.Button.Invalid);
        }

        public static void Update()
        {
            int? original = numPlayersOverride;
            if (playerCountSwitchButton1.IsDown())
            {
                numPlayersOverride = 1;
            }
            if (playerCountSwitchButton2.IsDown())
            {
                numPlayersOverride = 2;
            }
            if (playerCountSwitchButton3.IsDown())
            {
                numPlayersOverride = 3;
            }
            if (playerCountSwitchButton4.IsDown())
            {
                numPlayersOverride = 4;
            }
            if (playerCountSwitchButtonCancel.IsDown())
            {
                numPlayersOverride = null;
            }
            if (original != numPlayersOverride)
            {
                foreach (var instance in GameObject.FindObjectsOfType<WorldMapKitchenLevelIconUI>())
                {
                    UpdateLevelPreviewIfApplicable(instance);
                }
            }
        }

        private static ILogicalButton playerCountSwitchButton1;
        private static ILogicalButton playerCountSwitchButton2;
        private static ILogicalButton playerCountSwitchButton3;
        private static ILogicalButton playerCountSwitchButton4;
        private static ILogicalButton playerCountSwitchButtonCancel;
        private static int? numPlayersOverride = null;

        private static Type tUserScoreUI = typeof(WorldMapKitchenLevelIconUI).GetNestedType("UserScoreUI", System.Reflection.BindingFlags.NonPublic);
        private static FieldInfo field_m_userScoreUis = typeof(WorldMapKitchenLevelIconUI).GetField("m_userScoreUis", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static FieldInfo field_m_sceneData = typeof(WorldMapKitchenLevelIconUI).GetField("m_sceneData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static Type tSceneDirectoryEntry = field_m_sceneData.FieldType;

        private static FieldInfo field_LoadScreenOverride = tSceneDirectoryEntry.GetField("LoadScreenOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        private static FieldInfo field_container = tUserScoreUI.GetField("m_container", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static FieldInfo field_name = tUserScoreUI.GetField("m_name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static FieldInfo field_score = tUserScoreUI.GetField("m_score", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static void UpdateLevelPreviewIfApplicable(WorldMapKitchenLevelIconUI instance)
        {
            var m_userScoreUis = field_m_userScoreUis.GetValue(instance) as Array;
            if (m_userScoreUis.Length <= 4)
            {
                return;
            }
            var loadScreenOverride = field_LoadScreenOverride.GetValue(field_m_sceneData.GetValue(instance)) as Sprite;
            var levelName = loadScreenOverride?.name ?? "unknown";
            UpdateScoreUIs(m_userScoreUis, levelName);
        }

        private static bool UpdateScoreUIs(Array userScoreUIs, string levelName)
        {
            var levelScores = LeaderboardScoresRepository.Instance.GetOrStartRequestForLevel(levelName);
            var playerCount = numPlayersOverride ?? ClientUserSystem.m_Users.Count;
            var levelScoresForPlayerCount = levelScores?.SizeScores?.GetValueSafe(playerCount);

            for (int i = 4; i < 9; i++)
            {
                var ui = userScoreUIs.GetValue(i);
                (field_container.GetValue(ui) as GameObject).SetActive(true);
                if (i == 4)
                {
                    (field_name.GetValue(ui) as T17Text).text = "";
                    (field_score.GetValue(ui) as T17Text).text = "";
                    (field_container.GetValue(ui) as GameObject).SetActive(levelScores == null || levelScores.LevelIsSupported);

                }
                if (i == 5)
                {
                    (field_container.GetValue(ui) as GameObject).SetActive(levelScores == null || levelScores.LevelIsSupported);
                    if (levelScores == null)
                    {
                        (field_name.GetValue(ui) as T17Text).text = "Loading Online Scores...";
                    }
                    else
                    {
                        (field_name.GetValue(ui) as T17Text).text = "Online Scores for " + playerCount + (playerCount > 1 ? " Players" : " Player");
                    }
                    (field_score.GetValue(ui) as T17Text).text = "";
                }
                if (i >= 6)
                {
                    if (levelScores != null && levelScores.LevelIsSupported && levelScoresForPlayerCount != null &&
                        levelScoresForPlayerCount.Scores.Count > i - 6)
                    {
                        (field_container.GetValue(ui) as GameObject).SetActive(true);
                        (field_name.GetValue(ui) as T17Text).text = levelScoresForPlayerCount.Scores[i - 6].Place + ". " + levelScoresForPlayerCount.Scores[i - 6].Name;
                        (field_score.GetValue(ui) as T17Text).text = levelScoresForPlayerCount.Scores[i - 6].Score;
                    }
                    else
                    {
                        (field_container.GetValue(ui) as GameObject).SetActive(false);
                    }
                }
            }
            return levelScores != null;
        }

        private static IEnumerator UpdateScoreUIsCoroutine(Array userScoreUIs, string levelName)
        {
            // Refresh for at most 2 seconds.
            DateTime start = DateTime.Now;
            DateTime end = start + new TimeSpan(0, 0, 2);
            while (DateTime.Now < end)
            {
                if (UpdateScoreUIs(userScoreUIs, levelName))
                {
                    yield break;
                }
                yield return null;
            }
        }

        // Returns the container for user scores.
        private static GameObject RearrangeLevelPreviewLayout(GameObject layoutRoot)
        {
            try
            {
                var rootPanel = layoutRoot.transform.GetChild(0).GetChild(0).gameObject;
                var levelInfo = rootPanel.transform.GetChild(0).gameObject;
                if (rootPanel.transform.GetChild(1).gameObject.name == "RightHandSidePanel")
                {
                    // Already rearranged.
                    return rootPanel.transform.GetChild(1).gameObject;
                }
                var playerScores = new GameObject[]{
                    rootPanel.transform.GetChild(1).gameObject,
                    rootPanel.transform.GetChild(2).gameObject,
                    rootPanel.transform.GetChild(3).gameObject,
                    rootPanel.transform.GetChild(4).gameObject,
                };
                var goButton = rootPanel.transform.GetChild(5).gameObject;
                var goButtonIcon = goButton.transform.GetChild(0).gameObject;

                var rightHandPanel = new GameObject("RightHandSidePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(VerticalLayoutGroup));
                {
                    var rightHandPanel_RectTransform = rightHandPanel.GetComponent<RectTransform>();
                    rightHandPanel_RectTransform.sizeDelta = new Vector2(400, 0);
                }
                {
                    var rightHandPanel_VerticalLayoutGroup = rightHandPanel.GetComponent<VerticalLayoutGroup>();
                    rightHandPanel_VerticalLayoutGroup.spacing = 4;
                    rightHandPanel_VerticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    rightHandPanel_VerticalLayoutGroup.childControlWidth = true;
                    rightHandPanel_VerticalLayoutGroup.childControlHeight = true;
                    rightHandPanel_VerticalLayoutGroup.childForceExpandWidth = true;
                    rightHandPanel_VerticalLayoutGroup.childForceExpandHeight = false;
                }

                foreach (var playerScore in playerScores)
                {
                    playerScore.transform.SetParent(rightHandPanel.transform, false);
                }
                rightHandPanel.transform.SetParent(rootPanel.transform, false);
                rightHandPanel.transform.SetSiblingIndex(1);

                GameObject.DestroyImmediate(rootPanel.GetComponent<VerticalLayoutGroup>());
                {
                    var rootPanel_HorizontalLayoutGroup = rootPanel.AddComponent<HorizontalLayoutGroup>();
                    rootPanel_HorizontalLayoutGroup.padding = new RectOffset(50, 50, 20, 40);
                    rootPanel_HorizontalLayoutGroup.spacing = 10;
                    rootPanel_HorizontalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                    rootPanel_HorizontalLayoutGroup.childControlWidth = false;
                    rootPanel_HorizontalLayoutGroup.childControlHeight = true;
                    rootPanel_HorizontalLayoutGroup.childForceExpandWidth = false;
                    rootPanel_HorizontalLayoutGroup.childForceExpandHeight = false;
                }

                {
                    var rootPanel_ContentSizeFitter = rootPanel.GetComponent<ContentSizeFitter>();
                    rootPanel_ContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    rootPanel_ContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                {
                    var levelInfo_RectTransform = levelInfo.GetComponent<RectTransform>();
                    levelInfo_RectTransform.sizeDelta = new Vector2(400, 0);
                }

                {
                    var goButton_RectTransform = goButton.GetComponent<RectTransform>();
                    goButton_RectTransform.sizeDelta = new Vector2(0, 0);
                }

                {
                    var goButtonIcon_RectTransform = goButtonIcon.GetComponent<RectTransform>();
                    goButtonIcon_RectTransform.anchoredPosition = new Vector2(10, -290);
                }
                return rightHandPanel;

            }
            catch (Exception e)
            {
                Debug.Log("Unable to rearrange level preview " + layoutRoot.name + ": " + e);
                return null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldMapKitchenLevelIconUI), "SetUserScores")]
        public static void WorldMapSetUserScoresPatch(
            WorldMapKitchenLevelIconUI __instance
            )
        {
            var m_userScoreUis = field_m_userScoreUis.GetValue(__instance) as Array;
            var existingUserScoreContainer = RearrangeLevelPreviewLayout(__instance.gameObject);
            if (existingUserScoreContainer == null)
            {
                return;
            }
            var existingUserScoreUi = existingUserScoreContainer.transform.GetChild(0).gameObject;
            if (m_userScoreUis.Length == 4)
            {
                var newUserScoreUis = Array.CreateInstance(tUserScoreUI, 9);
                m_userScoreUis.CopyTo(newUserScoreUis, 0);
                for (int i = 0; i < 5; i++)
                {
                    var newGo = UnityEngine.Object.Instantiate(existingUserScoreUi);
                    newGo.transform.SetParent(existingUserScoreContainer.transform, false);
                    newGo.transform.SetSiblingIndex(4 + i);
                    var texts = newGo.GetComponentsInChildren<T17Text>();
                    var newUserScoreUI = Activator.CreateInstance(tUserScoreUI);
                    field_name.SetValue(newUserScoreUI, texts[0]);
                    field_score.SetValue(newUserScoreUI, texts[1]);
                    field_container.SetValue(newUserScoreUI, newGo);

                    newUserScoreUis.SetValue(newUserScoreUI, 4 + i);

                    if (i == 0)
                    {
                        newGo.GetComponent<T17Image>().color = new Color(0, 0, 0, 0);
                        newGo.GetComponent<LayoutElement>().preferredHeight = 10;
                    }
                    if (i == 1)
                    {
                        newGo.GetComponent<T17Image>().color = new Color(0, 0, 0, 0);
                        newGo.GetComponentsInChildren<T17Text>()[0].color = new Color32(8, 86, 25, 255);
                    }
                    if (i >= 2)
                    {
                        newGo.GetComponent<T17Image>().color = new Color32(77, 193, 188, 255);
                        newGo.GetComponentsInChildren<T17Text>()[0].color = new Color32(9, 107, 6, 255);
                        newGo.GetComponentsInChildren<T17Text>()[1].color = new Color32(245, 255, 73, 255);
                    }
                }
                field_m_userScoreUis.SetValue(__instance, newUserScoreUis);
                m_userScoreUis = newUserScoreUis;
            }

            var loadScreenOverride = field_LoadScreenOverride.GetValue(field_m_sceneData.GetValue(__instance)) as Sprite;
            var levelName = loadScreenOverride?.name ?? "unknown";

            __instance.StartCoroutine(UpdateScoreUIsCoroutine(m_userScoreUis, levelName));
        }
    }
}
