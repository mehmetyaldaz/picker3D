using System;
using System.Collections;
using System.Collections.Generic;
using Picker3D.Collectibles;
using Picker3D.Core;
using Picker3D.Data;
using Picker3D.Player;
using Picker3D.World;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private LevelPartController[] orderedParts;
        [SerializeField] private FinalRampController finalRamp;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform nextLevelAnchor;

        [Header("Random Outside View")]
        [SerializeField] private Transform outsideViewPoint;
        [SerializeField] private GameObject[] outsideViewPrefabs;

        [Header("Optional Spinner Power-Up")]
        [SerializeField] private SpinnerPowerUpPickup spinnerPickupPrefab;
        [Tooltip("Independent spawn chance evaluated once for every part.")]
        [SerializeField, Range(0f, 1f)] private float spinnerPickupChance = 0.5f;

        private GameFlowController gameFlow;
        private PlayerMovement playerMovement;
        private PlayerRampMovement playerRampMovement;
        private int currentPartIndex;
        private bool isInitialized;
        private bool isActiveLevel;
        private Coroutine finalRampPreparationRoutine;

        public event Action<int, LevelPartController> CurrentPartChanged;
        public event Action<int> PartCompleted;
        public event Action<int> LevelCompleted;

        public int PartCount =>
            orderedParts != null ? orderedParts.Length : 0;

        public int CurrentPartIndex => currentPartIndex;
        public int CompletedPartCount { get; private set; }
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Transform NextLevelAnchor => nextLevelAnchor;

        public void Initialize(
            GameFlowController sharedGameFlow,
            CollectibleReleaseController ballReleaseController,
            LevelRestartService restartService,
            PlayerMovement sharedPlayerMovement,
            DifficultyConfig difficultyConfig,
            int levelSeed,
            bool activateImmediately)
        {
            if (isInitialized)
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' was initialized more than once.",
                    this);
                return;
            }

            if (!ValidateConfiguration(
                    sharedGameFlow,
                    ballReleaseController,
                    restartService,
                    sharedPlayerMovement))
            {
                enabled = false;
                return;
            }

            gameFlow = sharedGameFlow;
            playerMovement = sharedPlayerMovement;

            playerRampMovement =
                sharedPlayerMovement.GetComponent<PlayerRampMovement>();

            if (playerRampMovement == null || playerSpawnPoint == null)
            {
                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' requires a player spawn point and ramp movement component.",
                    this);
                enabled = false;
                return;
            }

            for (int index = 0; index < orderedParts.Length; index++)
            {
                LevelPartController part = orderedParts[index];
                GateController nextGate = index + 1 < orderedParts.Length
                    ? orderedParts[index + 1].EntranceGate
                    : null;

                if (difficultyConfig != null &&
                    !part.ConfigureCollectibleGeneration(
                        difficultyConfig,
                        index,
                        CreatePartSeed(levelSeed, index)))
                {
                    enabled = false;
                    return;
                }

                part.Initialize(
                    sharedGameFlow,
                    ballReleaseController,
                    restartService,
                    nextGate);
                part.SetCurrentPart(false);
                part.TransitionCompleted += HandlePartTransitionCompleted;
            }

            TrySpawnOutsideView(levelSeed);
            TrySpawnSpinnerPickups(levelSeed);

            if (finalRamp != null)
            {
                finalRamp.Initialize(sharedGameFlow);
                finalRamp.RampCompleted += HandleFinalRampCompleted;
            }

            currentPartIndex = 0;
            isInitialized = true;

            if (activateImmediately)
            {
                Activate(true);
            }
        }

        public bool Activate(bool resetPlayerPosition)
        {
            if (!isInitialized || playerRampMovement == null)
            {
                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' cannot activate before initialization.",
                    this);
                return false;
            }

            currentPartIndex = 0;
            CompletedPartCount = 0;
            playerMovement.GetComponent<PlayerSpinnerPowerUp>()?.Deactivate();

            for (int index = 0; index < orderedParts.Length; index++)
            {
                orderedParts[index].SetCurrentPart(index == 0);
            }

            if (resetPlayerPosition)
            {
                playerRampMovement.ResetForLevelStart(
                    playerSpawnPoint.position,
                    playerSpawnPoint.rotation);
            }
            else
            {
                playerRampMovement.ResetForLevelContinuation(
                    playerSpawnPoint.rotation);
            }

            ApplyMovementLimits(orderedParts[currentPartIndex]);
            isActiveLevel = true;
            CurrentPartChanged?.Invoke(
                currentPartIndex,
                orderedParts[currentPartIndex]);
            return true;
        }

        private void TrySpawnSpinnerPickups(int levelSeed)
        {
            if (spinnerPickupPrefab == null || spinnerPickupChance <= 0f)
            {
                return;
            }

            System.Random random = new(
                unchecked(levelSeed * 397 ^ 0x51ED270B));

            for (int partIndex = 0;
                 partIndex < orderedParts.Length;
                 partIndex++)
            {
                if (random.NextDouble() >= spinnerPickupChance)
                {
                    continue;
                }

                orderedParts[partIndex].SpawnSpinnerPickup(
                    spinnerPickupPrefab);
            }
        }

        private void TrySpawnOutsideView(int levelSeed)
        {
            if (outsideViewPoint == null ||
                outsideViewPrefabs == null ||
                outsideViewPrefabs.Length == 0)
            {
                return;
            }

            int availablePrefabCount = 0;

            for (int index = 0;
                 index < outsideViewPrefabs.Length;
                 index++)
            {
                if (outsideViewPrefabs[index] != null)
                {
                    availablePrefabCount++;
                }
            }

            if (availablePrefabCount == 0)
            {
                return;
            }

            System.Random random = new(
                unchecked(levelSeed * 733 ^ 73428767));
            int selectedAvailableIndex =
                random.Next(availablePrefabCount);
            GameObject selectedPrefab = null;

            for (int index = 0;
                 index < outsideViewPrefabs.Length;
                 index++)
            {
                GameObject candidate = outsideViewPrefabs[index];

                if (candidate == null)
                {
                    continue;
                }

                if (selectedAvailableIndex == 0)
                {
                    selectedPrefab = candidate;
                    break;
                }

                selectedAvailableIndex--;
            }

            if (selectedPrefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(
                selectedPrefab,
                outsideViewPoint,
                false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Color sharedColor = Color.HSVToRGB(
                (float)random.NextDouble(),
                Mathf.Lerp(
                    0.55f,
                    0.85f,
                    (float)random.NextDouble()),
                Mathf.Lerp(
                    0.75f,
                    0.95f,
                    (float)random.NextDouble()));
            ApplyOutsideViewColor(instance, sharedColor);
        }

        private void ApplyOutsideViewColor(
            GameObject outsideView,
            Color color)
        {
            int baseColorId = Shader.PropertyToID("_BaseColor");
            int colorId = Shader.PropertyToID("_Color");
            Renderer[] renderers =
                outsideView.GetComponentsInChildren<Renderer>(true);
            MaterialPropertyBlock propertyBlock =
                new MaterialPropertyBlock();

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer targetRenderer = renderers[index];
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(baseColorId, color);
                propertyBlock.SetColor(colorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        public IEnumerator OpenEntranceGateRoutine(
            float duration,
            AnimationCurve transitionCurve)
        {
            if (!isInitialized ||
                orderedParts == null ||
                orderedParts.Length == 0 ||
                orderedParts[0] == null ||
                orderedParts[0].EntranceGate == null)
            {
                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' cannot open its entrance gate.",
                    this);
                yield break;
            }

            yield return orderedParts[0].EntranceGate.OpenRoutine(
                Mathf.Max(0f, duration),
                transitionCurve);
        }

        private void OnDestroy()
        {
            if (orderedParts == null)
            {
                return;
            }

            for (int index = 0; index < orderedParts.Length; index++)
            {
                if (orderedParts[index] != null)
                {
                    orderedParts[index].TransitionCompleted -=
                        HandlePartTransitionCompleted;
                }
            }


            if (finalRamp != null)
            {
                finalRamp.RampCompleted -= HandleFinalRampCompleted;
            }

            if (finalRampPreparationRoutine != null)
            {
                StopCoroutine(finalRampPreparationRoutine);
            }
        }

        private void HandlePartTransitionCompleted(
            LevelPartController completedPart)
        {
            if (!isInitialized ||
                !isActiveLevel ||
                orderedParts[currentPartIndex] != completedPart)
            {
                return;
            }

            completedPart.SetCurrentPart(false);
            CompletedPartCount = Mathf.Max(
                CompletedPartCount,
                currentPartIndex + 1);
            PartCompleted?.Invoke(currentPartIndex);

            if (currentPartIndex >= orderedParts.Length - 1)
            {
                if (finalRamp != null)
                {
                    finalRampPreparationRoutine =
                        StartCoroutine(PrepareFinalRampRoutine());
                }
                else
                {
                    isActiveLevel = false;
                    gameFlow.CompleteLevel();
                    LevelCompleted?.Invoke(0);
                }

                return;
            }

            currentPartIndex++;
            LevelPartController nextPart = orderedParts[currentPartIndex];
            nextPart.SetCurrentPart(true);
            ApplyMovementLimits(nextPart);
            gameFlow.ResumePlaying();
            CurrentPartChanged?.Invoke(currentPartIndex, nextPart);
        }

        private IEnumerator PrepareFinalRampRoutine()
        {
            yield return finalRamp.OpenEntranceRoutine();
            finalRamp.Arm();
            gameFlow.ResumePlaying();
            finalRampPreparationRoutine = null;
        }

        private void HandleFinalRampCompleted(int gemReward)
        {
            if (!isActiveLevel)
            {
                return;
            }

            isActiveLevel = false;
            LevelCompleted?.Invoke(gemReward);
        }

        private bool ValidateConfiguration(
            GameFlowController sharedGameFlow,
            CollectibleReleaseController ballReleaseController,
            LevelRestartService restartService,
            PlayerMovement sharedPlayerMovement)
        {
            bool isValid = true;

            if (sharedGameFlow == null ||
                ballReleaseController == null ||
                restartService == null ||
                sharedPlayerMovement == null)
            {
                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' received missing shared systems.",
                    this);
                isValid = false;
            }

            if (orderedParts == null || orderedParts.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' requires at least one ordered part.",
                    this);
                return false;
            }

            if (playerSpawnPoint == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' has no player spawn point.",
                    this);
            }

            if (nextLevelAnchor == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' has no next level anchor.",
                    this);
            }

            HashSet<LevelPartController> uniqueParts = new();

            for (int index = 0; index < orderedParts.Length; index++)
            {
                LevelPartController part = orderedParts[index];

                if (part != null && uniqueParts.Add(part))
                {
                    continue;
                }

                Debug.LogError(
                    $"{nameof(LevelController)} on '{name}' has a missing or duplicate part at index {index}.",
                    this);
                isValid = false;
            }

            return isValid;
        }

        private void ApplyMovementLimits(LevelPartController part)
        {
            playerMovement.PrepareForLevel(
                part.LeftMovementLimit,
                part.RightMovementLimit);
        }

        private int CreatePartSeed(int levelSeed, int partIndex)
        {
            unchecked
            {
                return levelSeed * 486187739 + partIndex * 16777619;
            }
        }

        private void OnValidate()
        {
            spinnerPickupChance = Mathf.Clamp01(spinnerPickupChance);

            if (orderedParts == null || orderedParts.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' has no ordered level parts.",
                    this);
            }

            if (spinnerPickupChance > 0f && spinnerPickupPrefab == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' has spinner pickup chance but no pickup prefab.",
                    this);
            }

            if (outsideViewPoint != null &&
                (outsideViewPrefabs == null ||
                 outsideViewPrefabs.Length == 0))
            {
                Debug.LogWarning(
                    $"{nameof(LevelController)} on '{name}' has an outside view point but no outside view prefabs.",
                    this);
            }
        }
    }
}
