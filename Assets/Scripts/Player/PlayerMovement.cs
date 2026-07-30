using UnityEngine;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerDragInput))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float MaximumNormalizedDragPerStep = 0.04f;
        private const float SafeMaximumHorizontalSpeed = 12f;
        private const float RestrictedNormalizedDragPerStep = 0.012f;
        private const float RestrictedMaximumHorizontalSpeed = 4.5f;
        private const float RestrictedMaximumTargetDistance = 0.3f;

        [Header("Dependencies")]
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerDragInput dragInput;

        [Header("Horizontal Limits")]
        [SerializeField] private Transform leftMovementLimit;
        [SerializeField] private Transform rightMovementLimit;

        private Rigidbody body;
        private float targetHorizontalPosition;
        private bool forwardMovementEnabled;
        private bool horizontalMovementEnabled;
        private Transform movementRestrictionAnchor;
        private bool movementRestrictionActive;
        private bool missingLimitsWarningShown;
        private float nextBlockedDragLogTime;
        private float nextAppliedDragLogTime;

        public bool HasMovementLimits =>
            leftMovementLimit != null && rightMovementLimit != null;

        private void Reset()
        {
            dragInput = GetComponent<PlayerDragInput>();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();

            if (dragInput == null)
            {
                dragInput = GetComponent<PlayerDragInput>();
            }

            if (dragInput != null)
            {
                dragInput.enabled = true;
                dragInput.Clear();
            }

            targetHorizontalPosition = body.position.x;

            if (!ValidateRuntimeConfiguration())
            {
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            float normalizedDrag = dragInput.ConsumeHorizontalDrag();
            float rawNormalizedDrag = normalizedDrag;

            if (!Mathf.Approximately(
                    normalizedDrag,
                    0f) &&
                (!horizontalMovementEnabled ||
                 !HasMovementLimits) &&
                Time.unscaledTime >=
                nextBlockedDragLogTime)
            {
                nextBlockedDragLogTime =
                    Time.unscaledTime + 1f;
                Debug.LogWarning(
                    $"[MovementDebug][Movement] Drag received but horizontal movement cannot apply | drag={normalizedDrag:F4} {GetDebugState()}",
                    this);
            }

            if (!forwardMovementEnabled &&
                !horizontalMovementEnabled)
            {
                targetHorizontalPosition = body.position.x;
                return;
            }

            Vector3 nextPosition = body.position;

            if (horizontalMovementEnabled && HasMovementLimits)
            {
                missingLimitsWarningShown = false;
                UpdateMovementRestriction();

                float maximumNormalizedDrag =
                    movementRestrictionActive
                        ? RestrictedNormalizedDragPerStep
                        : MaximumNormalizedDragPerStep;
                normalizedDrag = Mathf.Clamp(
                    normalizedDrag,
                    -maximumNormalizedDrag,
                    maximumNormalizedDrag);
                targetHorizontalPosition +=
                    normalizedDrag *
                    config.HorizontalSensitivity;

                if (movementRestrictionActive)
                {
                    targetHorizontalPosition = Mathf.Clamp(
                        targetHorizontalPosition,
                        body.position.x -
                        RestrictedMaximumTargetDistance,
                        body.position.x +
                        RestrictedMaximumTargetDistance);
                }

                targetHorizontalPosition =
                    ClampToMovementLimits(
                        targetHorizontalPosition);

                float maximumHorizontalSpeed =
                    movementRestrictionActive
                        ? RestrictedMaximumHorizontalSpeed
                        : SafeMaximumHorizontalSpeed;
                nextPosition.x = Mathf.MoveTowards(
                    body.position.x,
                    targetHorizontalPosition,
                    Mathf.Min(
                        config.MaximumHorizontalSpeed,
                        maximumHorizontalSpeed) *
                    Time.fixedDeltaTime);

                if (!Mathf.Approximately(
                        rawNormalizedDrag,
                        0f) &&
                    Time.unscaledTime >=
                    nextAppliedDragLogTime)
                {
                    nextAppliedDragLogTime =
                        Time.unscaledTime + 0.5f;
                    Debug.Log(
                        $"[MovementDebug][Movement] Drag applied | rawDrag={rawNormalizedDrag:F4} clampedDrag={normalizedDrag:F4} bodyX={body.position.x:F3} targetX={targetHorizontalPosition:F3} nextX={nextPosition.x:F3} restrictionActive={movementRestrictionActive} horizontalEnabled={horizontalMovementEnabled}",
                        this);
                }
            }
            else
            {
                if (horizontalMovementEnabled &&
                    !HasMovementLimits &&
                    !missingLimitsWarningShown)
                {
                    missingLimitsWarningShown = true;
                    Debug.LogError(
                        $"[MovementDebug][Movement] Horizontal movement is enabled but movement limits are missing | {GetDebugState()}",
                        this);
                }

                targetHorizontalPosition = body.position.x;
            }

            if (forwardMovementEnabled)
            {
                nextPosition.z +=
                    config.ForwardSpeed *
                    Time.fixedDeltaTime;
            }

            body.MovePosition(nextPosition);
        }

        public void SetMovementEnabled(
            bool enableForwardMovement,
            bool enableHorizontalMovement)
        {
            bool forwardStateChanged =
                forwardMovementEnabled !=
                enableForwardMovement;
            bool horizontalStateChanged =
                horizontalMovementEnabled !=
                enableHorizontalMovement;
            forwardMovementEnabled = enableForwardMovement;
            horizontalMovementEnabled =
                enableHorizontalMovement;

            if (horizontalStateChanged ||
                forwardStateChanged)
            {
                Debug.Log(
                    $"[MovementDebug][Movement] SetMovementEnabled | requestedForward={enableForwardMovement} requestedHorizontal={enableHorizontalMovement} forwardChanged={forwardStateChanged} horizontalChanged={horizontalStateChanged} {GetDebugState()}",
                    this);
            }

            if (horizontalStateChanged &&
                dragInput != null)
            {
                dragInput.Clear();
            }

            if (forwardMovementEnabled ||
                horizontalMovementEnabled)
            {
                RestoreGroundMovementBodyState();

                if (body != null &&
                    horizontalStateChanged)
                {
                    targetHorizontalPosition =
                        HasMovementLimits
                            ? ClampToMovementLimits(body.position.x)
                            : body.position.x;
                }

                return;
            }

            if (body != null)
            {
                targetHorizontalPosition = body.position.x;
            }
        }

        public void PrepareForLevel(
            Transform leftLimit,
            Transform rightLimit,
            Transform restrictionAnchor)
        {
            SetMovementLimits(leftLimit, rightLimit);
            movementRestrictionAnchor = restrictionAnchor;
            movementRestrictionActive = false;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            RestoreGroundMovementBodyState();

            if (dragInput != null)
            {
                dragInput.enabled = true;
            }

            targetHorizontalPosition =
                horizontalMovementEnabled
                    ? ClampToMovementLimits(
                        targetHorizontalPosition)
                    : ClampToMovementLimits(
                        body.position.x);

            Debug.Log(
                $"[MovementDebug][Movement] PrepareForLevel | left={DescribeTransform(leftLimit)} right={DescribeTransform(rightLimit)} restrictionAnchor={DescribeTransform(restrictionAnchor)} {GetDebugState()}",
                this);
        }

        private void UpdateMovementRestriction()
        {
            if (movementRestrictionActive ||
                movementRestrictionAnchor == null ||
                body.position.z <
                movementRestrictionAnchor.position.z)
            {
                return;
            }

            movementRestrictionActive = true;
            targetHorizontalPosition = body.position.x;
            dragInput.Clear();
            Debug.LogWarning(
                $"[MovementDebug][Movement] Restriction activated | anchor={DescribeTransform(movementRestrictionAnchor)} {GetDebugState()}",
                this);
        }

        public void ClearMovementRestriction()
        {
            bool wasActive =
                movementRestrictionActive;
            string previousAnchor =
                DescribeTransform(
                    movementRestrictionAnchor);
            movementRestrictionAnchor = null;
            movementRestrictionActive = false;

            if (body != null)
            {
                targetHorizontalPosition = body.position.x;
            }

            if (dragInput != null)
            {
                dragInput.Clear();
            }

            Debug.Log(
                $"[MovementDebug][Movement] Restriction cleared | wasActive={wasActive} previousAnchor={previousAnchor} {GetDebugState()}",
                this);
        }

        private void RestoreGroundMovementBodyState()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotation;
        }

        public void SetMovementLimits(
            Transform leftLimit,
            Transform rightLimit)
        {
            if (leftLimit == null || rightLimit == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} on '{name}' received missing movement limits.",
                    this);
                return;
            }

            leftMovementLimit = leftLimit;
            rightMovementLimit = rightLimit;

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            targetHorizontalPosition =
                horizontalMovementEnabled
                    ? ClampToMovementLimits(
                        targetHorizontalPosition)
                    : ClampToMovementLimits(
                        body.position.x);

            Debug.Log(
                $"[MovementDebug][Movement] Movement limits assigned | left={DescribeTransform(leftLimit)} right={DescribeTransform(rightLimit)} {GetDebugState()}",
                this);
        }

        public string GetDebugState()
        {
            string bodyState = body != null
                ? $"position={body.position} kinematic={body.isKinematic} gravity={body.useGravity} constraints={body.constraints}"
                : "body=null";
            string configState = config != null
                ? $"sensitivity={config.HorizontalSensitivity:F2} configuredMaxSpeed={config.MaximumHorizontalSpeed:F2}"
                : "config=null";
            string inputState = dragInput != null
                ? dragInput.GetDebugState()
                : "dragInput=null";

            return
                $"forward={forwardMovementEnabled} horizontal={horizontalMovementEnabled} restrictionActive={movementRestrictionActive} restrictionAnchor={DescribeTransform(movementRestrictionAnchor)} hasLimits={HasMovementLimits} left={DescribeTransform(leftMovementLimit)} right={DescribeTransform(rightMovementLimit)} targetX={targetHorizontalPosition:F3} {bodyState} {configState} input=[{inputState}]";
        }

        private static string DescribeTransform(
            Transform target)
        {
            return target != null
                ? $"{target.name}@{target.position}"
                : "null";
        }

        private float ClampToMovementLimits(float horizontalPosition)
        {
            float leftPosition = leftMovementLimit.position.x;
            float rightPosition = rightMovementLimit.position.x;
            float minimum = Mathf.Min(leftPosition, rightPosition);
            float maximum = Mathf.Max(leftPosition, rightPosition);
            return Mathf.Clamp(horizontalPosition, minimum, maximum);
        }

        private bool ValidateRuntimeConfiguration()
        {
            bool isValid = true;

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} on '{name}' requires a {nameof(PlayerConfig)} asset.",
                    this);
                isValid = false;
            }

            if (dragInput == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} on '{name}' requires a {nameof(PlayerDragInput)} component.",
                    this);
                isValid = false;
            }

            return isValid;
        }

        private void OnValidate()
        {
            if (dragInput == null)
            {
                dragInput = GetComponent<PlayerDragInput>();
            }

            if (leftMovementLimit != null &&
                rightMovementLimit != null &&
                Mathf.Approximately(
                    leftMovementLimit.position.x,
                    rightMovementLimit.position.x))
            {
                Debug.LogWarning(
                    $"{nameof(PlayerMovement)} movement limits on '{name}' share the same X position.",
                    this);
            }
        }
    }
}
