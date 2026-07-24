using Picker3D.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Picker3D.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelProgressHUD : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private TMP_Text currentLevelText;
        [SerializeField] private TMP_Text nextLevelText;

        [Header("Part Indicators")]
        [SerializeField] private Image[] partIndicators;
        [SerializeField] private Color incompleteColor =
            new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color completedColor =
            new Color(0.08f, 0.08f, 0.08f, 1f);

        private LevelController observedLevel;

        private void OnEnable()
        {
            if (levelManager != null)
            {
                levelManager.LevelStarted += HandleLevelStarted;
            }
        }

        private void Start()
        {
            RefreshForCurrentLevel();
        }

        private void OnDisable()
        {
            if (levelManager != null)
            {
                levelManager.LevelStarted -= HandleLevelStarted;
            }

            ObserveLevel(null);
        }

        private void HandleLevelStarted(int levelNumber)
        {
            RefreshForCurrentLevel();
        }

        private void HandlePartCompleted(int partIndex)
        {
            if (partIndicators == null ||
                partIndex < 0 ||
                partIndex >= partIndicators.Length ||
                partIndicators[partIndex] == null)
            {
                return;
            }

            partIndicators[partIndex].color = completedColor;
        }

        private void RefreshForCurrentLevel()
        {
            if (levelManager == null)
            {
                return;
            }

            int currentLevelNumber =
                levelManager.CurrentLevelNumber;

            if (currentLevelText != null)
            {
                currentLevelText.text =
                    currentLevelNumber.ToString();
            }

            if (nextLevelText != null)
            {
                nextLevelText.text =
                    (currentLevelNumber + 1).ToString();
            }

            ObserveLevel(levelManager.ActiveLevel);
            RefreshPartIndicators();
        }

        private void ObserveLevel(LevelController level)
        {
            if (observedLevel != null)
            {
                observedLevel.PartCompleted -=
                    HandlePartCompleted;
            }

            observedLevel = level;

            if (observedLevel != null)
            {
                observedLevel.PartCompleted +=
                    HandlePartCompleted;
            }
        }

        private void RefreshPartIndicators()
        {
            if (partIndicators == null)
            {
                return;
            }

            int partCount = observedLevel != null
                ? observedLevel.PartCount
                : 0;
            int completedPartCount = observedLevel != null
                ? observedLevel.CompletedPartCount
                : 0;

            for (int index = 0;
                 index < partIndicators.Length;
                 index++)
            {
                Image indicator = partIndicators[index];

                if (indicator == null)
                {
                    continue;
                }

                bool belongsToLevel = index < partCount;
                indicator.gameObject.SetActive(belongsToLevel);

                if (belongsToLevel)
                {
                    indicator.color =
                        index < completedPartCount
                            ? completedColor
                            : incompleteColor;
                }
            }
        }

        private void OnValidate()
        {
            if (currentLevelText == null ||
                nextLevelText == null)
            {
                Debug.LogWarning(
                    $"{nameof(LevelProgressHUD)} on '{name}' requires current and next level text references.",
                    this);
            }

            if (partIndicators == null ||
                partIndicators.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(LevelProgressHUD)} on '{name}' requires at least one part indicator.",
                    this);
            }
        }
    }
}
