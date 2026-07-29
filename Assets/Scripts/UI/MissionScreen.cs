using System;
using System.Collections;
using Picker3D.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Picker3D.UI
{
    public sealed class MissionScreen : UIScreenView
    {
        [SerializeField] private Button closeButton;

        [Header("Collect Meme Mission")]
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private MissionDefinition collectMemeMission;
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_Text taskText;
        [SerializeField] private GameObject gemLogo;
        [SerializeField] private TMP_Text rewardAmount;
        [SerializeField] private GameObject completedIcon;
        [SerializeField] private TMP_Text timerText;

        [Header("Finish Levels Mission")]
        [SerializeField] private MissionDefinition finishLevelsMission;
        [SerializeField] private Image finishLevelsProgressBar;
        [SerializeField] private TMP_Text finishLevelsTaskText;
        [SerializeField] private GameObject finishLevelsGemLogo;
        [SerializeField] private TMP_Text finishLevelsRewardAmount;
        [SerializeField] private GameObject finishLevelsCompletedIcon;

        [Header("Collect Shapes Mission")]
        [SerializeField] private MissionDefinition collectShapesMission;
        [SerializeField] private Image collectShapesProgressBar;
        [SerializeField] private TMP_Text collectShapesTaskText;
        [SerializeField] private GameObject collectShapesGemLogo;
        [SerializeField] private TMP_Text collectShapesRewardAmount;
        [SerializeField] private GameObject collectShapesCompletedIcon;

        [Header("Reward Animation")]
        [SerializeField] private GemBalanceHUD gemBalanceHUD;

        private float nextTimerRefreshTime;
        private Coroutine claimRewardsRoutine;

        public event Action CloseRequested;

        private void OnEnable()
        {
            FindLocalReferences();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(
                    HandleCloseRequested);
            }

            if (missionManager != null)
            {
                missionManager.ProgressChanged +=
                    HandleProgressChanged;
                missionManager.MissionCompleted +=
                    HandleMissionCompleted;
                missionManager.MissionsReset +=
                    HandleMissionsReset;
            }

            RefreshMissions();
            RefreshTimer();
        }

        protected override void OnShown()
        {
            RefreshMissions();

            if (claimRewardsRoutine == null)
            {
                claimRewardsRoutine = StartCoroutine(
                    ClaimCompletedRewardsRoutine());
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    HandleCloseRequested);
            }

            if (missionManager != null)
            {
                missionManager.ProgressChanged -=
                    HandleProgressChanged;
                missionManager.MissionCompleted -=
                    HandleMissionCompleted;
                missionManager.MissionsReset -=
                    HandleMissionsReset;
            }

            claimRewardsRoutine = null;
        }

        private void Update()
        {
            if (Time.unscaledTime <
                nextTimerRefreshTime)
            {
                return;
            }

            nextTimerRefreshTime =
                Time.unscaledTime + 1f;
            RefreshTimer();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleProgressChanged(
            MissionDefinition mission,
            int progress,
            int target)
        {
            if (mission == collectMemeMission ||
                mission == finishLevelsMission ||
                mission == collectShapesMission)
            {
                RefreshMissions();
            }
        }

        private void HandleMissionCompleted(
            MissionDefinition mission)
        {
            if (mission == collectMemeMission ||
                mission == finishLevelsMission ||
                mission == collectShapesMission)
            {
                RefreshMissions();
            }
        }

        private void HandleMissionsReset()
        {
            RefreshMissions();
            RefreshTimer();
        }

        private void RefreshMissions()
        {
            RefreshMission(
                collectMemeMission,
                progressBar,
                taskText,
                gemLogo,
                rewardAmount,
                completedIcon,
                "Collect",
                "Meme");
            RefreshMission(
                finishLevelsMission,
                finishLevelsProgressBar,
                finishLevelsTaskText,
                finishLevelsGemLogo,
                finishLevelsRewardAmount,
                finishLevelsCompletedIcon,
                "Finish",
                "Level");
            RefreshMission(
                collectShapesMission,
                collectShapesProgressBar,
                collectShapesTaskText,
                collectShapesGemLogo,
                collectShapesRewardAmount,
                collectShapesCompletedIcon,
                "Collect",
                "Shapes");
        }

        private void RefreshMission(
            MissionDefinition mission,
            Image missionProgressBar,
            TMP_Text missionTaskText,
            GameObject missionGemLogo,
            TMP_Text missionRewardAmount,
            GameObject missionCompletedIcon,
            string actionText,
            string itemText)
        {
            if (missionManager == null ||
                mission == null)
            {
                return;
            }

            int target =
                missionManager.GetTargetAmount(
                    mission);
            int progress =
                missionManager.GetProgress(
                    mission);
            bool isRewardGranted =
                missionManager.IsRewardGranted(
                    mission);

            if (missionProgressBar != null)
            {
                missionProgressBar.fillAmount =
                    target > 0
                        ? Mathf.Clamp01(
                            (float)progress / target)
                        : 0f;
            }

            if (missionTaskText != null)
            {
                missionTaskText.text =
                    $"{actionText} {progress}/{target} {itemText}";
            }

            if (missionRewardAmount != null)
            {
                missionRewardAmount.text =
                    missionManager.GetRewardAmount(
                            mission)
                        .ToString();
                missionRewardAmount.gameObject.SetActive(
                    !isRewardGranted);
            }

            if (missionGemLogo != null)
            {
                missionGemLogo.SetActive(
                    !isRewardGranted);
            }

            if (missionCompletedIcon != null)
            {
                missionCompletedIcon.SetActive(
                    isRewardGranted);
            }
        }

        private IEnumerator ClaimCompletedRewardsRoutine()
        {
            yield return ClaimRewardRoutine(
                collectMemeMission);
            yield return ClaimRewardRoutine(
                finishLevelsMission);
            yield return ClaimRewardRoutine(
                collectShapesMission);
            claimRewardsRoutine = null;
        }

        private IEnumerator ClaimRewardRoutine(
            MissionDefinition mission)
        {
            if (missionManager == null ||
                mission == null ||
                !missionManager.TryClaimReward(mission))
            {
                yield break;
            }

            RefreshMissions();

            if (gemBalanceHUD != null)
            {
                yield return
                    gemBalanceHUD
                        .WaitForRewardAnimationRoutine();
            }
        }

        private void RefreshTimer()
        {
            if (missionManager == null ||
                timerText == null)
            {
                return;
            }

            long totalSeconds =
                missionManager
                    .GetRemainingResetSeconds();
            long hours = totalSeconds / 3600;
            long minutes =
                totalSeconds % 3600 / 60;
            long seconds = totalSeconds % 60;

            timerText.text =
                $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        private void FindLocalReferences()
        {
            if (closeButton != null)
            {
                return;
            }

            Transform closeButtonTransform =
                transform.Find("CloseButton");

            if (closeButtonTransform != null)
            {
                closeButton =
                    closeButtonTransform.GetComponent<Button>();
            }
        }

        private void OnValidate()
        {
            FindLocalReferences();
        }
    }
}
