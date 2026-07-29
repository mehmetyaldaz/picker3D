using UnityEngine;

namespace Picker3D.Missions
{
    [System.Serializable]
    public struct MissionRewardOption
    {
        [SerializeField, Min(1)]
        [InspectorName("Required Amount")]
        private int requiredAmount;
        [SerializeField, Min(0)]
        [InspectorName("Reward Gems")]
        private int rewardGems;

        public int RequiredAmount =>
            Mathf.Max(1, requiredAmount);
        public int RewardGems =>
            Mathf.Max(0, rewardGems);

        public MissionRewardOption(
            int requiredAmount,
            int rewardGems)
        {
            this.requiredAmount =
                Mathf.Max(1, requiredAmount);
            this.rewardGems =
                Mathf.Max(0, rewardGems);
        }
    }

    [CreateAssetMenu(
        fileName = "MissionDefinition",
        menuName = "Picker 3D/Missions/Mission Definition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string missionId =
            "collect_memes";
        [SerializeField] private string displayName =
            "Collect Memes";

        [Header("Objective")]
        [SerializeField] private MissionObjectiveType
            objectiveType =
                MissionObjectiveType
                    .CollectMissionCollectible;

        [Header("Random Mission Options")]
        [SerializeField] private MissionRewardOption[]
            rewardOptions =
            {
                new MissionRewardOption(5, 500),
                new MissionRewardOption(10, 600),
                new MissionRewardOption(15, 700),
                new MissionRewardOption(20, 800),
                new MissionRewardOption(25, 900)
            };

        public string MissionId => missionId;
        public string DisplayName => displayName;
        public MissionObjectiveType ObjectiveType =>
            objectiveType;
        public int DefaultRequiredAmount =>
            rewardOptions != null &&
            rewardOptions.Length > 0
                ? rewardOptions[0].RequiredAmount
                : 1;
        public int DefaultRewardGems =>
            rewardOptions != null &&
            rewardOptions.Length > 0
                ? rewardOptions[0].RewardGems
                : 0;

        public void GetRandomOption(
            out int requiredAmount,
            out int selectedRewardGems)
        {
            if (rewardOptions == null ||
                rewardOptions.Length == 0)
            {
                requiredAmount = 1;
                selectedRewardGems = 0;
                return;
            }

            MissionRewardOption selectedOption =
                rewardOptions[
                    Random.Range(
                        0,
                        rewardOptions.Length)];
            requiredAmount =
                selectedOption.RequiredAmount;
            selectedRewardGems =
                selectedOption.RewardGems;
        }

        public int GetRewardForTarget(
            int requiredAmount)
        {
            if (rewardOptions != null)
            {
                foreach (MissionRewardOption option
                         in rewardOptions)
                {
                    if (option.RequiredAmount ==
                        requiredAmount)
                    {
                        return option.RewardGems;
                    }
                }
            }

            return DefaultRewardGems;
        }

        private void OnValidate()
        {
            missionId = (missionId ?? string.Empty).Trim();
            displayName =
                (displayName ?? string.Empty).Trim();
            if (rewardOptions == null ||
                rewardOptions.Length != 5)
            {
                System.Array.Resize(
                    ref rewardOptions,
                    5);
            }
        }
    }
}
