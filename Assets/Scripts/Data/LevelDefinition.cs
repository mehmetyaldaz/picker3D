using Picker3D.Level;
using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "Picker 3D/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private GameObject levelPrefab;

        public GameObject LevelPrefab => levelPrefab;

        private void OnValidate()
        {
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
