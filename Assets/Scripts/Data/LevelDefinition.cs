using Picker3D.Level;
using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "Picker 3D/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "level";
        [SerializeField] private GameObject levelPrefab;

        public string LevelId => levelId;
        public GameObject LevelPrefab => levelPrefab;

        private void OnValidate()
        {
            levelId = (levelId ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning(
                    $"{nameof(LevelDefinition)} '{name}' requires a non-empty level ID.",
                    this);
            }

            if (levelPrefab != null &&
                levelPrefab.GetComponentInChildren<LevelController>(true) == null)
            {
                Debug.LogError(
                    $"Level prefab '{levelPrefab.name}' must contain a {nameof(LevelController)}.",
                    this);
            }
        }
    }
}
