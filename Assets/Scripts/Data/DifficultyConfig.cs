using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "DifficultyConfig",
        menuName = "Picker 3D/Difficulty Configuration")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [SerializeField] private DifficultyType difficulty = DifficultyType.Easy;
        [SerializeField] private int[] requiredCounts = { 10, 15, 20 };
        [SerializeField, Min(0)] private int generatedCountBonus = 10;

        public DifficultyType Difficulty => difficulty;
        public int PartCount => requiredCounts != null ? requiredCounts.Length : 0;
        public int GeneratedCountBonus => generatedCountBonus;

        public bool TryGetPartCounts(
            int partIndex,
            out int requiredCount,
            out int generatedCount)
        {
            if (requiredCounts == null ||
                partIndex < 0 ||
                partIndex >= requiredCounts.Length)
            {
                requiredCount = 0;
                generatedCount = 0;
                return false;
            }

            requiredCount = requiredCounts[partIndex];
            generatedCount = requiredCount + generatedCountBonus;
            return true;
        }

        private void OnValidate()
        {
            generatedCountBonus = Mathf.Max(0, generatedCountBonus);

            if (requiredCounts == null || requiredCounts.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(DifficultyConfig)} '{name}' requires at least one part count.",
                    this);
                return;
            }

            for (int index = 0; index < requiredCounts.Length; index++)
            {
                requiredCounts[index] = Mathf.Max(1, requiredCounts[index]);
            }
        }
    }
}
