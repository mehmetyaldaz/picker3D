using System;
using System.Collections.Generic;
using Picker3D.Collectibles;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelPartCollectibleLayoutSetup : MonoBehaviour
    {
        private const int RecommendedLayoutCount = 3;

        [SerializeField] private Transform layoutAnchor;
        [SerializeField] private ManualCollectibleLayout[] layoutPrefabs;

        public bool TrySelect(
            System.Random random,
            out Transform selectedAnchor,
            out ManualCollectibleLayout selectedLayoutPrefab)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            selectedAnchor = layoutAnchor;
            selectedLayoutPrefab = null;

            if (!HasValidRuntimeConfiguration())
            {
                Debug.LogError(
                    $"{nameof(LevelPartCollectibleLayoutSetup)} on '{name}' has invalid layout references.",
                    this);
                return false;
            }

            selectedLayoutPrefab =
                layoutPrefabs[random.Next(layoutPrefabs.Length)];
            return true;
        }

        private bool HasValidRuntimeConfiguration()
        {
            if (layoutAnchor == null ||
                layoutPrefabs == null ||
                layoutPrefabs.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < layoutPrefabs.Length; index++)
            {
                if (layoutPrefabs[index] == null ||
                    layoutPrefabs[index].Capacity < 50)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            if (layoutAnchor == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartCollectibleLayoutSetup)} on '{name}' requires a layout anchor.",
                    this);
            }

            if (layoutPrefabs == null ||
                layoutPrefabs.Length != RecommendedLayoutCount)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartCollectibleLayoutSetup)} on '{name}' should have exactly {RecommendedLayoutCount} layout prefabs.",
                    this);
                return;
            }

            HashSet<ManualCollectibleLayout> uniqueLayouts = new();

            for (int index = 0; index < layoutPrefabs.Length; index++)
            {
                ManualCollectibleLayout layout = layoutPrefabs[index];

                if (layout != null && uniqueLayouts.Add(layout))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"{nameof(LevelPartCollectibleLayoutSetup)} on '{name}' has an empty or duplicate layout at index {index}.",
                    this);
            }
        }
    }
}
