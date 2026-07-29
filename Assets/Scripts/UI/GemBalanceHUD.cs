using System.Collections;
using System.Collections.Generic;
using Picker3D.Core;
using TMPro;
using UnityEngine;

namespace Picker3D.UI
{
    [DisallowMultipleComponent]
    public sealed class GemBalanceHUD : MonoBehaviour
    {
        [SerializeField] private GemWallet gemWallet;
        [SerializeField] private TMP_Text balanceText;

        [Header("Flying Gem Animation")]
        [SerializeField] private RectTransform animationRoot;
        [SerializeField] private RectTransform gemIconTemplate;
        [SerializeField] private RectTransform rewardSpawnPoint;
        [SerializeField] private RectTransform gemTarget;
        [SerializeField, Min(1)] private int flyingGemCount = 8;
        [SerializeField, Min(0f)] private float scatterRadius = 90f;
        [SerializeField, Min(0.05f)] private float flyDuration = 0.65f;
        [SerializeField, Min(0f)] private float staggerDelay = 0.08f;
        [SerializeField, Min(0f)] private float arcHeight = 140f;

        private readonly List<FlyingGem> flyingGems = new();
        private Coroutine rewardAnimationRoutine;

        public bool IsRewardAnimationPlaying =>
            rewardAnimationRoutine != null;

        private sealed class FlyingGem
        {
            public RectTransform RectTransform;
            public Vector3 StartPosition;
            public Vector3 ControlPosition;
            public float StartDelay;
            public int GemValue;
            public bool HasArrived;
        }

        private void OnEnable()
        {
            if (gemWallet != null)
            {
                gemWallet.BalanceChanged +=
                    HandleBalanceChanged;
                gemWallet.GemsAdded += HandleGemsAdded;
            }
        }

        private void Start()
        {
            if (gemWallet != null)
            {
                HandleBalanceChanged(gemWallet.TotalGems);
            }
        }

        private void OnDisable()
        {
            if (gemWallet != null)
            {
                gemWallet.BalanceChanged -=
                    HandleBalanceChanged;
                gemWallet.GemsAdded -= HandleGemsAdded;
            }

            FinishAnimationImmediately();
        }

        private void HandleBalanceChanged(int totalGems)
        {
            if (rewardAnimationRoutine == null)
            {
                SetDisplayedBalance(totalGems);
            }
        }

        private void HandleGemsAdded(
            int previousBalance,
            int addedAmount,
            int totalBalance)
        {
            if (!CanAnimateReward())
            {
                SetDisplayedBalance(totalBalance);
                return;
            }

            FinishAnimationImmediately();
            rewardAnimationRoutine = StartCoroutine(
                PlayRewardAnimationRoutine(
                    previousBalance,
                    addedAmount,
                    totalBalance));
        }

        private IEnumerator PlayRewardAnimationRoutine(
            int previousBalance,
            int addedAmount,
            int totalBalance)
        {
            SetDisplayedBalance(previousBalance);
            CreateFlyingGems(addedAmount);

            float elapsed = 0f;
            int arrivedGemValue = 0;
            int arrivedCount = 0;
            float totalDuration =
                flyDuration +
                staggerDelay * Mathf.Max(0, flyingGems.Count - 1);

            while (elapsed < totalDuration &&
                   arrivedCount < flyingGems.Count)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int index = 0;
                     index < flyingGems.Count;
                     index++)
                {
                    FlyingGem flyingGem = flyingGems[index];

                    if (flyingGem.HasArrived ||
                        elapsed < flyingGem.StartDelay)
                    {
                        continue;
                    }

                    float progress = Mathf.Clamp01(
                        (elapsed - flyingGem.StartDelay) /
                        flyDuration);
                    float easedProgress =
                        1f - Mathf.Pow(1f - progress, 3f);
                    flyingGem.RectTransform.position =
                        EvaluateQuadraticBezier(
                            flyingGem.StartPosition,
                            flyingGem.ControlPosition,
                            gemTarget.position,
                            easedProgress);

                    float scale = Mathf.Lerp(
                        1f,
                        0.45f,
                        easedProgress);
                    flyingGem.RectTransform.localScale =
                        Vector3.one * scale;

                    if (progress < 1f)
                    {
                        continue;
                    }

                    flyingGem.HasArrived = true;
                    arrivedCount++;
                    arrivedGemValue += flyingGem.GemValue;
                    SetDisplayedBalance(
                        previousBalance + arrivedGemValue);
                    Destroy(flyingGem.RectTransform.gameObject);
                }

                yield return null;
            }

            SetDisplayedBalance(totalBalance);
            flyingGems.Clear();
            rewardAnimationRoutine = null;
        }

        public IEnumerator WaitForRewardAnimationRoutine()
        {
            while (IsRewardAnimationPlaying)
            {
                yield return null;
            }
        }

        private void CreateFlyingGems(int addedAmount)
        {
            flyingGems.Clear();
            int iconCount = Mathf.Clamp(
                flyingGemCount,
                1,
                Mathf.Max(1, addedAmount));
            int valuePerIcon = addedAmount / iconCount;
            int remainder = addedAmount % iconCount;

            for (int index = 0; index < iconCount; index++)
            {
                RectTransform icon = Instantiate(
                    gemIconTemplate,
                    animationRoot);
                icon.gameObject.SetActive(true);

                Vector2 scatter =
                    Random.insideUnitCircle * scatterRadius;
                Vector3 startPosition =
                    rewardSpawnPoint.position +
                    new Vector3(scatter.x, scatter.y, 0f);
                Vector3 targetPosition = gemTarget.position;
                Vector3 controlPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        0.5f) +
                    Vector3.up * arcHeight +
                    Vector3.right * Random.Range(-60f, 60f);

                icon.position = startPosition;
                icon.localScale = Vector3.one;
                icon.SetAsLastSibling();

                flyingGems.Add(new FlyingGem
                {
                    RectTransform = icon,
                    StartPosition = startPosition,
                    ControlPosition = controlPosition,
                    StartDelay = index * staggerDelay,
                    GemValue =
                        valuePerIcon + (index < remainder ? 1 : 0)
                });
            }
        }

        private Vector3 EvaluateQuadraticBezier(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * start +
                   2f * inverse * progress * control +
                   progress * progress * end;
        }

        private bool CanAnimateReward()
        {
            return isActiveAndEnabled &&
                   animationRoot != null &&
                   gemIconTemplate != null &&
                   rewardSpawnPoint != null &&
                   gemTarget != null;
        }

        private void FinishAnimationImmediately()
        {
            if (rewardAnimationRoutine != null)
            {
                StopCoroutine(rewardAnimationRoutine);
                rewardAnimationRoutine = null;
            }

            for (int index = 0;
                 index < flyingGems.Count;
                 index++)
            {
                RectTransform icon =
                    flyingGems[index].RectTransform;

                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }

            flyingGems.Clear();

            if (gemWallet != null)
            {
                SetDisplayedBalance(gemWallet.TotalGems);
            }
        }

        private void SetDisplayedBalance(int totalGems)
        {
            if (balanceText != null)
            {
                balanceText.text =
                    Mathf.Max(0, totalGems).ToString();
            }
        }

        private void OnValidate()
        {
            flyingGemCount = Mathf.Max(1, flyingGemCount);
            scatterRadius = Mathf.Max(0f, scatterRadius);
            flyDuration = Mathf.Max(0.05f, flyDuration);
            staggerDelay = Mathf.Max(0f, staggerDelay);
            arcHeight = Mathf.Max(0f, arcHeight);

            if (gemWallet == null || balanceText == null)
            {
                Debug.LogWarning(
                    $"{nameof(GemBalanceHUD)} on '{name}' has missing Inspector references.",
                    this);
            }
        }
    }
}
