using System;
using UnityEngine;
using UnityEngine.UI;

namespace Picker3D.UI
{
    public sealed class TapToPlayScreen : UIScreenView
    {
        [SerializeField] private Button tapButton;
        [SerializeField] private Button storeButton;

        public event Action Tapped;
        public event Action StoreRequested;

        private void Reset()
        {
            tapButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (tapButton != null)
            {
                tapButton.onClick.AddListener(HandleTapped);
            }

            if (storeButton != null)
            {
                storeButton.onClick.AddListener(
                    HandleStoreRequested);
            }
        }

        private void OnDisable()
        {
            if (tapButton != null)
            {
                tapButton.onClick.RemoveListener(HandleTapped);
            }

            if (storeButton != null)
            {
                storeButton.onClick.RemoveListener(
                    HandleStoreRequested);
            }
        }

        private void HandleTapped()
        {
            Tapped?.Invoke();
        }

        private void HandleStoreRequested()
        {
            StoreRequested?.Invoke();
        }

        private void OnValidate()
        {
            if (tapButton == null)
            {
                tapButton = GetComponent<Button>();
            }
        }
    }
}
