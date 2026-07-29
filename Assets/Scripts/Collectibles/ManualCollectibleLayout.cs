using System.Collections.Generic;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class ManualCollectibleLayout : MonoBehaviour
    {
        private const int BasePointCount = 20;
        private const int ExtraGroupPointCount = 10;

        [SerializeField] private Transform base20Group;
        [SerializeField] private Transform extraTo30Group;
        [SerializeField] private Transform extraTo40Group;
        [SerializeField] private Transform extraTo50Group;
        [SerializeField] private Transform extraTo60Group;

        public int Capacity =>
            GetDirectChildCount(base20Group) +
            GetDirectChildCount(extraTo30Group) +
            GetDirectChildCount(extraTo40Group) +
            GetDirectChildCount(extraTo50Group) +
            GetDirectChildCount(extraTo60Group);

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

        private Transform GetPoint(int index)
        {
            if (index < 0 || index >= Capacity)
            {
                Debug.LogError(
                    $"Layout point index {index} is outside '{name}' capacity {Capacity}.",
                    this);
                return null;
            }

            Transform point = GetPointFromGroup(base20Group, ref index);

            if (point != null)
            {
                return point;
            }

            point = GetPointFromGroup(extraTo30Group, ref index);

            if (point != null)
            {
                return point;
            }

            point = GetPointFromGroup(extraTo40Group, ref index);

            if (point != null)
            {
                return point;
            }

            point = GetPointFromGroup(extraTo50Group, ref index);

            if (point != null)
            {
                return point;
            }

            return GetPointFromGroup(extraTo60Group, ref index);
        }

        private Transform GetPointFromGroup(
            Transform group,
            ref int remainingIndex)
        {
            int childCount = GetDirectChildCount(group);

            if (remainingIndex < childCount)
            {
                return group.GetChild(remainingIndex);
            }

            remainingIndex -= childCount;
            return null;
        }

        private int GetDirectChildCount(Transform group)
        {
            return group != null ? group.childCount : 0;
        }

        private void OnValidate()
        {
            ValidateGroup(base20Group, BasePointCount, nameof(base20Group));
            ValidateGroup(
                extraTo30Group,
                ExtraGroupPointCount,
                nameof(extraTo30Group));
            ValidateGroup(
                extraTo40Group,
                ExtraGroupPointCount,
                nameof(extraTo40Group));
            ValidateGroup(
                extraTo50Group,
                ExtraGroupPointCount,
                nameof(extraTo50Group));
            ValidateGroup(
                extraTo60Group,
                ExtraGroupPointCount,
                nameof(extraTo60Group));

            HashSet<Transform> groups = new();
            ValidateUniqueGroup(groups, base20Group);
            ValidateUniqueGroup(groups, extraTo30Group);
            ValidateUniqueGroup(groups, extraTo40Group);
            ValidateUniqueGroup(groups, extraTo50Group);
            ValidateUniqueGroup(groups, extraTo60Group);
        }

        private void ValidateGroup(
            Transform group,
            int expectedCount,
            string fieldName)
        {
            if (group == null)
            {
                Debug.LogWarning(
                    $"{nameof(ManualCollectibleLayout)} on '{name}' has no {fieldName}.",
                    this);
                return;
            }

            if (group.childCount != expectedCount)
            {
                Debug.LogWarning(
                    $"Layout group '{group.name}' on '{name}' should have {expectedCount} direct point children, but has {group.childCount}.",
                    this);
            }
        }

        private void ValidateUniqueGroup(
            HashSet<Transform> groups,
            Transform group)
        {
            if (group != null && !groups.Add(group))
            {
                Debug.LogError(
                    $"{nameof(ManualCollectibleLayout)} on '{name}' uses group '{group.name}' more than once.",
                    this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            for (int index = 0; index < Capacity; index++)
            {
                Transform point = GetPoint(index);

                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.08f);
                }
            }
        }
    }
}
