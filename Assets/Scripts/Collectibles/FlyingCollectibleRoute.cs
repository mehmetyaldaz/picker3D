using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class FlyingCollectibleRoute : MonoBehaviour
    {
        public int PointCount => transform.childCount;

        public float TotalLength
        {
            get
            {
                float totalLength = 0f;

                for (int index = 1;
                     index < PointCount;
                     index++)
                {
                    totalLength += Vector3.Distance(
                        transform.GetChild(index - 1).position,
                        transform.GetChild(index).position);
                }

                return totalLength;
            }
        }

        public Vector3 GetWorldPositionAtDistance(
            float routeDistance)
        {
            if (PointCount == 0)
            {
                return transform.position;
            }

            if (PointCount == 1)
            {
                return transform.GetChild(0).position;
            }

            float remainingDistance =
                Mathf.Max(0f, routeDistance);

            for (int index = 1;
                 index < PointCount;
                 index++)
            {
                Vector3 segmentStart =
                    transform.GetChild(index - 1).position;
                Vector3 segmentEnd =
                    transform.GetChild(index).position;
                float segmentLength =
                    Vector3.Distance(
                        segmentStart,
                        segmentEnd);

                if (remainingDistance <= segmentLength)
                {
                    float progress =
                        segmentLength > Mathf.Epsilon
                            ? remainingDistance /
                              segmentLength
                            : 1f;
                    return Vector3.Lerp(
                        segmentStart,
                        segmentEnd,
                        progress);
                }

                remainingDistance -= segmentLength;
            }

            return transform
                .GetChild(PointCount - 1)
                .position;
        }

        public void PrepareForRuntime()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            foreach (Renderer routeRenderer in renderers)
            {
                routeRenderer.enabled = false;
            }

            Collider[] colliders =
                GetComponentsInChildren<Collider>(true);

            foreach (Collider routeCollider in colliders)
            {
                routeCollider.enabled = false;
            }
        }

        private void OnValidate()
        {
            if (PointCount < 2)
            {
                Debug.LogWarning(
                    $"{nameof(FlyingCollectibleRoute)} on '{name}' requires at least two child route points.",
                    this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            for (int index = 0;
                 index < PointCount;
                 index++)
            {
                Transform point = transform.GetChild(index);
                Gizmos.DrawSphere(point.position, 0.12f);

                if (index > 0)
                {
                    Gizmos.DrawLine(
                        transform.GetChild(index - 1).position,
                        point.position);
                }
            }
        }
    }
}
