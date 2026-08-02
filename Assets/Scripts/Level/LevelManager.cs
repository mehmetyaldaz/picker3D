using System;
using System.Collections;
using Picker3D.Collectibles;
using Picker3D.Core;
using Picker3D.Data;
using Picker3D.Player;
using Picker3D.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelManager : MonoBehaviour
    {
        private const string CurrentLevelIndexPlayerPrefsKey =
            "picker3d_current_level_index";

        [Header("Level Source")]
        [FormerlySerializedAs("levelDefinition")]
        [SerializeField] private LevelDefinition threePartLevelDefinition;
        [SerializeField] private LevelDefinition fourPartLevelDefinition;
        [SerializeField] private LevelDefinition fivePartLevelDefinition;
        [SerializeField] private Transform levelParent;

        [Header("Level Length Progression")]
        [SerializeField, Min(1)] private int fourPartUnlockLevel = 11;
        [SerializeField, Min(2)] private int fivePartUnlockLevel = 21;

        [Header("Runtime Generation")]
        [SerializeField] private bool generateRuntimeCollectibles;
        [SerializeField] private DifficultyConfig[] difficultyConfigs;
        [SerializeField] private int levelSeed = 1;

        [Header("Infinite Gameplay")]
        [SerializeField] private bool infiniteGameplay = true;
        [SerializeField, Min(0f)] private float nextLevelDelay = 0.1f;
        [SerializeField, Min(0f)] private float nextGateOpenDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float playerTransferDuration = 1f;
        [SerializeField, Min(0f)] private float playerTransferArcHeight = 1.5f;
        [SerializeField] private AnimationCurve playerTransferCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Shared Systems")]
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private CollectibleReleaseController ballReleaseController;
        [SerializeField] private LevelRestartService restartService;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private ForwardOnlyCameraTarget cameraFollowTarget;
        [SerializeField] private GemWallet gemWallet;
        [SerializeField] private GemBalanceHUD gemBalanceHUD;

        private GameObject activeLevelRoot;
        private GameObject preparedLevelRoot;
        private GameObject previousLevelRoot;
        private LevelController preparedLevel;
        private DifficultyConfig activeDifficulty;
        private DifficultyConfig preparedDifficulty;
        private Coroutine levelTransitionRoutine;
        private int currentLevelIndex;

        public event Action<int> LevelStarted;
        public event Action<int> LevelFinished;
        public event Action<int> CollectiblesDeposited;

        public LevelController ActiveLevel { get; private set; }
        public int CurrentLevelNumber => currentLevelIndex + 1;

        public void ResetLevelProgress()
        {
            currentLevelIndex = 0;
            SaveLevelIndex(currentLevelIndex);
        }

        public void AddLevelProgress(int levelCount)
        {
            if (levelCount <= 0)
            {
                return;
            }

            currentLevelIndex = (int)Math.Min(
                (long)currentLevelIndex + levelCount,
                int.MaxValue - 1L);
            SaveLevelIndex(currentLevelIndex);
        }

        private void Awake()
        {
            if (gemBalanceHUD == null)
            {
                gemBalanceHUD =
                    FindFirstObjectByType<GemBalanceHUD>(
                        FindObjectsInactive.Include);
            }

            if (!ValidateRuntimeReferences())
            {
                enabled = false;
                return;
            }

            currentLevelIndex = LoadSavedLevelIndex();
            activeDifficulty = SelectDifficulty(currentLevelIndex);

            activeLevelRoot = InstantiateDefinitionRoot(
                CurrentLevelNumber);
            ActiveLevel = FindLevelController(activeLevelRoot);

            if (!InitializeLevel(
                    ActiveLevel,
                    currentLevelIndex,
                    true,
                    activeDifficulty))
            {
                enabled = false;
                return;
            }

            SubscribeToActiveLevel();

            if (infiniteGameplay)
            {
                restartService.SetRuntimeRestartRoutine(
                    RestartActiveLevelRoutine);

                if (!PrepareFollowingLevel())
                {
                    enabled = false;
                    return;
                }
            }

            LevelStarted?.Invoke(CurrentLevelNumber);
        }

        private void OnDestroy()
        {
            UnsubscribeFromActiveLevel();

            if (restartService != null)
            {
                restartService.ClearRuntimeRestartRoutine(
                    RestartActiveLevelRoutine);
            }

            if (levelTransitionRoutine != null)
            {
                StopCoroutine(levelTransitionRoutine);
            }
        }

        private bool ValidateRuntimeReferences()
        {
            bool isValid =
                gameFlow != null &&
                ballReleaseController != null &&
                restartService != null &&
                playerMovement != null &&
                gemWallet != null &&
                HasValidLevelDefinitions() &&
                (!generateRuntimeCollectibles ||
                 HasAvailableDifficultyConfig());

            if (!isValid)
            {
                Debug.LogError(
                    $"{nameof(LevelManager)} on '{name}' has missing Inspector references.",
                    this);
            }

            return isValid;
        }

        private bool InitializeLevel(
            LevelController controller,
            int levelIndex,
            bool activateImmediately,
            DifficultyConfig difficulty)
        {
            if (controller == null)
            {
                Debug.LogError(
                    $"{nameof(LevelManager)} on '{name}' could not create level {levelIndex + 1}.",
                    this);
                return false;
            }

            controller.Initialize(
                gameFlow,
                ballReleaseController,
                playerMovement,
                generateRuntimeCollectibles ? difficulty : null,
                CreateLevelSeed(levelIndex),
                activateImmediately);

            if (controller.enabled &&
                generateRuntimeCollectibles &&
                difficulty != null)
            {
                Debug.Log(
                    $"Level {levelIndex + 1} difficulty: {difficulty.Difficulty}.",
                    controller);
            }

            return controller.enabled;
        }

        private bool PrepareFollowingLevel()
        {
            if (ActiveLevel == null ||
                ActiveLevel.NextLevelAnchor == null)
            {
                Debug.LogError(
                    $"Active level '{ActiveLevel?.name}' requires a Next Level Anchor for infinite gameplay.",
                    ActiveLevel);
                return false;
            }

            int nextLevelIndex = currentLevelIndex + 1;
            preparedDifficulty = SelectDifficulty(nextLevelIndex);
            preparedLevelRoot = InstantiateDefinitionRoot(
                nextLevelIndex + 1);
            preparedLevel = FindLevelController(preparedLevelRoot);

            if (preparedLevel == null ||
                preparedLevel.PlayerSpawnPoint == null)
            {
                Debug.LogError(
                    "Prepared level requires a Player Spawn Point.",
                    preparedLevelRoot);
                return false;
            }

            AlignPreparedLevel(
                preparedLevelRoot.transform,
                preparedLevel.PlayerSpawnPoint,
                ActiveLevel.NextLevelAnchor);

            return InitializeLevel(
                preparedLevel,
                nextLevelIndex,
                false,
                preparedDifficulty);
        }

        private void AlignPreparedLevel(
            Transform preparedRoot,
            Transform preparedSpawnPoint,
            Transform connectionAnchor)
        {
            Quaternion rotationDelta =
                connectionAnchor.rotation *
                Quaternion.Inverse(preparedSpawnPoint.rotation);
            preparedRoot.rotation =
                rotationDelta * preparedRoot.rotation;
            preparedRoot.position +=
                connectionAnchor.position - preparedSpawnPoint.position;
        }

        private void HandleActiveLevelCompleted(int gemReward)
        {
            if (!infiniteGameplay || levelTransitionRoutine != null)
            {
                return;
            }

            gemWallet.AddGems(gemReward);
            SaveLevelIndex(GetNextLevelIndex(currentLevelIndex));
            LevelFinished?.Invoke(CurrentLevelNumber);
            levelTransitionRoutine = StartCoroutine(
                AdvanceToPreparedLevelRoutine());
        }

        private IEnumerator AdvanceToPreparedLevelRoutine()
        {
            if (gemBalanceHUD != null)
            {
                yield return
                    gemBalanceHUD.WaitForRewardAnimationRoutine();
            }

            if (nextLevelDelay > 0f)
            {
                yield return new WaitForSeconds(nextLevelDelay);
            }

            UnsubscribeFromActiveLevel();
            ballReleaseController.ReleaseCollectedItems();

            PlayerRampMovement rampMovement =
                playerMovement.GetComponent<PlayerRampMovement>();

            if (rampMovement == null ||
                preparedLevel == null ||
                preparedLevel.PlayerSpawnPoint == null)
            {
                Debug.LogError(
                    $"{nameof(LevelManager)} cannot animate the player to the prepared level.",
                    this);
                enabled = false;
                yield break;
            }

            Coroutine gateRoutine = StartCoroutine(
                preparedLevel.OpenEntranceGateRoutine(
                    nextGateOpenDuration,
                    playerTransferCurve));

            cameraFollowTarget?.BeginLevelTransfer();

            yield return rampMovement.FlyToLevelStartRoutine(
                preparedLevel.PlayerSpawnPoint.position,
                preparedLevel.PlayerSpawnPoint.rotation,
                playerTransferDuration,
                playerTransferArcHeight,
                playerTransferCurve);

            cameraFollowTarget?.CompleteLevelTransfer();

            if (gateRoutine != null)
            {
                yield return gateRoutine;
            }

            if (previousLevelRoot != null)
            {
                Destroy(previousLevelRoot);
            }

            previousLevelRoot = activeLevelRoot;
            activeLevelRoot = preparedLevelRoot;
            ActiveLevel = preparedLevel;
            activeDifficulty = preparedDifficulty;
            preparedLevelRoot = null;
            preparedLevel = null;
            preparedDifficulty = null;
            currentLevelIndex =
                GetNextLevelIndex(currentLevelIndex);
            SaveLevelIndex(currentLevelIndex);

            SubscribeToActiveLevel();

            if (!ActiveLevel.Activate(false))
            {
                enabled = false;
                yield break;
            }

            if (!PrepareFollowingLevel())
            {
                enabled = false;
                yield break;
            }

            gameFlow.WaitForLevelContinue();
            LevelStarted?.Invoke(CurrentLevelNumber);
            levelTransitionRoutine = null;
        }

        private IEnumerator RestartActiveLevelRoutine()
        {
            UnsubscribeFromActiveLevel();
            ballReleaseController.ReleaseCollectedItems();

            Vector3 activePosition = activeLevelRoot.transform.position;
            Quaternion activeRotation = activeLevelRoot.transform.rotation;

            if (preparedLevelRoot != null)
            {
                Destroy(preparedLevelRoot);
                preparedLevelRoot = null;
                preparedLevel = null;
                preparedDifficulty = null;
            }

            Destroy(activeLevelRoot);
            activeLevelRoot = null;
            ActiveLevel = null;
            yield return null;

            activeLevelRoot = InstantiateDefinitionRoot(
                CurrentLevelNumber);
            activeLevelRoot.transform.SetPositionAndRotation(
                activePosition,
                activeRotation);
            ActiveLevel = FindLevelController(activeLevelRoot);
            activeDifficulty = SelectDifficulty(currentLevelIndex);

            if (!InitializeLevel(
                    ActiveLevel,
                    currentLevelIndex,
                    true,
                    activeDifficulty))
            {
                enabled = false;
                yield break;
            }

            cameraFollowTarget?.RecenterForCurrentPlayer();
            SubscribeToActiveLevel();

            if (!PrepareFollowingLevel())
            {
                enabled = false;
                yield break;
            }

            gameFlow.PrepareToPlay();
            LevelStarted?.Invoke(CurrentLevelNumber);
        }

        private GameObject InstantiateDefinitionRoot(int levelNumber)
        {
            LevelDefinition selectedDefinition =
                SelectLevelDefinition(levelNumber);

            if (selectedDefinition == null ||
                selectedDefinition.LevelPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(
                selectedDefinition.LevelPrefab,
                levelParent);
            instance.name =
                $"{selectedDefinition.LevelPrefab.name}_{levelNumber:000}";
            return instance;
        }

        private LevelDefinition SelectLevelDefinition(int levelNumber)
        {
            int safeLevelNumber = Mathf.Max(1, levelNumber);

            if (safeLevelNumber < fourPartUnlockLevel)
            {
                return threePartLevelDefinition;
            }

            int availableTemplateCount =
                safeLevelNumber < fivePartUnlockLevel
                    ? 2
                    : 3;
            System.Random random = new System.Random(
                unchecked(
                    levelSeed * 73856093 ^
                    safeLevelNumber * 19349663));

            return random.Next(availableTemplateCount) switch
            {
                0 => threePartLevelDefinition,
                1 => fourPartLevelDefinition,
                _ => fivePartLevelDefinition
            };
        }

        private bool HasValidLevelDefinitions()
        {
            return
                threePartLevelDefinition != null &&
                threePartLevelDefinition.LevelPrefab != null &&
                fourPartLevelDefinition != null &&
                fourPartLevelDefinition.LevelPrefab != null &&
                fivePartLevelDefinition != null &&
                fivePartLevelDefinition.LevelPrefab != null;
        }

        private LevelController FindLevelController(GameObject levelRoot)
        {
            if (levelRoot == null)
            {
                return null;
            }

            LevelController controller =
                levelRoot.GetComponentInChildren<LevelController>(true);

            if (controller == null)
            {
                Debug.LogError(
                    $"Instantiated level '{levelRoot.name}' has no {nameof(LevelController)}.",
                    levelRoot);
            }

            return controller;
        }

        private void SubscribeToActiveLevel()
        {
            if (ActiveLevel != null)
            {
                ActiveLevel.LevelCompleted +=
                    HandleActiveLevelCompleted;
                ActiveLevel.CollectiblesDeposited +=
                    HandleCollectiblesDeposited;
            }
        }

        private void UnsubscribeFromActiveLevel()
        {
            if (ActiveLevel != null)
            {
                ActiveLevel.LevelCompleted -=
                    HandleActiveLevelCompleted;
                ActiveLevel.CollectiblesDeposited -=
                    HandleCollectiblesDeposited;
            }
        }

        private void HandleCollectiblesDeposited(
            int depositedCount)
        {
            CollectiblesDeposited?.Invoke(
                depositedCount);
        }

        private int CreateLevelSeed(int levelIndex)
        {
            unchecked
            {
                return levelSeed + levelIndex * 104729;
            }
        }

        private int LoadSavedLevelIndex()
        {
            int savedIndex = PlayerPrefs.GetInt(
                CurrentLevelIndexPlayerPrefsKey,
                0);
            return Mathf.Clamp(
                savedIndex,
                0,
                int.MaxValue - 1);
        }

        private void SaveLevelIndex(int levelIndex)
        {
            int safeLevelIndex = Mathf.Clamp(
                levelIndex,
                0,
                int.MaxValue - 1);
            PlayerPrefs.SetInt(
                CurrentLevelIndexPlayerPrefsKey,
                safeLevelIndex);
            PlayerPrefs.Save();
        }

        private int GetNextLevelIndex(int levelIndex)
        {
            return levelIndex < int.MaxValue - 1
                ? levelIndex + 1
                : int.MaxValue - 1;
        }

        private DifficultyConfig SelectDifficulty(int levelIndex)
        {
            if (!generateRuntimeCollectibles ||
                !HasAvailableDifficultyConfig())
            {
                return null;
            }

            int availableCount = 0;

            for (int index = 0;
                 index < difficultyConfigs.Length;
                 index++)
            {
                if (difficultyConfigs[index] != null)
                {
                    availableCount++;
                }
            }

            System.Random random = new System.Random(
                unchecked(
                    CreateLevelSeed(levelIndex) ^
                    (levelIndex + 1) * 19349663));
            int selectedAvailableIndex =
                random.Next(availableCount);

            for (int index = 0;
                 index < difficultyConfigs.Length;
                 index++)
            {
                DifficultyConfig config =
                    difficultyConfigs[index];

                if (config == null)
                {
                    continue;
                }

                if (selectedAvailableIndex == 0)
                {
                    return config;
                }

                selectedAvailableIndex--;
            }

            return null;
        }

        private bool HasAvailableDifficultyConfig()
        {
            if (difficultyConfigs == null ||
                difficultyConfigs.Length == 0)
            {
                return false;
            }

            for (int index = 0;
                 index < difficultyConfigs.Length;
                 index++)
            {
                if (difficultyConfigs[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            fourPartUnlockLevel = Mathf.Max(
                1,
                fourPartUnlockLevel);
            fivePartUnlockLevel = Mathf.Max(
                fourPartUnlockLevel + 1,
                fivePartUnlockLevel);
            nextLevelDelay = Mathf.Max(0f, nextLevelDelay);
            nextGateOpenDuration = Mathf.Max(0f, nextGateOpenDuration);
            playerTransferDuration = Mathf.Max(
                0.01f,
                playerTransferDuration);
            playerTransferArcHeight = Mathf.Max(
                0f,
                playerTransferArcHeight);

            if (generateRuntimeCollectibles &&
                !HasAvailableDifficultyConfig())
            {
                Debug.LogWarning(
                    $"{nameof(LevelManager)} on '{name}' requires at least one difficulty config when runtime generation is enabled.",
                    this);
            }

            if (!HasValidLevelDefinitions())
            {
                Debug.LogWarning(
                    $"{nameof(LevelManager)} on '{name}' requires valid 3, 4 and 5 part level definitions.",
                    this);
            }
        }
    }
}
