using System;
using System.Collections;
using Picker3D.Core;
using Picker3D.Data;
using Picker3D.Player;
using Picker3D.World;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class FinalRampController : MonoBehaviour
    {
        [SerializeField] private FinalRampConfig config;
        [SerializeField] private GateController entranceGate;
        [SerializeField] private FinalRampEntryTrigger entryTrigger;
        [SerializeField] private Transform rampLaunchPoint;
        [SerializeField] private Transform maximumLandingPoint;
        [SerializeField] private RewardZone[] rewardZones;

        private GameFlowController gameFlow;
        private PlayerRampMovement activeMovement;
        private Coroutine rewardSettleRoutine;
        private Coroutine flightTimeoutRoutine;
        private int highestGemReward;
        private bool isInitialized;
        private bool isArmed;
        private bool isRunning;

        public event Action RampStarted;
        public event Action<string, int> RewardZoneReached;
        public event Action<int> RampCompleted;

        public void Initialize(GameFlowController sharedGameFlow)
        {
            if (isInitialized)
            {
                return;
            }

            if (sharedGameFlow == null || !ValidateReferences())
            {
                Debug.LogError(
                    $"{nameof(FinalRampController)} on '{name}' could not initialize.",
                    this);
                enabled = false;
                return;
            }

            gameFlow = sharedGameFlow;
            isInitialized = true;
        }

        public void Arm()
        {
            if (isInitialized)
            {
                isArmed = true;
            }
        }

        public IEnumerator OpenEntranceRoutine()
        {
            if (!isInitialized)
            {
                yield break;
            }

            yield return entranceGate.OpenRoutine(
                config.GateOpenDuration,
                config.TransitionCurve);
        }

        private void OnEnable()
        {
            if (entryTrigger != null)
            {
                entryTrigger.PlayerEntered += HandlePlayerEntered;
            }

            if (rewardZones == null)
            {
                return;
            }

            foreach (RewardZone zone in rewardZones)
            {
                if (zone != null)
                {
                    zone.Reached += HandleRewardZoneReached;
                }
            }
        }

        private void OnDisable()
        {
            if (entryTrigger != null)
            {
                entryTrigger.PlayerEntered -= HandlePlayerEntered;
            }

            if (rewardZones != null)
            {
                foreach (RewardZone zone in rewardZones)
                {
                    if (zone != null)
                    {
                        zone.Reached -= HandleRewardZoneReached;
                    }
                }
            }

            StopRamp();
        }

        private void HandlePlayerEntered(PlayerRampMovement movement)
        {
            if (!isInitialized ||
                !isArmed ||
                isRunning ||
                gameFlow.CurrentState != GameState.PlayingPart)
            {
                return;
            }

            isArmed = false;
            isRunning = true;
            highestGemReward = 0;
            activeMovement = movement;
            activeMovement.RampEndReached += HandleRampEndReached;
            activeMovement.PhysicsLanded += HandlePhysicsLanded;

            gameFlow.EnterFinalRamp();
            activeMovement.BeginRamp(
                rampLaunchPoint.position,
                config.InitialRampSpeed,
                config.SpeedAddedPerTap,
                config.MaximumRampSpeed,
                config.PlayerRampZAngle,
                config.PlayerRampVerticalOffset);
            RampStarted?.Invoke();
        }

        private void HandleRewardZoneReached(RewardZone zone)
        {
            if (!isRunning ||
                activeMovement == null ||
                !activeMovement.IsInPhysicsFlight)
            {
                return;
            }

            ApplyRewardZone(zone);
        }

        private void ApplyRewardZone(RewardZone zone)
        {
            if (!isRunning ||
                zone == null ||
                zone.GemReward < highestGemReward)
            {
                return;
            }

            highestGemReward = zone.GemReward;
            RewardZoneReached?.Invoke(
                zone.ZoneId,
                highestGemReward);

            if (rewardSettleRoutine != null)
            {
                StopCoroutine(rewardSettleRoutine);
            }

            rewardSettleRoutine = StartCoroutine(RewardSettleRoutine());
        }

        private void HandlePhysicsLanded(Vector3 landingPosition)
        {
            RewardZone closestZone = null;
            float closestSqrDistance = float.PositiveInfinity;

            foreach (RewardZone zone in rewardZones)
            {
                Vector3 offset = zone.transform.position - landingPosition;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestZone = zone;
            }

            if (closestZone != null)
            {
                ApplyRewardZone(closestZone);
            }
        }

        private void HandleRampEndReached(float speedProgress)
        {
            activeMovement.BeginPhysicsFlight(
                GetMinimumLandingPosition(),
                maximumLandingPoint.position,
                speedProgress,
                config.MinimumLaunchSpeed,
                config.MaximumLaunchSpeed,
                config.UpwardLaunchSpeed,
                config.SpinSpeed);

            flightTimeoutRoutine = StartCoroutine(FlightTimeoutRoutine());
        }

        private Vector3 GetMinimumLandingPosition()
        {
            RewardZone minimumZone = rewardZones[0];

            for (int index = 1; index < rewardZones.Length; index++)
            {
                if (rewardZones[index].GemReward <
                    minimumZone.GemReward)
                {
                    minimumZone = rewardZones[index];
                }
            }

            return minimumZone.transform.position;
        }

        private IEnumerator RewardSettleRoutine()
        {
            yield return new WaitForSeconds(config.RewardSettleDuration);
            rewardSettleRoutine = null;
            CompleteRamp();
        }

        private IEnumerator FlightTimeoutRoutine()
        {
            yield return new WaitForSeconds(config.MaximumFlightDuration);
            flightTimeoutRoutine = null;
            CompleteRamp();
        }

        private void CompleteRamp()
        {
            if (!isRunning)
            {
                return;
            }

            int gemReward = highestGemReward;
            StopRamp();
            gameFlow.CompleteLevel();
            Debug.Log(
                $"Final ramp completed with {gemReward} gems.",
                this);
            RampCompleted?.Invoke(gemReward);
        }

        private void StopRamp()
        {
            isRunning = false;

            if (activeMovement != null)
            {
                activeMovement.RampEndReached -= HandleRampEndReached;
                activeMovement.PhysicsLanded -= HandlePhysicsLanded;
                activeMovement.Stop();
                activeMovement = null;
            }

            if (rewardSettleRoutine != null)
            {
                StopCoroutine(rewardSettleRoutine);
                rewardSettleRoutine = null;
            }

            if (flightTimeoutRoutine != null)
            {
                StopCoroutine(flightTimeoutRoutine);
                flightTimeoutRoutine = null;
            }
        }

        private bool ValidateReferences()
        {
            bool isValid = config != null &&
                           entranceGate != null &&
                           entryTrigger != null &&
                           rampLaunchPoint != null &&
                           maximumLandingPoint != null &&
                           rewardZones != null &&
                           rewardZones.Length > 0;

            if (!isValid)
            {
                Debug.LogError(
                    $"{nameof(FinalRampController)} on '{name}' has missing local references.",
                    this);
                return false;
            }

            for (int index = 0; index < rewardZones.Length; index++)
            {
                if (rewardZones[index] == null)
                {
                    Debug.LogError(
                        $"{nameof(FinalRampController)} on '{name}' has an empty reward zone at index {index}.",
                        this);
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            if (config == null ||
                entranceGate == null ||
                entryTrigger == null ||
                rampLaunchPoint == null ||
                maximumLandingPoint == null)
            {
                Debug.LogWarning(
                    $"{nameof(FinalRampController)} on '{name}' has missing Inspector references.",
                    this);
            }
        }
    }
}
