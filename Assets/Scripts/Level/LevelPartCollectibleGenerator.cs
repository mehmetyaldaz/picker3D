using System.Collections.Generic;
using Picker3D.Collectibles;
using Picker3D.Data;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelPartCollectibleGenerator : MonoBehaviour
    {
        private enum GenerationMode
        {
            Small,
            Large,
            Flying
        }

        [SerializeField] private LevelPartCollectibleLayoutSetup layoutSetup;
        [SerializeField] private CollectiblePrefabCatalog prefabCatalog;
        [SerializeField] private CollectiblePalette colorPalette;

        [Header("Large Collectibles")]
        [SerializeField] private LevelPartLargeCollectibleLayoutSetup largeLayoutSetup;
        [SerializeField] private LargeCollectibleSplitter[] largeCollectiblePrefabs;
        [SerializeField, Range(0f, 1f)] private float largeLayoutChance;

        [Header("Flying Collectibles")]
        [SerializeField] private Transform flyingSpawnAnchor;
        [SerializeField] private FlyingCollectibleSpawner flyingSpawnerPrefab;
        [SerializeField] private FlyingCollectibleRoute[] flyingRoutePrefabs;
        [SerializeField, Range(0f, 1f)] private float flyingLayoutChance;

        private readonly List<CollectibleItem> generatedItems = new();
        private GameObject activeLayoutRoot;
        private FlyingCollectibleSpawner activeFlyingSpawner;
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
            GenerationMode generationMode =
                SelectGenerationMode(random);

            if (generationMode == GenerationMode.Flying)
            {
                return GenerateFlyingLayout(
                    requiredCount,
                    generatedCount,
                    random);
            }

            if (generationMode == GenerationMode.Large)
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

        public void SetPartActive(bool isActive)
        {
            activeFlyingSpawner?.SetRunning(isActive);
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

        private bool GenerateFlyingLayout(
            int requiredCount,
            int generatedCount,
            System.Random random)
        {
            if (!HasValidFlyingConfiguration())
            {
                return false;
            }

            FlyingCollectibleRoute routePrefab =
                GetRandomFlyingRoutePrefab(random);
            CollectibleItem partPrefab =
                prefabCatalog.GetRandomPrefab(random);

            if (routePrefab == null || partPrefab == null)
            {
                return false;
            }

            GameObject flyingContentRoot =
                new GameObject("FlyingCollectibleContent");
            flyingContentRoot.transform.SetParent(
                flyingSpawnAnchor,
                false);
            activeLayoutRoot = flyingContentRoot;

            FlyingCollectibleRoute activeRoute =
                Instantiate(
                    routePrefab,
                    flyingContentRoot.transform);
            activeRoute.name = routePrefab.name;
            activeRoute.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);

            activeFlyingSpawner = Instantiate(
                flyingSpawnerPrefab,
                flyingContentRoot.transform);
            activeFlyingSpawner.name =
                flyingSpawnerPrefab.name;
            activeFlyingSpawner.transform
                .SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);

            Color partColor =
                colorPalette.GetRandomColor(random);

            if (!activeFlyingSpawner.Configure(
                    activeRoute,
                    partPrefab,
                    flyingContentRoot.transform,
                    partColor,
                    generatedCount,
                    random.Next()))
            {
                ClearGeneratedObjects();
                return false;
            }

            generatedCollectibleCount = generatedCount;
            Debug.Log(
                $"Prepared flying collectible dropper for '{name}'. It will drop {generatedCount} {partPrefab.Shape} collectibles for requirement {requiredCount}.",
                this);
            return true;
        }

        private GenerationMode SelectGenerationMode(
            System.Random random)
        {
            double selection = random.NextDouble();

            if (flyingLayoutChance > 0f &&
                selection < flyingLayoutChance)
            {
                if (HasValidFlyingConfiguration())
                {
                    return GenerationMode.Flying;
                }

                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' selected Flying mode but its flying references are incomplete.",
                    this);
                return GenerationMode.Small;
            }

            if (largeLayoutChance > 0f &&
                selection <
                flyingLayoutChance +
                largeLayoutChance)
            {
                if (HasValidLargeConfiguration())
                {
                    return GenerationMode.Large;
                }

                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' selected Large mode but its large collectible references are incomplete.",
                    this);
            }

            return GenerationMode.Small;
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

        private bool HasValidFlyingConfiguration()
        {
            if (flyingSpawnAnchor == null ||
                flyingSpawnerPrefab == null ||
                flyingRoutePrefabs == null ||
                flyingRoutePrefabs.Length == 0)
            {
                return false;
            }

            for (int index = 0;
                 index < flyingRoutePrefabs.Length;
                 index++)
            {
                if (flyingRoutePrefabs[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private FlyingCollectibleRoute GetRandomFlyingRoutePrefab(
            System.Random random)
        {
            if (!HasValidFlyingConfiguration())
            {
                Debug.LogError(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' has invalid flying collectible references.",
                    this);
                return null;
            }

            return flyingRoutePrefabs[
                random.Next(flyingRoutePrefabs.Length)];
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
            activeFlyingSpawner = null;

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

            if (flyingLayoutChance +
                largeLayoutChance > 1f)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartCollectibleGenerator)} on '{name}' has Flying and Large chances whose sum is above 1. Small mode will not be selected.",
                    this);
            }
        }
    }
}
