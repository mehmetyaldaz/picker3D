using System.Collections.Generic;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class CollectibleCollector : MonoBehaviour
    {
        [SerializeField] private Collider collectionTrigger;

        private readonly HashSet<CollectibleItem> collectedItems = new();

        protected virtual void Reset()
        {
            collectionTrigger = GetComponent<Collider>();
            collectionTrigger.isTrigger = true;
        }

        protected virtual void Awake()
        {
            if (collectionTrigger == null)
            {
                collectionTrigger = GetComponent<Collider>();
            }

            if (collectionTrigger == null || !collectionTrigger.isTrigger)
            {
                Debug.LogError(
                    $"{nameof(CollectibleCollector)} on '{name}' requires an Is Trigger collider.",
                    this);
                enabled = false;
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            CollectibleItem item = other.GetComponentInParent<CollectibleItem>();

            TryRegisterCollectedItem(item);
        }

        public bool TryRegisterIfOverlapping(CollectibleItem item)
        {
            if (item == null || collectionTrigger == null)
            {
                return false;
            }

            Collider[] itemColliders =
                item.GetComponentsInChildren<Collider>();
            bool isOverlapping = false;

            for (int index = 0; index < itemColliders.Length; index++)
            {
                Collider itemCollider = itemColliders[index];

                if (itemCollider != null &&
                    itemCollider.enabled &&
                    collectionTrigger.bounds.Intersects(
                        itemCollider.bounds))
                {
                    isOverlapping = true;
                    break;
                }
            }

            return isOverlapping && TryRegisterCollectedItem(item);
        }

        private bool TryRegisterCollectedItem(CollectibleItem item)
        {
            if (item == null || !item.TryCollect(this))
            {
                return false;
            }

            if (!collectedItems.Add(item))
            {
                return false;
            }

            return true;
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            CollectibleItem item = other.GetComponentInParent<CollectibleItem>();

            if (item == null || !collectedItems.Remove(item))
            {
                return;
            }

            item.Release(Vector3.zero);
        }

        public void ReleaseAll(
            Vector3 targetPosition,
            float movementSpeed)
        {
            foreach (CollectibleItem item in collectedItems)
            {
                if (item == null)
                {
                    continue;
                }

                item.BeginGuidedRelease(
                    targetPosition,
                    movementSpeed);
            }

            collectedItems.Clear();
        }

        public void ReleaseAll(
            DropboxCollectibleCounter dropboxCounter,
            float movementSpeed)
        {
            if (dropboxCounter == null)
            {
                return;
            }

            int itemCount = collectedItems.Count;
            int itemIndex = 0;

            foreach (CollectibleItem item in collectedItems)
            {
                if (item != null)
                {
                    item.BeginGuidedRelease(
                        dropboxCounter.GetReleaseTargetPosition(
                            itemIndex,
                            itemCount),
                        movementSpeed);
                }

                itemIndex++;
            }

            collectedItems.Clear();
        }

        protected virtual void OnDisable()
        {
            foreach (CollectibleItem item in collectedItems)
            {
                if (item != null)
                {
                    item.Release(Vector3.zero);
                }
            }

            collectedItems.Clear();
        }

        protected virtual void OnValidate()
        {
            if (collectionTrigger == null)
            {
                collectionTrigger = GetComponent<Collider>();
            }

            if (collectionTrigger != null && !collectionTrigger.isTrigger)
            {
                Debug.LogWarning(
                    $"{nameof(CollectibleCollector)} collider on '{name}' must have Is Trigger enabled.",
                    this);
            }
        }
    }
}
