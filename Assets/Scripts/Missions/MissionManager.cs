using System;
using System.Collections.Generic;
using Picker3D.Core;
using Picker3D.Level;
using UnityEngine;

namespace Picker3D.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionManager : MonoBehaviour
    {
        private const string ProgressKeyPrefix =
            "picker3d_mission_progress_";
        private const string TargetKeyPrefix =
            "picker3d_mission_target_";
        private const string RewardAmountKeyPrefix =
            "picker3d_mission_reward_amount_";
        private const string RewardGrantedKeyPrefix =
            "picker3d_mission_reward_granted_";
        private const string NextResetTimeKey =
            "picker3d_mission_next_reset_utc";
        private const string ResetIntervalKey =
            "picker3d_mission_reset_interval_seconds";
        private const string MissionCollectibleLastLevelKey =
            "picker3d_mission_collectible_last_level";

        [SerializeField] private MissionDefinition[]
            activeMissions = Array.Empty<MissionDefinition>();
        [SerializeField] private GemWallet gemWallet;
        [SerializeField] private LevelManager levelManager;
        [SerializeField, Min(0.1f)]
        private float resetIntervalMinutes = 1f;

        private readonly Dictionary<string, int>
            progressByMissionId = new();
        private readonly Dictionary<string, int>
            targetByMissionId = new();
        private readonly Dictionary<string, int>
            rewardByMissionId = new();
        private readonly HashSet<string>
            rewardedMissionIds = new();

        private bool isLoaded;
        private long nextResetUnixTime;
        private float nextResetCheckTime;

        public event Action<
            MissionDefinition,
            int,
            int> ProgressChanged;
        public event Action<MissionDefinition> MissionCompleted;
        public event Action MissionsReset;

        private void Awake()
        {
            InitializeResetTimer();
            ResetMissionsIfExpired();
            LoadMissionProgress();
        }

        private void OnEnable()
        {
            if (levelManager != null)
            {
                levelManager.LevelFinished +=
                    HandleLevelFinished;
                levelManager.CollectiblesDeposited +=
                    HandleShapesDeposited;
            }
        }

        private void OnDisable()
        {
            if (levelManager != null)
            {
                levelManager.LevelFinished -=
                    HandleLevelFinished;
                levelManager.CollectiblesDeposited -=
                    HandleShapesDeposited;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextResetCheckTime)
            {
                return;
            }

            nextResetCheckTime =
                Time.unscaledTime + 1f;
            ResetMissionsIfExpired();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ResetMissionsIfExpired();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused)
            {
                ResetMissionsIfExpired();
            }
        }

        private void HandleLevelFinished(int levelNumber)
        {
            ReportProgress(
                MissionObjectiveType.FinishLevel);
        }

        private void HandleShapesDeposited(
            int depositedCount)
        {
            ReportProgress(
                MissionObjectiveType.CollectShape,
                depositedCount);
        }

        public bool HasIncompleteMission(
            MissionObjectiveType objectiveType)
        {
            if (activeMissions == null)
            {
                return false;
            }

            foreach (MissionDefinition mission
                     in activeMissions)
            {
                if (mission != null &&
                    mission.ObjectiveType == objectiveType &&
                    !IsCompleted(mission))
                {
                    return true;
                }
            }

            return false;
        }

        public void ReportProgress(
            MissionObjectiveType objectiveType,
            int amount = 1)
        {
            ResetMissionsIfExpired();

            if (amount <= 0 || activeMissions == null)
            {
                return;
            }

            foreach (MissionDefinition mission
                     in activeMissions)
            {
                if (mission == null ||
                    mission.ObjectiveType != objectiveType ||
                    IsCompleted(mission))
                {
                    continue;
                }

                int previousProgress =
                    GetProgress(mission);
                int targetAmount =
                    GetTargetAmount(mission);
                int nextProgress = Mathf.Min(
                    previousProgress + amount,
                    targetAmount);

                if (nextProgress == previousProgress)
                {
                    continue;
                }

                progressByMissionId[mission.MissionId] =
                    nextProgress;
                SaveProgress(mission, nextProgress);
                Debug.Log(
                    $"Mission '{mission.DisplayName}' progress: {nextProgress}/{targetAmount}.",
                    this);
                ProgressChanged?.Invoke(
                    mission,
                    nextProgress,
                    targetAmount);

                if (nextProgress >= targetAmount)
                {
                    MissionCompleted?.Invoke(mission);
                }
            }
        }

        public int GetProgress(
            MissionDefinition mission)
        {
            if (!IsValidMission(mission))
            {
                return 0;
            }

            EnsureLoaded();

            return progressByMissionId.TryGetValue(
                    mission.MissionId,
                    out int progress)
                ? Mathf.Clamp(
                    progress,
                    0,
                    GetTargetAmount(mission))
                : 0;
        }

        public int GetTargetAmount(
            MissionDefinition mission)
        {
            if (!IsValidMission(mission))
            {
                return 1;
            }

            EnsureLoaded();

            return targetByMissionId.TryGetValue(
                    mission.MissionId,
                    out int targetAmount)
                ? Mathf.Max(1, targetAmount)
                : mission.DefaultRequiredAmount;
        }

        public bool IsCompleted(
            MissionDefinition mission)
        {
            return IsValidMission(mission) &&
                   GetProgress(mission) >=
                   GetTargetAmount(mission);
        }

        public bool IsRewardGranted(
            MissionDefinition mission)
        {
            if (!IsValidMission(mission))
            {
                return false;
            }

            EnsureLoaded();
            return rewardedMissionIds.Contains(
                mission.MissionId);
        }

        public bool TryClaimReward(
            MissionDefinition mission)
        {
            if (!IsCompleted(mission) ||
                IsRewardGranted(mission))
            {
                return false;
            }

            if (gemWallet == null)
            {
                Debug.LogError(
                    $"{nameof(MissionManager)} on '{name}' requires a GemWallet reference to grant mission rewards.",
                    this);
                return false;
            }

            rewardedMissionIds.Add(mission.MissionId);
            PlayerPrefs.SetInt(
                GetRewardGrantedKey(mission),
                1);
            PlayerPrefs.Save();
            gemWallet.AddGems(
                GetRewardAmount(mission));
            return true;
        }

        public int GetRewardAmount(
            MissionDefinition mission)
        {
            if (!IsValidMission(mission))
            {
                return 0;
            }

            EnsureLoaded();

            return rewardByMissionId.TryGetValue(
                    mission.MissionId,
                    out int rewardAmount)
                ? Mathf.Max(0, rewardAmount)
                : mission.DefaultRewardGems;
        }

        public long GetRemainingResetSeconds()
        {
            ResetMissionsIfExpired();

            return Math.Max(
                0L,
                nextResetUnixTime -
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public void ResetAllMissions()
        {
            ResetMissionData(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        private void LoadMissionProgress()
        {
            if (isLoaded)
            {
                return;
            }

            isLoaded = true;
            progressByMissionId.Clear();
            targetByMissionId.Clear();
            rewardByMissionId.Clear();
            rewardedMissionIds.Clear();
            HashSet<string> usedMissionIds = new();

            if (activeMissions == null)
            {
                return;
            }

            foreach (MissionDefinition mission
                     in activeMissions)
            {
                if (!IsValidMission(mission))
                {
                    continue;
                }

                if (!usedMissionIds.Add(mission.MissionId))
                {
                    Debug.LogError(
                        $"{nameof(MissionManager)} on '{name}' contains the duplicate mission ID '{mission.MissionId}'.",
                        this);
                    continue;
                }

                string targetKey = GetTargetKey(mission);
                string rewardAmountKey =
                    GetRewardAmountKey(mission);
                int targetAmount;
                int rewardAmount;

                if (PlayerPrefs.HasKey(targetKey))
                {
                    targetAmount = Mathf.Max(
                        1,
                        PlayerPrefs.GetInt(
                            targetKey,
                            mission.DefaultRequiredAmount));

                    rewardAmount =
                        PlayerPrefs.HasKey(rewardAmountKey)
                            ? Mathf.Max(
                                0,
                                PlayerPrefs.GetInt(
                                    rewardAmountKey,
                                    mission.DefaultRewardGems))
                            : mission.GetRewardForTarget(
                                targetAmount);
                }
                else
                {
                    mission.GetRandomOption(
                        out targetAmount,
                        out rewardAmount);
                    PlayerPrefs.SetInt(
                        targetKey,
                        targetAmount);
                }

                PlayerPrefs.SetInt(
                    rewardAmountKey,
                    rewardAmount);
                targetByMissionId[mission.MissionId] =
                    targetAmount;
                rewardByMissionId[mission.MissionId] =
                    rewardAmount;

                int savedProgress = Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        GetProgressKey(mission),
                        0),
                    0,
                    targetAmount);
                progressByMissionId[mission.MissionId] =
                    savedProgress;

                if (PlayerPrefs.GetInt(
                        GetRewardGrantedKey(mission),
                        0) == 1)
                {
                    rewardedMissionIds.Add(
                        mission.MissionId);
                }
            }

            PlayerPrefs.Save();
        }

        private void SaveProgress(
            MissionDefinition mission,
            int progress)
        {
            PlayerPrefs.SetInt(
                GetProgressKey(mission),
                progress);
            PlayerPrefs.Save();
        }

        private bool IsValidMission(
            MissionDefinition mission)
        {
            return mission != null &&
                   !string.IsNullOrWhiteSpace(
                       mission.MissionId);
        }

        private string GetProgressKey(
            MissionDefinition mission)
        {
            return ProgressKeyPrefix +
                   mission.MissionId;
        }

        private string GetTargetKey(
            MissionDefinition mission)
        {
            return TargetKeyPrefix +
                   mission.MissionId;
        }

        private string GetRewardGrantedKey(
            MissionDefinition mission)
        {
            return RewardGrantedKeyPrefix +
                   mission.MissionId;
        }

        private string GetRewardAmountKey(
            MissionDefinition mission)
        {
            return RewardAmountKeyPrefix +
                   mission.MissionId;
        }

        private void InitializeResetTimer()
        {
            long now =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int intervalSeconds =
                GetResetIntervalSeconds();
            int savedIntervalSeconds =
                PlayerPrefs.GetInt(
                    ResetIntervalKey,
                    -1);
            bool hasSavedResetTime =
                long.TryParse(
                    PlayerPrefs.GetString(
                        NextResetTimeKey,
                        string.Empty),
                    out nextResetUnixTime);

            if (!hasSavedResetTime ||
                savedIntervalSeconds != intervalSeconds)
            {
                ScheduleNextReset(now);
            }
        }

        private void ResetMissionsIfExpired()
        {
            long now =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (nextResetUnixTime <= 0)
            {
                InitializeResetTimer();
            }

            if (now < nextResetUnixTime)
            {
                return;
            }

            ResetMissionData(now);
        }

        private void ResetMissionData(long now)
        {
            if (activeMissions != null)
            {
                foreach (MissionDefinition mission
                         in activeMissions)
                {
                    if (!IsValidMission(mission))
                    {
                        continue;
                    }

                    PlayerPrefs.DeleteKey(
                        GetProgressKey(mission));
                    PlayerPrefs.DeleteKey(
                        GetTargetKey(mission));
                    PlayerPrefs.DeleteKey(
                        GetRewardAmountKey(mission));
                    PlayerPrefs.DeleteKey(
                        GetRewardGrantedKey(mission));
                }
            }

            PlayerPrefs.DeleteKey(
                MissionCollectibleLastLevelKey);
            ScheduleNextReset(now);

            isLoaded = false;
            LoadMissionProgress();
            MissionsReset?.Invoke();
        }

        private void ScheduleNextReset(long now)
        {
            int intervalSeconds =
                GetResetIntervalSeconds();
            nextResetUnixTime =
                now + intervalSeconds;

            PlayerPrefs.SetString(
                NextResetTimeKey,
                nextResetUnixTime.ToString());
            PlayerPrefs.SetInt(
                ResetIntervalKey,
                intervalSeconds);
            PlayerPrefs.Save();
        }

        private int GetResetIntervalSeconds()
        {
            return Mathf.Max(
                1,
                Mathf.RoundToInt(
                    resetIntervalMinutes * 60f));
        }

        private void EnsureLoaded()
        {
            if (!isLoaded)
            {
                LoadMissionProgress();
            }
        }

        private void OnValidate()
        {
            activeMissions ??=
                Array.Empty<MissionDefinition>();
            resetIntervalMinutes =
                Mathf.Max(
                    0.1f,
                    resetIntervalMinutes);
        }
    }
}
