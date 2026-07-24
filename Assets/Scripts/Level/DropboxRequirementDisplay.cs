using Picker3D.Collectibles;
using TMPro;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    public sealed class DropboxRequirementDisplay : MonoBehaviour
    {
        [SerializeField] private DropboxCollectibleCounter counter;
        [SerializeField] private TMP_Text counterText;

        private int requiredCount;

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();

            if (counter != null)
            {
                counter.CountChanged += HandleCountChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (counter != null)
            {
                counter.CountChanged -= HandleCountChanged;
            }
        }

        public void Configure(int newRequiredCount)
        {
            requiredCount = Mathf.Max(0, newRequiredCount);
            SetVisible(true);
            Refresh();
        }

        public void SetVisible(bool isVisible)
        {
            if (counterText != null)
            {
                counterText.gameObject.SetActive(isVisible);
            }
        }

        private void HandleCountChanged(int count)
        {
            Refresh(count);
        }

        private void Refresh()
        {
            Refresh(counter != null ? counter.Count : 0);
        }

        private void Refresh(int count)
        {
            if (counterText != null)
            {
                counterText.text = $"{Mathf.Max(0, count)}/{requiredCount}";
            }
        }

        private void FindReferences()
        {
            if (counter == null)
            {
                counter = GetComponentInChildren<
                    DropboxCollectibleCounter>(true);
            }

            if (counterText == null)
            {
                counterText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnValidate()
        {
            FindReferences();

            if (counter == null || counterText == null)
            {
                Debug.LogWarning(
                    $"{nameof(DropboxRequirementDisplay)} on '{name}' requires a Dropbox counter and TMP text.",
                    this);
            }
        }
    }
}
