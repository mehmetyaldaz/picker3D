using System;
using System.Collections.Generic;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DropboxCollectibleCounter : MonoBehaviour
    {
        private const float ReleaseTargetSpread = 0.32f;
        private const float GoldenAngleDegrees = 137.50776f;

        [SerializeField] private Collider countingTrigger;

        private readonly HashSet<CollectibleItem> countedItems = new();
        private readonly List<CollectibleItem> countedItemOrder = new();

        public event Action<int> CountChanged;

        public int Count => countedItems.Count;

        public Vector3 GetReleaseTargetPosition(
            int itemIndex,
            int itemCount)
        {
            if (countingTrigger == null)
            {
                return transform.position;
            }

            int safeItemCount = Mathf.Max(1, itemCount);
            int safeItemIndex = Mathf.Max(0, itemIndex);
            float radius =
                Mathf.Sqrt(
                    (safeItemIndex + 1f) /
                    safeItemCount) *
                ReleaseTargetSpread;
            float angle =
                safeItemIndex *
                GoldenAngleDegrees *
                Mathf.Deg2Rad;
            float horizontalOffset =
                Mathf.Cos(angle) * radius;
            float depthOffset =
                Mathf.Sin(angle) * radius;

            if (countingTrigger is BoxCollider boxCollider)
            {
                Vector3 localTarget =
                    boxCollider.center +
                    new Vector3(
                        horizontalOffset *
                        boxCollider.size.x,
                        0f,
                        depthOffset *
                        boxCollider.size.z);
                return boxCollider.transform.TransformPoint(
                    localTarget);
            }

            Bounds triggerBounds = countingTrigger.bounds;
            return triggerBounds.center +
                   new Vector3(
                       horizontalOffset *
                       triggerBounds.size.x,
                       0f,
                       depthOffset *
                       triggerBounds.size.z);
        }

        protected virtual void Reset()
        {
            countingTrigger = GetComponent<Collider>();
            countingTrigger.isTrigger = true;
        }

        protected virtual void Awake()
        {
            if (countingTrigger == null)
            {
                countingTrigger = GetComponent<Collider>();
            }

            if (countingTrigger == null || !countingTrigger.isTrigger)
            {
                Debug.LogError(
                    $"{nameof(DropboxCollectibleCounter)} on '{name}' requires an Is Trigger collider.",
                    this);
                enabled = false;
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            CollectibleItem item = other.GetComponentInParent<CollectibleItem>();

            if (item == null || !item.TryMarkCountedByDropbox())
            {
                return;
            }

            if (countedItems.Add(item))
            {
                countedItemOrder.Add(item);
                CountChanged?.Invoke(countedItems.Count);
            }
        }

        public void ConsumeAllCountedItems()
        {
            for (int index = 0; index < countedItemOrder.Count; index++)
            {
                CollectibleItem item = countedItemOrder[index];

                if (item == null)
                {
                    continue;
                }

                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }

            countedItems.Clear();
            countedItemOrder.Clear();
            CountChanged?.Invoke(0);
        }

        protected virtual void OnValidate()
        {
            if (countingTrigger == null)
            {
                countingTrigger = GetComponent<Collider>();
            }

            if (countingTrigger != null && !countingTrigger.isTrigger)
            {
                Debug.LogWarning(
                    $"{nameof(DropboxCollectibleCounter)} collider on '{name}' must have Is Trigger enabled.",
                    this);
            }
        }
    }
}
