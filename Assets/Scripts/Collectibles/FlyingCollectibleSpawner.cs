using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class FlyingCollectibleSpawner : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Transform wing;
        [SerializeField, Min(0f)] private float wingRotationSpeed = 540f;

        [Header("Dropping")]
        [SerializeField] private Transform dropPoint;
        [SerializeField, Min(0.01f)] private float routeSpeed = 4.5f;
        [SerializeField, Min(0f)] private float verticalBobDistance = 0.2f;
        [SerializeField, Min(0f)] private float verticalBobFrequency = 1.5f;
        [SerializeField, Min(0f)] private float downwardSpeed = 1.5f;
        [SerializeField, Min(0f)] private float horizontalSpread = 0.6f;
        [SerializeField, Min(0f)] private float angularSpeed = 3f;

        [Header("Departure")]
        [SerializeField, Min(0.01f)] private float departureDuration = 1.2f;
        [SerializeField, Min(0f)] private float departureUpDistance = 3f;
        [SerializeField, Min(0f)] private float departureForwardDistance = 2f;

        private FlyingCollectibleRoute route;
        private CollectibleItem collectiblePrefab;
        private Transform spawnedItemsParent;
        private System.Random random;
        private Color collectibleColor;
        private float routeLength;
        private float travelledDistance;
        private float bobElapsed;
        private int totalSpawnCount;
        private int spawnedCount;
        private bool isConfigured;
        private bool isRunning;
        private bool isDeparting;
        private float departureElapsed;
        private Vector3 departureStartPosition;
        private Vector3 departureStartScale;
        private Vector3 departureDirection;

        public bool Configure(
            FlyingCollectibleRoute movementRoute,
            CollectibleItem itemPrefab,
            Transform itemParent,
            Color itemColor,
            int spawnCount,
            int seed)
        {
            FindLocalReferences();

            if (movementRoute == null ||
                movementRoute.PointCount < 2 ||
                itemPrefab == null ||
                itemParent == null ||
                dropPoint == null ||
                spawnCount <= 0)
            {
                Debug.LogError(
                    $"{nameof(FlyingCollectibleSpawner)} on '{name}' received missing configuration.",
                    this);
                return false;
            }

            route = movementRoute;
            collectiblePrefab = itemPrefab;
            spawnedItemsParent = itemParent;
            collectibleColor = itemColor;
            totalSpawnCount = spawnCount;
            random = new System.Random(seed);
            routeLength = route.TotalLength;
            travelledDistance = 0f;
            bobElapsed = 0f;
            spawnedCount = 0;
            isRunning = false;
            isDeparting = false;
            isConfigured = routeLength > Mathf.Epsilon;

            if (!isConfigured)
            {
                Debug.LogError(
                    $"{nameof(FlyingCollectibleRoute)} '{route.name}' has no usable length.",
                    route);
                return false;
            }

            route.PrepareForRuntime();
            transform.position =
                route.GetWorldPositionAtDistance(0f);
            return true;
        }

        public void SetRunning(bool shouldRun)
        {
            isRunning = isConfigured &&
                        shouldRun &&
                        travelledDistance < routeLength;
        }

        private void Update()
        {
            RotateWing();

            if (isDeparting)
            {
                AnimateDeparture();
                return;
            }

            if (!isRunning)
            {
                return;
            }

            travelledDistance = Mathf.Min(
                routeLength,
                travelledDistance +
                routeSpeed * Time.deltaTime);
            bobElapsed += Time.deltaTime;

            Vector3 currentPosition =
                route.GetWorldPositionAtDistance(
                    travelledDistance);
            Vector3 nextPosition =
                route.GetWorldPositionAtDistance(
                    Mathf.Min(
                        routeLength,
                        travelledDistance + 0.1f));
            Vector3 forwardDirection =
                nextPosition - currentPosition;

            float verticalOffset =
                Mathf.Sin(
                    bobElapsed *
                    verticalBobFrequency *
                    Mathf.PI *
                    2f) *
                verticalBobDistance;
            transform.position =
                currentPosition +
                Vector3.up * verticalOffset;

            if (forwardDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        forwardDirection.normalized,
                        Vector3.up);
            }

            SpawnDueCollectibles();

            if (travelledDistance >= routeLength)
            {
                isRunning = false;
                BeginDeparture();
            }
        }

        private void RotateWing()
        {
            if (wing == null || wingRotationSpeed <= 0f)
            {
                return;
            }

            wing.Rotate(
                0f,
                wingRotationSpeed * Time.unscaledDeltaTime,
                0f,
                Space.Self);
        }

        private void BeginDeparture()
        {
            if (isDeparting || spawnedCount < totalSpawnCount)
            {
                return;
            }

            isDeparting = true;
            departureElapsed = 0f;
            departureStartPosition = transform.position;
            departureStartScale = transform.localScale;
            departureDirection =
                Vector3.up * departureUpDistance +
                transform.forward * departureForwardDistance;
        }

        private void AnimateDeparture()
        {
            departureElapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                departureElapsed / departureDuration);
            float smoothProgress =
                progress * progress * (3f - 2f * progress);

            transform.position =
                departureStartPosition +
                departureDirection * smoothProgress;
            transform.localScale = Vector3.Lerp(
                departureStartScale,
                Vector3.zero,
                smoothProgress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void SpawnDueCollectibles()
        {
            while (spawnedCount < totalSpawnCount)
            {
                float spawnDistance =
                    routeLength *
                    (spawnedCount + 1f) /
                    (totalSpawnCount + 1f);

                if (travelledDistance < spawnDistance)
                {
                    return;
                }

                SpawnCollectible();
            }
        }

        private void SpawnCollectible()
        {
            CollectibleItem item = Instantiate(
                collectiblePrefab,
                dropPoint.position,
                Quaternion.identity,
                spawnedItemsParent);
            item.name =
                $"{collectiblePrefab.name}_{spawnedCount + 1:00}";
            item.ResetForReuse();
            item.ApplyColor(collectibleColor);

            Rigidbody itemBody =
                item.GetComponent<Rigidbody>();

            if (itemBody != null)
            {
                float horizontalX =
                    RandomRange(
                        -horizontalSpread,
                        horizontalSpread);
                float horizontalZ =
                    RandomRange(
                        -horizontalSpread,
                        horizontalSpread);
                itemBody.isKinematic = false;
                itemBody.useGravity = true;
                itemBody.linearVelocity =
                    new Vector3(
                        horizontalX,
                        -downwardSpeed,
                        horizontalZ);
                itemBody.angularVelocity =
                    new Vector3(
                        RandomRange(-angularSpeed, angularSpeed),
                        RandomRange(-angularSpeed, angularSpeed),
                        RandomRange(-angularSpeed, angularSpeed));
            }

            spawnedCount++;
        }

        private float RandomRange(float minimum, float maximum)
        {
            return Mathf.Lerp(
                minimum,
                maximum,
                (float)random.NextDouble());
        }

        private void FindLocalReferences()
        {
            if (dropPoint == null)
            {
                dropPoint = transform.Find("DropPoint");
            }

            if (wing == null)
            {
                wing = transform.Find("Wing");
            }
        }

        private void OnValidate()
        {
            wingRotationSpeed = Mathf.Max(0f, wingRotationSpeed);
            routeSpeed = Mathf.Max(0.01f, routeSpeed);
            verticalBobDistance =
                Mathf.Max(0f, verticalBobDistance);
            verticalBobFrequency =
                Mathf.Max(0f, verticalBobFrequency);
            downwardSpeed = Mathf.Max(0f, downwardSpeed);
            horizontalSpread = Mathf.Max(0f, horizontalSpread);
            angularSpeed = Mathf.Max(0f, angularSpeed);
            departureDuration = Mathf.Max(0.01f, departureDuration);
            departureUpDistance = Mathf.Max(0f, departureUpDistance);
            departureForwardDistance =
                Mathf.Max(0f, departureForwardDistance);
            FindLocalReferences();
        }
    }
}
