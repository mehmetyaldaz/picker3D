using System;
using System.Collections.Generic;
using Picker3D.Collectibles;
using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "CollectiblePrefabCatalog",
        menuName = "Picker 3D/Collectible Prefab Catalog")]
    public sealed class CollectiblePrefabCatalog : ScriptableObject
    {
        [SerializeField] private CollectiblePrefabEntry[] entries;

        public int EntryCount => entries != null ? entries.Length : 0;

        public CollectibleItem GetRandomPrefab(System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (!TryGetTotalWeight(out int totalWeight))
            {
                Debug.LogError(
                    $"{nameof(CollectiblePrefabCatalog)} '{name}' has no valid prefab entries.",
                    this);
                return null;
            }

            int selection = random.Next(totalWeight);

            for (int index = 0; index < entries.Length; index++)
            {
                CollectiblePrefabEntry entry = entries[index];

                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                selection -= entry.SelectionWeight;

                if (selection < 0)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        private bool TryGetTotalWeight(out int totalWeight)
        {
            totalWeight = 0;

            if (entries == null)
            {
                return false;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                CollectiblePrefabEntry entry = entries[index];

                if (entry != null && entry.Prefab != null)
                {
                    totalWeight += entry.SelectionWeight;
                }
            }

            return totalWeight > 0;
        }

        private void OnValidate()
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(CollectiblePrefabCatalog)} '{name}' has no prefab entries.",
                    this);
                return;
            }

            HashSet<CollectibleShape> shapes = new();

            for (int index = 0; index < entries.Length; index++)
            {
                CollectiblePrefabEntry entry = entries[index];

                if (entry == null || entry.Prefab == null)
                {
                    Debug.LogError(
                        $"{nameof(CollectiblePrefabCatalog)} '{name}' has an empty entry at index {index}.",
                        this);
                    continue;
                }

                if (!shapes.Add(entry.Prefab.Shape))
                {
                    Debug.LogWarning(
                        $"{nameof(CollectiblePrefabCatalog)} '{name}' contains more than one {entry.Prefab.Shape} prefab.",
                        this);
                }
            }
        }
    }
}
