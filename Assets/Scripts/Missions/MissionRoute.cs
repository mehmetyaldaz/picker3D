using UnityEngine;

namespace Picker3D.Missions
{
    public enum MissionRouteMode
    {
        Loop = 0,
        PingPong = 1
    }

    [DisallowMultipleComponent]
    public sealed class MissionRoute : MonoBehaviour
    {
        [SerializeField] private MissionRouteMode routeMode =
            MissionRouteMode.Loop;

        public MissionRouteMode RouteMode => routeMode;
        public int PointCount => transform.childCount;

        public Vector3 GetPointPosition(int pointIndex)
        {
            if (PointCount == 0)
            {
                return transform.position;
            }

            int safeIndex = Mathf.Clamp(
                pointIndex,
                0,
                PointCount - 1);
            return transform
                .GetChild(safeIndex)
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
            if (transform.childCount < 2)
            {
                Debug.LogWarning(
                    $"{nameof(MissionRoute)} on '{name}' requires at least two child route points.",
                    this);
            }
        }
    }
}
