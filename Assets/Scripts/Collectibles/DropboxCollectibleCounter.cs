using System;
using System.Collections.Generic;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DropboxCollectibleCounter : MonoBehaviour
    {
        [SerializeField] private Collider countingTrigger;

        private readonly HashSet<CollectibleItem> countedItems = new();
        private readonly List<CollectibleItem> countedItemOrder = new();

        public event Action<int> CountChanged;

        public int Count => countedItems.Count;

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

        public void ResetCounter()
        {
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
