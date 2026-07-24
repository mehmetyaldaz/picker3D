using System;
using System.Collections;
using UnityEngine;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerTapInput))]
    public sealed class PlayerRampMovement : MonoBehaviour
    {
        private enum MovementPhase
        {
            Inactive,
            ClimbingRamp,
            PhysicsFlight
        }

        [SerializeField] private PlayerTapInput tapInput;

        private Rigidbody body;
        private Vector3 pathStart;
        private Vector3 pathEnd;
        private float pathLength;
        private float currentDistance;
        private float rampSpeed;
        private float initialRampSpeed;
        private float speedAddedPerTap;
        private float maximumRampSpeed;
        private float rampSurfaceOffset;
        private MovementPhase phase;
        private RigidbodyConstraints originalConstraints;
        private bool constraintsWereCaptured;
        private bool originalUseGravity;
        private bool originalIsKinematic;
        private bool originalDetectCollisions;
        private CollisionDetectionMode originalCollisionDetectionMode;
        private float physicsFlightStartTime;
        private Coroutine collisionRestoreRoutine;
        private readonly RaycastHit[] surfaceHits = new RaycastHit[16];

        public event Action<float> RampEndReached;
        public event Action<Vector3> PhysicsLanded;

        public bool IsActive => phase != MovementPhase.Inactive;
        public bool IsInPhysicsFlight =>
            phase == MovementPhase.PhysicsFlight;

        private void Reset()
        {
            tapInput = GetComponent<PlayerTapInput>();
        }

        private void Awake()
        {
            if (!EnsureDependencies())
            {
                Debug.LogError(
                    $"{nameof(PlayerRampMovement)} on '{name}' requires a Rigidbody and {nameof(PlayerTapInput)}.",
                    this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            int tapCount = tapInput.ConsumeTapCount();

            if (phase == MovementPhase.Inactive)
            {
                return;
            }

            if (phase == MovementPhase.ClimbingRamp)
            {
                UpdateRampMovement(tapCount);
                return;
            }

            tapInput.Clear();
        }

        private void UpdateRampMovement(int tapCount)
        {
            if (tapCount > 0)
            {
                rampSpeed = Mathf.Min(
                    maximumRampSpeed,
                    rampSpeed + tapCount * speedAddedPerTap);
            }

            currentDistance = Mathf.Min(
                pathLength,
                currentDistance + rampSpeed * Time.fixedDeltaTime);

            float progress = pathLength > Mathf.Epsilon
                ? currentDistance / pathLength
                : 1f;
            Vector3 nextPosition = Vector3.Lerp(
                pathStart,
                pathEnd,
                progress);

            if (TryGetSurfaceHeight(nextPosition, out float surfaceHeight))
            {
                nextPosition.y = surfaceHeight + rampSurfaceOffset;
            }

            body.MovePosition(nextPosition);

            if (currentDistance < pathLength)
            {
                return;
            }

            phase = MovementPhase.Inactive;
            float speedProgress = Mathf.InverseLerp(
                initialRampSpeed,
                maximumRampSpeed,
                rampSpeed);
            RampEndReached?.Invoke(speedProgress);
        }

        public void BeginRamp(
            Vector3 launchPosition,
            float startingSpeed,
            float tapSpeedIncrease,
            float maximumSpeed,
            float rampZAngle,
            float verticalOffset)
        {
            if (!constraintsWereCaptured)
            {
                originalConstraints = body.constraints;
                originalUseGravity = body.useGravity;
                originalIsKinematic = body.isKinematic;
                originalDetectCollisions = body.detectCollisions;
                originalCollisionDetectionMode = body.collisionDetectionMode;
                constraintsWereCaptured = true;
            }

            body.constraints &= ~RigidbodyConstraints.FreezePositionY;
            Vector3 rampEulerAngles = body.rotation.eulerAngles;
            rampEulerAngles.z = rampZAngle;
            body.rotation = Quaternion.Euler(rampEulerAngles);
            transform.rotation = body.rotation;
            Physics.SyncTransforms();

            rampSurfaceOffset =
                CalculatePhysicalBottomOffset() +
                Mathf.Max(0f, verticalOffset);
            pathStart = body.position;

            if (TryGetSurfaceHeight(
                    pathStart,
                    out float startSurfaceHeight))
            {
                pathStart.y = startSurfaceHeight + rampSurfaceOffset;
            }

            body.position = pathStart;
            transform.position = pathStart;
            pathEnd = launchPosition;
            pathLength = Vector3.Distance(pathStart, pathEnd);
            currentDistance = 0f;
            initialRampSpeed = Mathf.Max(0.01f, startingSpeed);
            rampSpeed = initialRampSpeed;
            speedAddedPerTap = Mathf.Max(0.01f, tapSpeedIncrease);
            maximumRampSpeed = Mathf.Max(initialRampSpeed, maximumSpeed);
            tapInput.Clear();
            phase = MovementPhase.ClimbingRamp;

            if (pathLength > Mathf.Epsilon)
            {
                return;
            }

            phase = MovementPhase.Inactive;
            RampEndReached?.Invoke(0f);
        }

        private float CalculatePhysicalBottomOffset()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            float lowestPoint = float.PositiveInfinity;

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider playerCollider = colliders[index];

                if (playerCollider == null ||
                    !playerCollider.enabled ||
                    playerCollider.isTrigger)
                {
                    continue;
                }

                lowestPoint = Mathf.Min(
                    lowestPoint,
                    playerCollider.bounds.min.y);
            }

            return float.IsPositiveInfinity(lowestPoint)
                ? 0f
                : Mathf.Max(0f, body.position.y - lowestPoint);
        }

        private bool TryGetSurfaceHeight(
            Vector3 position,
            out float surfaceHeight)
        {
            float rayStartHeight =
                Mathf.Max(pathStart.y, pathEnd.y) + 10f;
            Vector3 rayOrigin = new(
                position.x,
                rayStartHeight,
                position.z);
            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                surfaceHits,
                50f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            surfaceHeight = float.NegativeInfinity;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = surfaceHits[index];

                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.normal.y < 0.25f)
                {
                    continue;
                }

                surfaceHeight = Mathf.Max(
                    surfaceHeight,
                    hit.point.y);
            }

            return !float.IsNegativeInfinity(surfaceHeight);
        }

        public void BeginPhysicsFlight(
            Vector3 minimumLandingPosition,
            Vector3 maximumLandingPosition,
            float speedProgress,
            float minimumSpeed,
            float maximumSpeed,
            float upwardSpeed,
            float angularSpeed)
        {
            Vector3 launchDirection =
                maximumLandingPosition - body.position;
            launchDirection.y = 0f;

            if (launchDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                launchDirection = transform.forward;
                launchDirection.y = 0f;
            }

            launchDirection.Normalize();
            float safeUpwardSpeed = Mathf.Max(0f, upwardSpeed);
            float requiredMinimumSpeed =
                CalculateRequiredForwardSpeed(
                    minimumLandingPosition,
                    safeUpwardSpeed);
            float requiredMaximumSpeed =
                CalculateRequiredForwardSpeed(
                    maximumLandingPosition,
                    safeUpwardSpeed);
            float effectiveMinimumSpeed = requiredMinimumSpeed > 0.01f
                ? requiredMinimumSpeed
                : Mathf.Max(0.1f, minimumSpeed);
            float effectiveMaximumSpeed = Mathf.Max(
                effectiveMinimumSpeed,
                maximumSpeed,
                requiredMaximumSpeed);
            float forwardSpeed = Mathf.Lerp(
                effectiveMinimumSpeed,
                effectiveMaximumSpeed,
                Mathf.Clamp01(speedProgress));

            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.detectCollisions = false;
            body.linearVelocity =
                launchDirection * forwardSpeed +
                Vector3.up * safeUpwardSpeed;
            body.angularVelocity =
                new Vector3(1f, 0.65f, -0.8f).normalized *
                Mathf.Max(0f, angularSpeed);
            tapInput.Clear();
            phase = MovementPhase.PhysicsFlight;
            physicsFlightStartTime = Time.time;
            float collisionSuppressionDuration = Mathf.Clamp(
                1.25f / Mathf.Max(0.1f, forwardSpeed),
                0.15f,
                1.25f);
            collisionRestoreRoutine = StartCoroutine(
                RestoreFlightCollisionsRoutine(
                    collisionSuppressionDuration));
        }

        private float CalculateRequiredForwardSpeed(
            Vector3 landingPosition,
            float upwardSpeed)
        {
            float gravity = Physics.gravity.y;
            float targetBodyHeight =
                landingPosition.y + rampSurfaceOffset;
            float heightDifference =
                body.position.y - targetBodyHeight;
            float flightTime;

            if (gravity < -Mathf.Epsilon)
            {
                float discriminant =
                    upwardSpeed * upwardSpeed -
                    2f * gravity * heightDifference;

                if (discriminant >= 0f)
                {
                    flightTime =
                        (-upwardSpeed - Mathf.Sqrt(discriminant)) /
                        gravity;
                }
                else
                {
                    flightTime = 0f;
                }
            }
            else
            {
                flightTime = 0f;
            }

            if (flightTime <= 0.01f)
            {
                flightTime = Mathf.Max(
                    0.25f,
                    upwardSpeed / Mathf.Max(0.01f, -gravity) * 2f);
            }

            Vector3 planarOffset =
                landingPosition - body.position;
            planarOffset.y = 0f;
            float forwardDistance = planarOffset.magnitude;
            return forwardDistance / flightTime;
        }

        private IEnumerator RestoreFlightCollisionsRoutine(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, delay));

            if (body != null)
            {
                body.detectCollisions = true;
            }

            collisionRestoreRoutine = null;
        }

        public void StabilizeLanding()
        {
            if (phase != MovementPhase.PhysicsFlight)
            {
                return;
            }

            phase = MovementPhase.Inactive;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.detectCollisions = true;
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (phase != MovementPhase.PhysicsFlight ||
                Time.time - physicsFlightStartTime < 0.15f ||
                collision.contactCount == 0)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);

            if (contact.normal.y < 0.35f)
            {
                return;
            }

            Vector3 landingPosition = body.position;
            StabilizeLanding();
            PhysicsLanded?.Invoke(landingPosition);
        }

        public void Stop()
        {
            phase = MovementPhase.Inactive;
            tapInput?.Clear();

            if (collisionRestoreRoutine != null)
            {
                StopCoroutine(collisionRestoreRoutine);
                collisionRestoreRoutine = null;
            }

            if (constraintsWereCaptured && body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = originalIsKinematic;
                body.useGravity = originalUseGravity;
                body.detectCollisions = originalDetectCollisions;
                body.constraints = originalConstraints;
                body.collisionDetectionMode = originalCollisionDetectionMode;
                constraintsWereCaptured = false;
            }
        }

        public void ResetForLevelStart(
            Vector3 spawnPosition,
            Quaternion spawnRotation)
        {
            if (!EnsureDependencies())
            {
                Debug.LogError(
                    $"{nameof(PlayerRampMovement)} on '{name}' could not reset because its dependencies are missing.",
                    this);
                return;
            }

            Stop();
            body.isKinematic = true;
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            body.useGravity = false;
        }

        public void ResetForLevelContinuation(Quaternion forwardRotation)
        {
            if (!EnsureDependencies())
            {
                Debug.LogError(
                    $"{nameof(PlayerRampMovement)} on '{name}' could not continue because its dependencies are missing.",
                    this);
                return;
            }

            Vector3 continuationPosition = body.position;
            Stop();
            body.isKinematic = true;
            body.useGravity = false;
            body.position = continuationPosition;
            body.rotation = forwardRotation;
            transform.SetPositionAndRotation(
                continuationPosition,
                forwardRotation);
        }

        public IEnumerator FlyToLevelStartRoutine(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration,
            float arcHeight,
            AnimationCurve transitionCurve)
        {
            if (!EnsureDependencies())
            {
                yield break;
            }

            Stop();
            body.isKinematic = true;
            body.useGravity = false;

            Vector3 startPosition = body.position;
            Quaternion startRotation = body.rotation;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(
                    elapsed / safeDuration);
                float progress = transitionCurve != null
                    ? transitionCurve.Evaluate(normalizedTime)
                    : normalizedTime;
                Vector3 position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress);
                position.y += Mathf.Sin(normalizedTime * Mathf.PI) *
                              Mathf.Max(0f, arcHeight);
                Quaternion rotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    progress);

                body.position = position;
                body.rotation = rotation;
                transform.SetPositionAndRotation(position, rotation);
                yield return null;
            }

            body.position = targetPosition;
            body.rotation = targetRotation;
            transform.SetPositionAndRotation(
                targetPosition,
                targetRotation);
        }

        private bool EnsureDependencies()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (tapInput == null)
            {
                tapInput = GetComponent<PlayerTapInput>();
            }

            return body != null && tapInput != null;
        }

        private void OnValidate()
        {
            if (tapInput == null)
            {
                tapInput = GetComponent<PlayerTapInput>();
            }
        }
    }
}
