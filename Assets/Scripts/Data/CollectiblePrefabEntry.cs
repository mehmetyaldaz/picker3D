using System;
using Picker3D.Collectibles;
using UnityEngine;

namespace Picker3D.Data
{
    [Serializable]
    public sealed class CollectiblePrefabEntry
    {
        [SerializeField] private CollectibleItem prefab;
        [SerializeField, Min(1)] private int selectionWeight = 1;

        public CollectibleItem Prefab => prefab;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
    }
}
