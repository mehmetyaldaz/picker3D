using UnityEngine;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerDragInput))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerDragInput dragInput;

        [Header("Horizontal Limits")]
        [SerializeField] private Transform leftMovementLimit;
        [SerializeField] private Transform rightMovementLimit;

        private Rigidbody body;
        private float targetHorizontalPosition;
        private bool automaticMovementEnabled;

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

            targetHorizontalPosition = body.position.x;

            if (!ValidateRuntimeConfiguration())
            {
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            float normalizedDrag = dragInput.ConsumeHorizontalDrag();

            if (!automaticMovementEnabled || !HasMovementLimits)
            {
                targetHorizontalPosition = body.position.x;
                return;
            }

            targetHorizontalPosition += normalizedDrag * config.HorizontalSensitivity;
            targetHorizontalPosition = ClampToMovementLimits(targetHorizontalPosition);

            float horizontalPosition = Mathf.MoveTowards(
                body.position.x,
                targetHorizontalPosition,
                config.MaximumHorizontalSpeed * Time.fixedDeltaTime);

            Vector3 nextPosition = body.position;
            nextPosition.x = horizontalPosition;
            nextPosition.z += config.ForwardSpeed * Time.fixedDeltaTime;
            body.MovePosition(nextPosition);
        }

        public void SetAutomaticMovementEnabled(bool isEnabled)
        {
            automaticMovementEnabled = isEnabled;

            if (isEnabled)
            {
                RestoreGroundMovementBodyState();

                if (dragInput != null)
                {
                    dragInput.enabled = true;
                    dragInput.Clear();
                }

                if (body != null)
                {
                    targetHorizontalPosition =
                        HasMovementLimits
                            ? ClampToMovementLimits(body.position.x)
                            : body.position.x;
                }

                return;
            }

            if (!isEnabled && body != null)
            {
                targetHorizontalPosition = body.position.x;
            }

            if (!isEnabled && dragInput != null)
            {
                dragInput.Clear();
                dragInput.enabled = false;
            }
        }

        public void PrepareForLevel(
            Transform leftLimit,
            Transform rightLimit)
        {
            SetMovementLimits(leftLimit, rightLimit);

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            RestoreGroundMovementBodyState();

            if (dragInput != null)
            {
                dragInput.Clear();
                dragInput.enabled = automaticMovementEnabled;
            }

            targetHorizontalPosition =
                ClampToMovementLimits(body.position.x);
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

            targetHorizontalPosition = ClampToMovementLimits(body.position.x);
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
