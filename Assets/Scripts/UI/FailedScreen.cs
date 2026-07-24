using System;
using UnityEngine;
using UnityEngine.UI;

namespace Picker3D.UI
{
    public sealed class FailedScreen : UIScreenView
    {
        [SerializeField] private Button continueButton;

        public event Action ContinueRequested;

        private void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(
                    HandleContinueRequested);
            }
        }

        private void OnDisable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    HandleContinueRequested);
            }
        }

        private void HandleContinueRequested()
        {
            ContinueRequested?.Invoke();
        }

        private void OnValidate()
        {
            if (continueButton == null)
            {
                continueButton =
                    GetComponentInChildren<Button>(true);
            }
        }
    }
}
