using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class ManualLargeCollectibleLayout : MonoBehaviour
    {
        private const int ExpectedPointCount = 3;

        [SerializeField] private Transform spawnPointGroup;

        public int Capacity =>
            spawnPointGroup != null ? spawnPointGroup.childCount : 0;

        public Vector3 GetWorldPosition(int index)
        {
            Transform point = GetPoint(index);
            return point != null ? point.position : transform.position;
        }

        public Quaternion GetWorldRotation(int index)
        {
            Transform point = GetPoint(index);
            return point != null ? point.rotation : transform.rotation;
        }

        private void Reset()
        {
            spawnPointGroup = transform;
        }

        private Transform GetPoint(int index)
        {
            if (spawnPointGroup == null ||
                index < 0 ||
                index >= spawnPointGroup.childCount)
            {
                Debug.LogError(
                    $"Large layout point index {index} is invalid on '{name}'.",
                    this);
                return null;
            }

            return spawnPointGroup.GetChild(index);
        }

        private void OnValidate()
        {
            if (spawnPointGroup == null)
            {
                spawnPointGroup = transform;
            }

            if (spawnPointGroup.childCount != ExpectedPointCount)
            {
                Debug.LogWarning(
                    $"Large layout '{name}' should have exactly {ExpectedPointCount} spawn points, but has {spawnPointGroup.childCount}.",
                    this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPointGroup == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;

            for (int index = 0; index < spawnPointGroup.childCount; index++)
            {
                Gizmos.DrawWireSphere(
                    spawnPointGroup.GetChild(index).position,
                    0.35f);
            }
        }
    }
}
