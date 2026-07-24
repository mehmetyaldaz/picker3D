using UnityEngine;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpinnerPowerUp : MonoBehaviour
    {
        [SerializeField] private GameObject spinnerRoot;
        [SerializeField] private Transform leftSpinnerPivot;
        [SerializeField] private Transform rightSpinnerPivot;
        [SerializeField, Min(0f)] private float rotationSpeed = 480f;
        [SerializeField] private Vector3 localRotationAxis = Vector3.up;

        private Quaternion leftStartRotation;
        private Quaternion rightStartRotation;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            CaptureStartRotations();
            SetActiveState(false);
        }

        private void FixedUpdate()
        {
            if (!IsActive)
            {
                return;
            }

            float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
            leftSpinnerPivot.Rotate(
                localRotationAxis,
                rotationAmount,
                Space.Self);
            rightSpinnerPivot.Rotate(
                localRotationAxis,
                -rotationAmount,
                Space.Self);
        }

        public bool Activate()
        {
            if (!ValidateReferences())
            {
                return false;
            }

            SetActiveState(true);
            return true;
        }

        public void Deactivate()
        {
            IsActive = false;

            if (leftSpinnerPivot != null)
            {
                leftSpinnerPivot.localRotation = leftStartRotation;
            }

            if (rightSpinnerPivot != null)
            {
                rightSpinnerPivot.localRotation = rightStartRotation;
            }

            if (spinnerRoot != null)
            {
                spinnerRoot.SetActive(false);
            }
        }

        private void SetActiveState(bool isActive)
        {
            IsActive = isActive;

            if (spinnerRoot != null)
            {
                spinnerRoot.SetActive(isActive);
            }
        }

        private void CaptureStartRotations()
        {
            if (leftSpinnerPivot != null)
            {
                leftStartRotation = leftSpinnerPivot.localRotation;
            }

            if (rightSpinnerPivot != null)
            {
                rightStartRotation = rightSpinnerPivot.localRotation;
            }
        }

        private bool ValidateReferences()
        {
            bool isValid = spinnerRoot != null &&
                           leftSpinnerPivot != null &&
                           rightSpinnerPivot != null;

            if (!isValid)
            {
                Debug.LogError(
                    $"{nameof(PlayerSpinnerPowerUp)} on '{name}' has missing spinner references.",
                    this);
            }

            return isValid;
        }

        private void OnValidate()
        {
            rotationSpeed = Mathf.Max(0f, rotationSpeed);

            if (localRotationAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                localRotationAxis = Vector3.up;
            }
            else
            {
                localRotationAxis.Normalize();
            }
        }
    }
}
