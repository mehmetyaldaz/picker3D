using System;
using System.Collections;
using Picker3D.Collectibles;
using Picker3D.Core;
using Picker3D.Data;
using Picker3D.Player;
using Picker3D.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        [Header("Local Volume Chances")]
        [SerializeField, Range(0f, 1f)]
        private float volumeEffectChance = 0.5f;
        [SerializeField, Min(0f)] private float redStyleWeight = 1f;
        [SerializeField, Min(0f)] private float greenStyleWeight = 1f;
        [SerializeField, Min(0f)]
        private float blackAndWhiteStyleWeight = 1f;

        [Header("Player Movement Limits")]
        [SerializeField] private Transform leftMovementLimit;
        [SerializeField] private Transform rightMovementLimit;
        [SerializeField] private Transform movementRestrictionAnchor;

        private Coroutine resolutionRoutine;
        private GameFlowController gameFlow;
        private CollectibleReleaseController ballReleaseController;
        private GateController nextGate;
        private Volume localVolume;
        private bool isInitialized;
        private bool isCurrentPart;
        private bool isResolving;
        private int runtimeRequiredCollectibleCount = -1;

        private static readonly Vector3 LuminanceMixer =
            new(21.26f, 71.52f, 7.22f);

        public event Action<LevelPartController> TransitionCompleted;
        public event Action<int> CollectiblesDeposited;

        public int RequiredBallCount =>
            runtimeRequiredCollectibleCount >= 0
                ? runtimeRequiredCollectibleCount
                : config != null
                    ? config.RequiredBallCount
                    : 0;

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

        private void OnDestroy()
        {
            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleGameStateChanged;
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
                gameFlow.FailLevel();
                resolutionRoutine = null;
                yield break;
            }

            Debug.Log(
                $"Level part '{name}' succeeded: {dropboxBallCounter.Count}/{RequiredBallCount} collectibles counted.",
                this);
            gameFlow.BeginTransition();
            collectibleGenerator.PlayClearParticles();
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
            nextGate = followingGate;
            isInitialized = true;
            gameFlow.StateChanged += HandleGameStateChanged;

            if (requirementDisplay != null)
            {
                requirementDisplay.Configure(RequiredBallCount);
            }
        }

        public void SetCurrentPart(bool isCurrent)
        {
            isCurrentPart = isCurrent;
            RefreshFlyingCollectibleState();
        }

        private void HandleGameStateChanged(
            GameState previousState,
            GameState nextState)
        {
            RefreshFlyingCollectibleState();
        }

        private void RefreshFlyingCollectibleState()
        {
            collectibleGenerator?.SetPartActive(
                isCurrentPart &&
                gameFlow != null &&
                gameFlow.CurrentState == GameState.PlayingPart);
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
            bool generated = collectibleGenerator.Generate(
                requiredCount,
                generatedCount,
                partSeed);

            if (generated)
            {
                ApplyRandomVolumeStyle(partSeed);
            }

            return generated;
        }

        private void ApplyRandomVolumeStyle(int partSeed)
        {
            if (localVolume == null)
            {
                localVolume =
                    GetComponentInChildren<Volume>(true);
            }

            if (localVolume == null ||
                !localVolume.profile.TryGet(
                    out ChannelMixer channelMixer))
            {
                return;
            }

            System.Random random =
                new(partSeed ^ 7919);

            if (random.NextDouble() >= volumeEffectChance)
            {
                channelMixer.active = false;
                return;
            }

            float totalStyleWeight =
                redStyleWeight +
                greenStyleWeight +
                blackAndWhiteStyleWeight;

            if (totalStyleWeight <= 0f)
            {
                channelMixer.active = false;
                return;
            }

            double styleRoll =
                random.NextDouble() * totalStyleWeight;
            Vector3 zero = Vector3.zero;

            if (styleRoll < redStyleWeight)
            {
                ApplyChannelMixer(
                    channelMixer,
                    LuminanceMixer,
                    zero,
                    zero);
                return;
            }

            if (styleRoll <
                redStyleWeight + greenStyleWeight)
            {
                ApplyChannelMixer(
                    channelMixer,
                    zero,
                    LuminanceMixer,
                    zero);
                return;
            }

            ApplyChannelMixer(
                channelMixer,
                LuminanceMixer,
                LuminanceMixer,
                LuminanceMixer);
        }

        private static void ApplyChannelMixer(
            ChannelMixer channelMixer,
            Vector3 redOutput,
            Vector3 greenOutput,
            Vector3 blueOutput)
        {
            channelMixer.active = true;
            channelMixer.redOutRedIn.Override(redOutput.x);
            channelMixer.redOutGreenIn.Override(redOutput.y);
            channelMixer.redOutBlueIn.Override(redOutput.z);
            channelMixer.greenOutRedIn.Override(greenOutput.x);
            channelMixer.greenOutGreenIn.Override(greenOutput.y);
            channelMixer.greenOutBlueIn.Override(greenOutput.z);
            channelMixer.blueOutRedIn.Override(blueOutput.x);
            channelMixer.blueOutGreenIn.Override(blueOutput.y);
            channelMixer.blueOutBlueIn.Override(blueOutput.z);
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
