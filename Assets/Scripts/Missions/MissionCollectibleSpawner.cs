using System;
using System.Collections.Generic;
using Picker3D.Level;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionCollectibleSpawner :
        MonoBehaviour
    {
        private const string LastCollectedLevelKey =
            "picker3d_mission_collectible_last_level";

        [Header("Systems")]
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Content")]
        [SerializeField] private MissionCollectible
            collectiblePrefab;
        [SerializeField] private MissionRoute[] routePrefabs =
            Array.Empty<MissionRoute>();
        [SerializeField] private Material[] visualMaterials =
            Array.Empty<Material>();

        [Header("Randomization")]
        [SerializeField] private int randomSeed = 9176;

        private MissionCollectible spawnedCollectible;
        private MissionRoute spawnedRoute;
        private int spawnedLevelNumber = -1;
        private int lastCollectedLevelNumber = -1;

        private void Awake()
        {
            FindSystemReferences();
            lastCollectedLevelNumber =
                PlayerPrefs.GetInt(
                    LastCollectedLevelKey,
                    -1);
        }

        private void OnEnable()
        {
            if (levelManager != null)
            {
                levelManager.LevelStarted +=
                    HandleLevelStarted;
            }

            if (missionManager != null)
            {
                missionManager.MissionsReset +=
                    HandleMissionsReset;
            }
        }

        private void Start()
        {
            TrySpawnForActiveLevel();
        }

        private void OnDisable()
        {
            if (levelManager != null)
            {
                levelManager.LevelStarted -=
                    HandleLevelStarted;
            }

            if (missionManager != null)
            {
                missionManager.MissionsReset -=
                    HandleMissionsReset;
            }

            UnsubscribeFromCollectible();
        }

        private void HandleLevelStarted(int levelNumber)
        {
            if (spawnedLevelNumber != levelNumber)
            {
                DestroySpawnedObjects();
            }

            TrySpawnForActiveLevel();
        }

        private void HandleMissionsReset()
        {
            lastCollectedLevelNumber = -1;
            DestroySpawnedObjects();
            TrySpawnForActiveLevel();
        }

        private void TrySpawnForActiveLevel()
        {
            if (!ValidateRuntimeConfiguration() ||
                levelManager.ActiveLevel == null ||
                !missionManager.HasIncompleteMission(
                    MissionObjectiveType
                        .CollectMissionCollectible))
            {
                return;
            }

            int levelNumber =
                levelManager.CurrentLevelNumber;

            if (lastCollectedLevelNumber == levelNumber ||
                (spawnedLevelNumber == levelNumber &&
                 spawnedCollectible != null))
            {
                return;
            }

            List<Transform> availableAnchors =
                GetAvailablePartAnchors(
                    levelManager.ActiveLevel);

            if (availableAnchors.Count == 0)
            {
                Debug.LogWarning(
                    $"{nameof(MissionCollectibleSpawner)} found no MissionRouteAnchor in level {levelNumber}.",
                    this);
                return;
            }

            System.Random random = new(
                unchecked(
                    randomSeed ^
                    levelNumber * 73856093));
            Transform selectedAnchor =
                availableAnchors[
                    random.Next(
                        availableAnchors.Count)];
            MissionRoute selectedRoutePrefab =
                GetRandomNonNull(
                    routePrefabs,
                    random);
            Material selectedMaterial =
                GetRandomNonNull(
                    visualMaterials,
                    random);

            if (selectedRoutePrefab == null ||
                selectedMaterial == null)
            {
                Debug.LogError(
                    $"{nameof(MissionCollectibleSpawner)} requires at least one route prefab and one visual material.",
                    this);
                return;
            }

            spawnedRoute = Instantiate(
                selectedRoutePrefab,
                selectedAnchor.position,
                selectedAnchor.rotation,
                selectedAnchor);
            spawnedRoute.name =
                selectedRoutePrefab.name;
            spawnedCollectible = Instantiate(
                collectiblePrefab,
                selectedAnchor);
            spawnedCollectible.name =
                collectiblePrefab.name;
            spawnedLevelNumber = levelNumber;

            if (!spawnedCollectible.Configure(
                    missionManager,
                    playerMovement.transform,
                    spawnedRoute,
                    selectedMaterial))
            {
                DestroySpawnedObjects();
                return;
            }

            spawnedCollectible.Collected +=
                HandleCollectibleCollected;
        }

        private List<Transform> GetAvailablePartAnchors(
            LevelController level)
        {
            List<Transform> anchors = new();

            for (int partIndex = 0;
                 partIndex < level.PartCount;
                 partIndex++)
            {
                LevelPartController part =
                    level.GetPart(partIndex);
                Transform anchor =
                    part != null
                        ? part.MissionRouteAnchor
                        : null;

                if (anchor != null)
                {
                    anchors.Add(anchor);
                }
            }

            return anchors;
        }

        private T GetRandomNonNull<T>(
            T[] values,
            System.Random random)
            where T : UnityEngine.Object
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            int validCount = 0;

            foreach (T value in values)
            {
                if (value != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int selectedValidIndex =
                random.Next(validCount);

            foreach (T value in values)
            {
                if (value == null)
                {
                    continue;
                }

                if (selectedValidIndex == 0)
                {
                    return value;
                }

                selectedValidIndex--;
            }

            return null;
        }

        private void HandleCollectibleCollected(
            MissionCollectible collectible)
        {
            if (collectible != spawnedCollectible)
            {
                return;
            }

            lastCollectedLevelNumber =
                spawnedLevelNumber;
            PlayerPrefs.SetInt(
                LastCollectedLevelKey,
                lastCollectedLevelNumber);
            PlayerPrefs.Save();
            UnsubscribeFromCollectible();
            spawnedCollectible = null;
            spawnedRoute = null;
        }

        private void DestroySpawnedObjects()
        {
            UnsubscribeFromCollectible();

            if (spawnedCollectible != null)
            {
                Destroy(spawnedCollectible.gameObject);
            }

            if (spawnedRoute != null)
            {
                Destroy(spawnedRoute.gameObject);
            }

            spawnedCollectible = null;
            spawnedRoute = null;
            spawnedLevelNumber = -1;
        }

        private void UnsubscribeFromCollectible()
        {
            if (spawnedCollectible != null)
            {
                spawnedCollectible.Collected -=
                    HandleCollectibleCollected;
            }
        }

        private bool ValidateRuntimeConfiguration()
        {
            bool isValid =
                missionManager != null &&
                levelManager != null &&
                playerMovement != null &&
                collectiblePrefab != null;

            if (!isValid)
            {
                Debug.LogError(
                    $"{nameof(MissionCollectibleSpawner)} on '{name}' has missing system or prefab references.",
                    this);
            }

            return isValid;
        }

        private void FindSystemReferences()
        {
            missionManager ??=
                GetComponent<MissionManager>();
            levelManager ??=
                FindFirstObjectByType<LevelManager>(
                    FindObjectsInactive.Include);
            playerMovement ??=
                FindFirstObjectByType<PlayerMovement>(
                    FindObjectsInactive.Include);
        }

        private void OnValidate()
        {
            routePrefabs ??=
                Array.Empty<MissionRoute>();
            visualMaterials ??=
                Array.Empty<Material>();
            FindSystemReferences();
        }
    }
}
