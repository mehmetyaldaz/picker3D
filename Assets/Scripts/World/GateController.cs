using System.Collections;
using UnityEngine;

namespace Picker3D.World
{
    [DisallowMultipleComponent]
    public sealed class GateController : MonoBehaviour
    {
        private const float OpenAngle = -70f;

        [SerializeField] private Transform leftGatePivot;
        [SerializeField] private Transform rightGatePivot;

        public bool IsOpen { get; private set; }

        public IEnumerator OpenRoutine(
            float duration,
            AnimationCurve transitionCurve)
        {
            if (IsOpen || !ValidateReferences())
            {
                yield break;
            }

            Quaternion leftStartRotation = leftGatePivot.localRotation;
            Quaternion rightStartRotation = rightGatePivot.localRotation;
            Quaternion leftTargetRotation =
                leftStartRotation *
                Quaternion.AngleAxis(OpenAngle, Vector3.forward);
            Quaternion rightTargetRotation =
                rightStartRotation *
                Quaternion.AngleAxis(OpenAngle, Vector3.forward);

            if (duration <= 0f)
            {
                SetGateRotations(
                    leftTargetRotation,
                    rightTargetRotation);
                IsOpen = true;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float interpolation = transitionCurve != null
                    ? transitionCurve.Evaluate(normalizedTime)
                    : normalizedTime;

                leftGatePivot.localRotation =
                    Quaternion.SlerpUnclamped(
                        leftStartRotation,
                        leftTargetRotation,
                        interpolation);
                rightGatePivot.localRotation =
                    Quaternion.SlerpUnclamped(
                        rightStartRotation,
                        rightTargetRotation,
                        interpolation);

                yield return null;
            }

            SetGateRotations(
                leftTargetRotation,
                rightTargetRotation);
            IsOpen = true;
        }

        private void SetGateRotations(
            Quaternion leftRotation,
            Quaternion rightRotation)
        {
            leftGatePivot.localRotation = leftRotation;
            rightGatePivot.localRotation = rightRotation;
        }

        private bool ValidateReferences()
        {
            if (leftGatePivot == null || rightGatePivot == null)
            {
                Debug.LogError(
                    $"{nameof(GateController)} on '{name}' requires both left and right gate pivots.",
                    this);
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (leftGatePivot == null || rightGatePivot == null)
            {
                Debug.LogWarning(
                    $"{nameof(GateController)} on '{name}' has a missing gate pivot.",
                    this);
            }
        }
    }
}
