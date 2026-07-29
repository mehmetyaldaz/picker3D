using System;
using System.Collections;
using Picker3D.Collectibles;
using Picker3D.Core;
using Picker3D.Data;
using Picker3D.Player;
using Picker3D.World;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class LevelPartController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LevelPartConfig config;

        [Header("Part Components")]
        [SerializeField] private DropAreaTrigger dropAreaTrigger;
        [SerializeField] private DropboxCollectibleCounter dropboxBallCounter;
        [SerializeField] private DropboxMovementController dropboxMovement;
        [SerializeField] private BridgeShapeController bridgeShape;
        [SerializeField] private SurfaceColorController bridgeColorController;
        [SerializeField] private GateController entranceGate;
        [SerializeField] private LevelPartCollectibleGenerator collectibleGenerator;
        [SerializeField] private DropboxRequirementDisplay requirementDisplay;
        [SerializeField] private Transform spinnerPickupSpawnPoint;
        [SerializeField] private Transform missionRouteAnchor;

        [Header("Player Movement Limits")]
        [SerializeField] private Transform leftMovementLimit;
        [SerializeField] private Transform rightMovementLimit;
        [SerializeField] private Transform movementRestrictionAnchor;

        private Coroutine resolutionRoutine;
        private GameFlowController gameFlow;
        private CollectibleReleaseController ballReleaseController;
        private LevelRestartService restartService;
        private GateController nextGate;
        private bool isInitialized;
        private bool isCurrentPart;
        private bool isResolving;
        private int runtimeRequiredCollectibleCount = -1;

        public event Action<LevelPartController> PartSucceeded;
        public event Action<LevelPartController> PartFailed;
        public event Action<LevelPartController> TransitionCompleted;
        public event Action<int> CollectiblesDeposited;

        public int RequiredBallCount =>
            runtimeRequiredCollectibleCount >= 0
                ? runtimeRequiredCollectibleCount
                : config != null
                    ? config.RequiredBallCount
                    : 0;

        public int CountedBallCount =>
            dropboxBallCounter != null ? dropboxBallCounter.Count : 0;

        public GateController EntranceGate => entranceGate;
        public Transform LeftMovementLimit => leftMovementLimit;
        public Transform RightMovementLimit => rightMovementLimit;
        public Transform MissionRouteAnchor
        {
            get
            {
                ResolveMissionRouteAnchor();
                return missionRouteAnchor;
            }
        }
        public Transform MovementRestrictionAnchor
        {
            get
            {
                ResolveMovementRestrictionAnchor();
                return movementRestrictionAnchor;
            }
        }

        private void Start()
        {
            ResolveMovementRestrictionAnchor();
            ResolveMissionRouteAnchor();

            if (!ValidateReferences())
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (dropAreaTrigger != null)
            {
                dropAreaTrigger.PlayerEntered += HandlePlayerEntered;
            }
        }

        private void OnDisable()
        {
            if (dropAreaTrigger != null)
            {
                dropAreaTrigger.PlayerEntered -= HandlePlayerEntered;
            }

            if (resolutionRoutine != null)
            {
                StopCoroutine(resolutionRoutine);
                resolutionRoutine = null;
            }
        }

        private void HandlePlayerEntered(PlayerMovement playerMovement)
        {
            if (!isInitialized ||
                !isCurrentPart ||
                isResolving ||
                gameFlow.CurrentState != GameState.PlayingPart)
            {
                return;
            }

            isResolving = true;
            playerMovement.GetComponent<PlayerSpinnerPowerUp>()?.Deactivate();
            gameFlow.StopForDrop();
            ballReleaseController.ReleaseCollectedItems(
                dropboxBallCounter);
            resolutionRoutine = StartCoroutine(ResolvePartRoutine());
        }

        private IEnumerator ResolvePartRoutine()
        {
            if (config.DropSettleDuration > 0f)
            {
                yield return new WaitForSeconds(config.DropSettleDuration);
            }

            gameFlow.BeginPartResolution();

            int depositedCount =
                dropboxBallCounter.Count;

            if (depositedCount > 0)
            {
                CollectiblesDeposited?.Invoke(
                    depositedCount);
            }

            if (dropboxBallCounter.Count < RequiredBallCount)
            {
                Debug.Log(
                    $"Level part '{name}' failed: {dropboxBallCounter.Count}/{RequiredBallCount} collectibles counted.",
                    this);
                PartFailed?.Invoke(this);
                gameFlow.FailLevel();
                resolutionRoutine = null;
                yield break;
            }

            Debug.Log(
                $"Level part '{name}' succeeded: {dropboxBallCounter.Count}/{RequiredBallCount} collectibles counted.",
                this);
            PartSucceeded?.Invoke(this);

            gameFlow.BeginTransition();
            dropboxBallCounter.ConsumeAllCountedItems();
            collectibleGenerator.ClearGeneratedObjects();
            requirementDisplay?.SetVisible(false);

            yield return dropboxMovement.RaiseRoutine(
                config.DropboxRaiseDuration,
                config.TransitionCurve);

            yield return bridgeShape.ExtendRoutine(
                config.BridgeExtendDuration,
                config.TransitionCurve);

            bridgeColorController.ApplyColor(config.BridgeColor);

            if (nextGate != null)
            {
                yield return nextGate.OpenRoutine(
                    config.GateOpenDuration,
                    config.TransitionCurve);
            }

            TransitionCompleted?.Invoke(this);
            resolutionRoutine = null;
        }

        public void Initialize(
            GameFlowController sharedGameFlow,
            CollectibleReleaseController sharedBallReleaseController,
            LevelRestartService sharedRestartService,
            GateController followingGate)
        {
            if (isInitialized)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartController)} on '{name}' was initialized more than once.",
                    this);
                return;
            }

            gameFlow = sharedGameFlow;
            ballReleaseController = sharedBallReleaseController;
            restartService = sharedRestartService;
            nextGate = followingGate;
            isInitialized = true;

            if (requirementDisplay != null)
            {
                requirementDisplay.Configure(RequiredBallCount);
            }
        }

        public void SetCurrentPart(bool isCurrent)
        {
            isCurrentPart = isCurrent;
        }

        public bool ConfigureCollectibleGeneration(
            DifficultyConfig difficultyConfig,
            int partIndex,
            int partSeed)
        {
            if (difficultyConfig == null ||
                !difficultyConfig.TryGetPartCounts(
                    partIndex,
                    out int requiredCount,
                    out int generatedCount))
            {
                Debug.LogError(
                    $"Could not read difficulty counts for part index {partIndex} on '{name}'.",
                    this);
                return false;
            }

            if (collectibleGenerator == null)
            {
                Debug.LogError(
                    $"{nameof(LevelPartController)} on '{name}' requires a {nameof(LevelPartCollectibleGenerator)} for runtime generation.",
                    this);
                return false;
            }

            runtimeRequiredCollectibleCount = requiredCount;
            return collectibleGenerator.Generate(
                requiredCount,
                generatedCount,
                partSeed);
        }

        public bool SpawnSpinnerPickup(SpinnerPowerUpPickup pickupPrefab)
        {
            if (pickupPrefab == null || spinnerPickupSpawnPoint == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartController)} on '{name}' cannot spawn a spinner pickup because its prefab or spawn point is missing.",
                    this);
                return false;
            }

            SpinnerPowerUpPickup pickup = Instantiate(
                pickupPrefab,
                spinnerPickupSpawnPoint.position,
                spinnerPickupSpawnPoint.rotation,
                transform);
            pickup.name = pickupPrefab.name;
            return true;
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            isValid &= ValidateReference(config, nameof(config));
            isValid &= ValidateReference(dropAreaTrigger, nameof(dropAreaTrigger));
            isValid &= ValidateReference(dropboxBallCounter, nameof(dropboxBallCounter));
            isValid &= ValidateReference(dropboxMovement, nameof(dropboxMovement));
            isValid &= ValidateReference(bridgeShape, nameof(bridgeShape));
            isValid &= ValidateReference(bridgeColorController, nameof(bridgeColorController));
            isValid &= ValidateReference(entranceGate, nameof(entranceGate));
            isValid &= ValidateReference(leftMovementLimit, nameof(leftMovementLimit));
            isValid &= ValidateReference(rightMovementLimit, nameof(rightMovementLimit));
            isValid &= ValidateReference(
                movementRestrictionAnchor,
                nameof(movementRestrictionAnchor));
            isValid &= ValidateReference(gameFlow, nameof(gameFlow));
            isValid &= ValidateReference(ballReleaseController, nameof(ballReleaseController));
            isValid &= ValidateReference(restartService, nameof(restartService));

            return isValid;
        }

        private bool ValidateReference(
            UnityEngine.Object reference,
            string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(LevelPartController)} on '{name}' requires '{fieldName}'.",
                this);
            return false;
        }

        private void ResolveMovementRestrictionAnchor()
        {
            if (movementRestrictionAnchor != null)
            {
                return;
            }

            Transform[] childTransforms =
                GetComponentsInChildren<Transform>(true);

            foreach (Transform childTransform in childTransforms)
            {
                if (childTransform.name == "MovementRestirictionAnchor" ||
                    childTransform.name == "MovementRestrictionAnchor")
                {
                    movementRestrictionAnchor = childTransform;
                    return;
                }
            }
        }

        private void ResolveMissionRouteAnchor()
        {
            if (missionRouteAnchor != null)
            {
                return;
            }

            Transform[] childTransforms =
                GetComponentsInChildren<Transform>(true);

            foreach (Transform childTransform in childTransforms)
            {
                if (childTransform.name ==
                    "MissionRouteAnchor")
                {
                    missionRouteAnchor = childTransform;
                    return;
                }
            }
        }

        private void OnValidate()
        {
            if (requirementDisplay == null)
            {
                requirementDisplay = GetComponentInChildren<
                    DropboxRequirementDisplay>(true);
            }

            if (config == null ||
                dropAreaTrigger == null ||
                dropboxBallCounter == null ||
                dropboxMovement == null ||
                bridgeShape == null ||
                bridgeColorController == null ||
                entranceGate == null ||
                collectibleGenerator == null ||
                leftMovementLimit == null ||
                rightMovementLimit == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartController)} on '{name}' has missing local prefab references.",
                    this);
            }

            if (requirementDisplay == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartController)} on '{name}' has no Dropbox requirement display.",
                    this);
            }

            if (spinnerPickupSpawnPoint == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelPartController)} on '{name}' has no spinner pickup spawn point.",
                    this);
            }
        }
    }
}
