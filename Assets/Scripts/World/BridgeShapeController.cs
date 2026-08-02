using System.Collections;
using UnityEngine;

namespace Picker3D.World
{
    [DisallowMultipleComponent]
    public sealed class BridgeShapeController : MonoBehaviour
    {
        [SerializeField] private GameObject[] sideObjects;
        [SerializeField] private Transform bridgeSurface;
        [SerializeField] private Transform scaleTarget;

        public bool IsExtended { get; private set; }

        public IEnumerator ExtendRoutine(
            float duration,
            AnimationCurve transitionCurve)
        {
            if (IsExtended || !ValidateReferences())
            {
                yield break;
            }

            SetSidesActive(false);

            Vector3 startScale = bridgeSurface.localScale;
            Vector3 targetScale = scaleTarget.localScale;

            if (duration <= 0f)
            {
                bridgeSurface.localScale = targetScale;
                IsExtended = true;
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

                bridgeSurface.localScale = Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    interpolation);

                yield return null;
            }

            bridgeSurface.localScale = targetScale;
            IsExtended = true;
        }

        private void SetSidesActive(bool isActive)
        {
            if (sideObjects == null)
            {
                return;
            }

            for (int index = 0; index < sideObjects.Length; index++)
            {
                GameObject sideObject = sideObjects[index];

                if (sideObject != null)
                {
                    sideObject.SetActive(isActive);
                }
            }
        }

        private bool ValidateReferences()
        {
            if (bridgeSurface == null || scaleTarget == null)
            {
                Debug.LogError(
                    $"{nameof(BridgeShapeController)} on '{name}' requires a bridge surface and scale target.",
                    this);
                return false;
            }

            if (sideObjects == null || sideObjects.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(BridgeShapeController)} on '{name}' requires at least one side object.",
                    this);
                return false;
            }

            if (scaleTarget == bridgeSurface ||
                scaleTarget.IsChildOf(bridgeSurface))
            {
                Debug.LogError(
                    $"{nameof(BridgeShapeController)} scale target on '{name}' must not be the bridge surface or its child.",
                    this);
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (bridgeSurface == null ||
                scaleTarget == null ||
                sideObjects == null ||
                sideObjects.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(BridgeShapeController)} on '{name}' has missing Inspector references.",
                    this);
            }
        }
    }
}
