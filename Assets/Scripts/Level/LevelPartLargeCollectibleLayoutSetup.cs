using System;
using System.Collections.Generic;
using Picker3D.Collectibles;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelPartLargeCollectibleLayoutSetup : MonoBehaviour
    {
        private const int RequiredPointCount = 3;

        [SerializeField] private Transform layoutAnchor;
        [SerializeField] private ManualLargeCollectibleLayout[] layoutPrefabs;

        public bool TrySelect(
            System.Random random,
            out Transform selectedAnchor,
            out ManualLargeCollectibleLayout selectedLayoutPrefab)
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
                    $"{nameof(LevelPartLargeCollectibleLayoutSetup)} on '{name}' has invalid layout references.",
                    this);
                return false;
            }

            selectedLayoutPrefab =
                layoutPrefabs[random.Next(layoutPrefabs.Length)];
            return true;
        }

        public bool HasValidRuntimeConfiguration()
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
                    layoutPrefabs[index].Capacity < RequiredPointCount)
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
                    $"{nameof(LevelPartLargeCollectibleLayoutSetup)} on '{name}' requires a layout anchor.",
                    this);
            }

            if (layoutPrefabs == null || layoutPrefabs.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartLargeCollectibleLayoutSetup)} on '{name}' requires at least one layout prefab.",
                    this);
                return;
            }

            HashSet<ManualLargeCollectibleLayout> uniqueLayouts = new();

            for (int index = 0; index < layoutPrefabs.Length; index++)
            {
                ManualLargeCollectibleLayout layout = layoutPrefabs[index];

                if (layout != null && uniqueLayouts.Add(layout))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"{nameof(LevelPartLargeCollectibleLayoutSetup)} on '{name}' has an empty or duplicate layout at index {index}.",
                    this);
            }
        }
    }
}
