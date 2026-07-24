using System.Collections.Generic;
using Picker3D.Collectibles;
using Picker3D.Data;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelPartCollectibleGenerator : MonoBehaviour
    {
        [SerializeField] private LevelPartCollectibleLayoutSetup layoutSetup;
        [SerializeField] private CollectiblePrefabCatalog prefabCatalog;
        [SerializeField] private CollectiblePalette colorPalette;

        [Header("Large Collectibles")]
        [SerializeField] private LevelPartLargeCollectibleLayoutSetup largeLayoutSetup;
        [SerializeField] private LargeCollectibleSplitter[] largeCollectiblePrefabs;
        [SerializeField, Range(0f, 1f)] private float largeLayoutChance;

        private readonly List<CollectibleItem> generatedItems = new();
        private GameObject activeLayoutRoot;
        private int generatedCollectibleCount;

        public int GeneratedCount => generatedCollectibleCount;

        public bool Generate(
            int requiredCount,
            int generatedCount,
            int seed)
        {
            if (!ValidateGenerationRequest(requiredCount, generatedCount))
            {
                return false;
            }

            ClearGeneratedObjects();

            System.Random random = new(seed);

            if (ShouldGenerateLargeLayout(random))
            {
                return GenerateLargeLayout(
                    requiredCount,
                    generatedCount,
                    random);
            }

            return GenerateSmallLayout(
                requiredCount,
                generatedCount,
                random);
        }

        private bool GenerateSmallLayout(
            int requiredCount,
            int generatedCount,
            System.Random random)
        {

            if (!layoutSetup.TrySelect(
                    random,
                    out Transform selectedAnchor,
                    out ManualCollectibleLayout selectedLayoutPrefab))
            {
                return false;
            }

            if (selectedLayoutPrefab.Capacity < generatedCount)
            {
                Debug.LogError(
                    $"Manual layout '{selectedLayoutPrefab.name}' has capacity {selectedLayoutPrefab.Capacity}, but part '{name}' needs {generatedCount} collectibles.",
                    this);
                return false;
            }

            ManualCollectibleLayout activeLayout =
                Instantiate(selectedLayoutPrefab, selectedAnchor);
            activeLayout.name = selectedLayoutPrefab.name;
            activeLayout.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            activeLayoutRoot = activeLayout.gameObject;

            Color partColor = colorPalette.GetRandomColor(random);
            CollectibleItem partPrefab = prefabCatalog.GetRandomPrefab(random);

            if (partPrefab == null)
            {
                ClearGeneratedObjects();
                return false;
            }

            for (int index = 0; index < generatedCount; index++)
            {
                Vector3 spawnPosition =
                    activeLayout.GetWorldPosition(index);
                CollectibleItem item = Instantiate(
                    partPrefab,
                    spawnPosition,
                    activeLayout.GetWorldRotation(index),
                    activeLayout.transform);
                item.name = $"{partPrefab.name}_{index + 1:00}";
                item.ResetForReuse();
                item.ApplyColor(partColor);
                generatedItems.Add(item);
            }

            generatedCollectibleCount = generatedCount;

            Debug.Log(
                $"Generated {generatedCount} small collectibles for '{name}' with requirement {requiredCount}.",
                this);
            return true;
        }

        private bool GenerateLargeLayout(
            int requiredCount,
            int generatedCount,
            System.Random random)
        {
            if (!largeLayoutSetup.TrySelect(
                    random,
                    out Transform selectedAnchor,
                    out ManualLargeCollectibleLayout selectedLayoutPrefab))
            {
                return false;
            }

            LargeCollectibleSplitter largePrefab =
                GetRandomLargeCollectiblePrefab(random);

            if (largePrefab == null)
            {
                return false;
            }

            ManualLargeCollectibleLayout activeLayout =
                Instantiate(selectedLayoutPrefab, selectedAnchor);
            activeLayout.name = selectedLayoutPrefab.name;
            activeLayout.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            activeLayoutRoot = activeLayout.gameObject;

            Color partColor = colorPalette.GetRandomColor(random);
            int largeObjectCount = Mathf.Min(3, activeLayout.Capacity);
            int baseSplitCount = generatedCount / largeObjectCount;
            int remainder = generatedCount % largeObjectCount;

            for (int index = 0; index < largeObjectCount; index++)
            {
                int splitCount = baseSplitCount +
                                 (index < remainder ? 1 : 0);
                LargeCollectibleSplitter largeItem = Instantiate(
                    largePrefab,
                    activeLayout.GetWorldPosition(index),
                    activeLayout.GetWorldRotation(index),
                    activeLayout.transform);
                largeItem.name = $"{largePrefab.name}_{index + 1:00}";

                if (!largeItem.Configure(
                        splitCount,
                        partColor,
                        random.Next()))
                {
                    ClearGeneratedObjects();
                    return false;
                }
            }

            generatedCollectibleCount = generatedCount;
            Debug.Log(
                $"Generated {largeObjectCount} large {largePrefab.Shape} objects for '{name}'. They split into {generatedCount} collectibles for requirement {requiredCount}.",
                this);
            return true;
        }

        private bool ShouldGenerateLargeLayout(System.Random random)
        {
            if (largeLayoutChance <= 0f)
            {
                return false;
            }

            if (!HasValidLargeConfiguration())
            {
                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' has Large Layout Chance above zero but its large collectible references are incomplete.",
                    this);
                return false;
            }

            return random.NextDouble() < largeLayoutChance;
        }

        private bool HasValidLargeConfiguration()
        {
            if (largeLayoutSetup == null ||
                !largeLayoutSetup.HasValidRuntimeConfiguration() ||
                largeCollectiblePrefabs == null ||
                largeCollectiblePrefabs.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < largeCollectiblePrefabs.Length; index++)
            {
                if (largeCollectiblePrefabs[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private LargeCollectibleSplitter GetRandomLargeCollectiblePrefab(
            System.Random random)
        {
            if (!HasValidLargeConfiguration())
            {
                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' has invalid large collectible references.",
                    this);
                return null;
            }

            return largeCollectiblePrefabs[
                random.Next(largeCollectiblePrefabs.Length)];
        }

        public void ClearGeneratedObjects()
        {
            if (activeLayoutRoot != null)
            {
                Destroy(activeLayoutRoot);
                activeLayoutRoot = null;
            }
            else
            {
                for (int index = 0; index < generatedItems.Count; index++)
                {
                    if (generatedItems[index] != null)
                    {
                        Destroy(generatedItems[index].gameObject);
                    }
                }
            }

            generatedItems.Clear();
            generatedCollectibleCount = 0;
        }

        private bool ValidateGenerationRequest(
            int requiredCount,
            int generatedCount)
        {
            if (layoutSetup == null ||
                prefabCatalog == null ||
                colorPalette == null)
            {
                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' has missing Inspector references.",
                    this);
                return false;
            }

            if (requiredCount <= 0 || generatedCount < requiredCount)
            {
                Debug.LogError(
                    $"Invalid collectible counts on '{name}': required {requiredCount}, generated {generatedCount}.",
                    this);
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (layoutSetup == null)
            {
                layoutSetup = GetComponent<LevelPartCollectibleLayoutSetup>();
            }

            if (largeLayoutSetup == null)
            {
                largeLayoutSetup =
                    GetComponent<LevelPartLargeCollectibleLayoutSetup>();
            }
        }
    }
}
