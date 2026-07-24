using System.Collections;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LargeCollectibleSplitter : MonoBehaviour
    {
        [SerializeField] private CollectibleItem smallCollectiblePrefab;
        [SerializeField] private CollectibleAppearance appearance;
        [SerializeField, Min(0.05f)] private float spawnSpacing = 0.28f;
        [SerializeField, Min(0f)] private float preSplitMoveDistance = 0.25f;
        [SerializeField, Min(0.01f)] private float preSplitMoveDuration = 0.12f;

        private int smallCollectibleCount;
        private int splitSeed;
        private Color collectibleColor = Color.white;
        private CollectibleCollector contactCollector;
        private bool isConfigured;
        private bool hasSplit;

        public CollectibleShape Shape =>
            smallCollectiblePrefab != null
                ? smallCollectiblePrefab.Shape
                : CollectibleShape.Sphere;

        public bool Configure(int smallCount, Color color, int seed)
        {
            if (smallCollectiblePrefab == null || smallCount <= 0)
            {
                Debug.LogError(
                    $"{nameof(LargeCollectibleSplitter)} on '{name}' has invalid split configuration.",
                    this);
                return false;
            }

            smallCollectibleCount = smallCount;
            collectibleColor = color;
            splitSeed = seed;
            isConfigured = true;
            hasSplit = false;

            if (appearance != null)
            {
                appearance.ApplyColor(collectibleColor);
            }

            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TrySplitFromPlayerContact(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TrySplitFromPlayerContact(collision.collider);
        }

        private void TrySplitFromPlayerContact(Collider other)
        {
            if (hasSplit || !isConfigured || other == null)
            {
                return;
            }

            contactCollector = FindPlayerCollector(other);

            if (contactCollector == null &&
                other.GetComponentInParent<PlayerMovement>() == null)
            {
                return;
            }

            hasSplit = true;
            Vector3 moveDirection =
                other.bounds.center - transform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                moveDirection.Normalize();
            }

            StartCoroutine(PreSplitRoutine(moveDirection));
        }

        private IEnumerator PreSplitRoutine(Vector3 moveDirection)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition =
                startPosition + moveDirection * preSplitMoveDistance;
            float elapsed = 0f;

            while (elapsed < preSplitMoveDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    elapsed / preSplitMoveDuration);
                transform.position = Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            transform.position = targetPosition;
            SpawnSmallCollectibles();
            Destroy(gameObject);
        }

        private CollectibleCollector FindPlayerCollector(Collider other)
        {
            CollectibleCollector collector =
                other.GetComponentInParent<CollectibleCollector>();

            if (collector != null)
            {
                return collector;
            }

            PlayerMovement playerMovement =
                other.GetComponentInParent<PlayerMovement>();
            return playerMovement != null
                ? playerMovement.GetComponentInChildren<CollectibleCollector>()
                : null;
        }

        private void SpawnSmallCollectibles()
        {
            System.Random random = new(splitSeed);
            int gridSize = Mathf.Max(
                1,
                Mathf.CeilToInt(Mathf.Pow(smallCollectibleCount, 1f / 3f)));
            int pointsPerLayer = gridSize * gridSize;
            Transform spawnedParent = transform.parent;

            for (int index = 0; index < smallCollectibleCount; index++)
            {
                int layer = index / pointsPerLayer;
                int layerIndex = index % pointsPerLayer;
                int row = layerIndex / gridSize;
                int column = layerIndex % gridSize;
                float center = (gridSize - 1) * 0.5f;
                Vector3 offset = new(
                    (column - center) * spawnSpacing,
                    layer * spawnSpacing,
                    (row - center) * spawnSpacing);
                Quaternion rotation = Quaternion.Euler(
                    random.Next(0, 360),
                    random.Next(0, 360),
                    random.Next(0, 360));
                CollectibleItem item = Instantiate(
                    smallCollectiblePrefab,
                    transform.position + transform.rotation * offset,
                    rotation,
                    spawnedParent);
                item.name = $"{smallCollectiblePrefab.name}_{index + 1:00}";
                item.ResetForReuse();
                item.ApplyColor(collectibleColor);
                contactCollector?.TryRegisterIfOverlapping(item);
            }
        }

        private void OnValidate()
        {
            if (appearance == null)
            {
                appearance = GetComponent<CollectibleAppearance>();
            }

            spawnSpacing = Mathf.Max(0.05f, spawnSpacing);
            preSplitMoveDistance = Mathf.Max(0f, preSplitMoveDistance);
            preSplitMoveDuration = Mathf.Max(0.01f, preSplitMoveDuration);

            if (smallCollectiblePrefab == null)
            {
                Debug.LogWarning(
                    $"{nameof(LargeCollectibleSplitter)} on '{name}' requires its matching small collectible prefab.",
                    this);
            }
        }
    }
}
