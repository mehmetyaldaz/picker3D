using System;
using System.Collections;
using UnityEngine;

namespace Picker3D.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionCollectible : MonoBehaviour
    {
        private const float CollectionAnimationDuration =
            0.65f;
        private const float CollectionRiseHeight = 2f;
        private const float CollectionRotationSpeed = 720f;
        private const float ShrinkStartProgress = 0.4f;

        [Header("Visual")]
        [SerializeField] private Transform rotatingVisual;
        [SerializeField] private MeshRenderer visualRenderer;
        [SerializeField] private Vector3 localRotationAxis =
            Vector3.up;
        [SerializeField, Min(0f)] private float rotationSpeed =
            120f;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float routeSpeed =
            1.5f;
        [SerializeField, Min(0.001f)]
        private float pointReachDistance = 0.03f;

        [Header("Collection")]
        [SerializeField, Min(0.01f)]
        private float collectionDistance = 0.9f;

        private MissionManager missionManager;
        private MissionRoute route;
        private Transform playerTransform;
        private int targetPointIndex;
        private int routeDirection = 1;
        private bool isConfigured;
        private bool isCollected;

        public event Action<MissionCollectible> Collected;

        private void Update()
        {
            if (!isConfigured || isCollected)
            {
                return;
            }

            MoveAlongRoute();
            RotateVisual();
            TryCollect();
        }

        public bool Configure(
            MissionManager sharedMissionManager,
            Transform sharedPlayerTransform,
            MissionRoute movementRoute,
            Material visualMaterial)
        {
            FindLocalReferences();

            if (sharedMissionManager == null ||
                sharedPlayerTransform == null ||
                movementRoute == null ||
                movementRoute.PointCount < 2 ||
                rotatingVisual == null ||
                visualRenderer == null ||
                visualMaterial == null)
            {
                Debug.LogError(
                    $"{nameof(MissionCollectible)} on '{name}' received missing configuration.",
                    this);
                return false;
            }

            missionManager = sharedMissionManager;
            playerTransform = sharedPlayerTransform;
            route = movementRoute;
            visualRenderer.sharedMaterial = visualMaterial;
            route.PrepareForRuntime();

            transform.position =
                route.GetPointPosition(0);
            targetPointIndex = 1;
            routeDirection = 1;
            isCollected = false;
            isConfigured = true;
            return true;
        }

        private void MoveAlongRoute()
        {
            Vector3 targetPosition =
                route.GetPointPosition(targetPointIndex);
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                routeSpeed * Time.deltaTime);

            if ((transform.position - targetPosition)
                    .sqrMagnitude >
                pointReachDistance * pointReachDistance)
            {
                return;
            }

            AdvanceRoutePoint();
        }

        private void AdvanceRoutePoint()
        {
            if (route.RouteMode ==
                MissionRouteMode.Loop)
            {
                targetPointIndex =
                    (targetPointIndex + 1) %
                    route.PointCount;
                return;
            }

            if (targetPointIndex >=
                route.PointCount - 1)
            {
                routeDirection = -1;
            }
            else if (targetPointIndex <= 0)
            {
                routeDirection = 1;
            }

            targetPointIndex += routeDirection;
        }

        private void RotateVisual()
        {
            Vector3 safeAxis =
                localRotationAxis.sqrMagnitude >
                0.0001f
                    ? localRotationAxis.normalized
                    : Vector3.up;
            rotatingVisual.Rotate(
                safeAxis,
                rotationSpeed * Time.deltaTime,
                Space.Self);
        }

        private void TryCollect()
        {
            float maximumDistanceSquared =
                collectionDistance *
                collectionDistance;

            if ((transform.position -
                 playerTransform.position).sqrMagnitude >
                maximumDistanceSquared)
            {
                return;
            }

            isCollected = true;
            missionManager.ReportProgress(
                MissionObjectiveType
                    .CollectMissionCollectible);
            Collected?.Invoke(this);

            GameObject routeObject = route.gameObject;

            if (transform.IsChildOf(route.transform))
            {
                transform.SetParent(
                    route.transform.parent,
                    true);
            }

            Destroy(routeObject);
            StartCoroutine(
                PlayCollectionAnimationRoutine());
        }

        private IEnumerator PlayCollectionAnimationRoutine()
        {
            Vector3 startPosition = transform.position;
            Vector3 startVisualScale =
                rotatingVisual.localScale;
            float elapsedTime = 0f;

            while (elapsedTime <
                   CollectionAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(
                    elapsedTime /
                    CollectionAnimationDuration);
                float riseProgress =
                    1f -
                    (1f - progress) *
                    (1f - progress);
                transform.position =
                    startPosition +
                    Vector3.up *
                    (CollectionRiseHeight *
                     riseProgress);
                rotatingVisual.Rotate(
                    Vector3.up,
                    CollectionRotationSpeed *
                    Time.deltaTime,
                    Space.Self);

                float shrinkProgress =
                    Mathf.InverseLerp(
                        ShrinkStartProgress,
                        1f,
                        progress);
                rotatingVisual.localScale =
                    Vector3.Lerp(
                        startVisualScale,
                        Vector3.zero,
                        shrinkProgress);

                yield return null;
            }

            Destroy(gameObject);
        }

        private void FindLocalReferences()
        {
            if (rotatingVisual == null)
            {
                Transform visual =
                    transform.Find("Visual");
                rotatingVisual =
                    visual != null
                        ? visual
                        : transform;
            }

            if (visualRenderer == null)
            {
                visualRenderer =
                    rotatingVisual
                        .GetComponent<MeshRenderer>();
            }
        }

        private void OnValidate()
        {
            rotationSpeed = Mathf.Max(
                0f,
                rotationSpeed);
            routeSpeed = Mathf.Max(
                0.01f,
                routeSpeed);
            pointReachDistance = Mathf.Max(
                0.001f,
                pointReachDistance);
            collectionDistance = Mathf.Max(
                0.01f,
                collectionDistance);
            FindLocalReferences();
        }
    }
}
