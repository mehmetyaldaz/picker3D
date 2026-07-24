using System.Collections;
using UnityEngine;

namespace Picker3D.World
{
    [DisallowMultipleComponent]
    public sealed class DropboxMovementController : MonoBehaviour
    {
        [SerializeField] private Transform movingTransform;
        [SerializeField] private Transform raisedTarget;

        public bool IsRaised { get; private set; }

        private void Reset()
        {
            movingTransform = transform;
        }

        public IEnumerator RaiseRoutine(
            float duration,
            AnimationCurve transitionCurve)
        {
            if (IsRaised || !ValidateReferences())
            {
                yield break;
            }

            bool useLocalPosition =
                movingTransform.parent == raisedTarget.parent;
            Vector3 startPosition = useLocalPosition
                ? movingTransform.localPosition
                : movingTransform.position;
            Vector3 targetPosition = useLocalPosition
                ? raisedTarget.localPosition
                : raisedTarget.position;

            if (duration <= 0f)
            {
                SetPosition(targetPosition, useLocalPosition);
                IsRaised = true;
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

                SetPosition(
                    Vector3.LerpUnclamped(
                        startPosition,
                        targetPosition,
                        interpolation),
                    useLocalPosition);

                yield return null;
            }

            SetPosition(targetPosition, useLocalPosition);
            IsRaised = true;
        }

        private void SetPosition(Vector3 position, bool useLocalPosition)
        {
            if (useLocalPosition)
            {
                movingTransform.localPosition = position;
            }
            else
            {
                movingTransform.position = position;
            }
        }

        private bool ValidateReferences()
        {
            if (movingTransform != null && raisedTarget != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(DropboxMovementController)} on '{name}' requires both moving and raised target Transforms.",
                this);
            return false;
        }

        private void OnValidate()
        {
            if (movingTransform == null)
            {
                movingTransform = transform;
            }

            if (raisedTarget == movingTransform)
            {
                Debug.LogWarning(
                    $"{nameof(DropboxMovementController)} on '{name}' cannot use its moving Transform as the raised target.",
                    this);
            }
        }
    }
}
