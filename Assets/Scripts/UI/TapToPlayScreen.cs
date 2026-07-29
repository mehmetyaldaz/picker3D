using System;
using UnityEngine;
using UnityEngine.UI;

namespace Picker3D.UI
{
    public sealed class TapToPlayScreen : UIScreenView
    {
        [SerializeField] private Button tapButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button missionButton;

        public event Action Tapped;
        public event Action StoreRequested;
        public event Action MissionRequested;

        private void Reset()
        {
            tapButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            FindLocalReferences();

            if (tapButton != null)
            {
                tapButton.onClick.AddListener(HandleTapped);
            }

            if (storeButton != null)
            {
                storeButton.onClick.AddListener(
                    HandleStoreRequested);
            }

            if (missionButton != null)
            {
                missionButton.onClick.AddListener(
                    HandleMissionRequested);
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

            if (missionButton != null)
            {
                missionButton.onClick.RemoveListener(
                    HandleMissionRequested);
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

        private void HandleMissionRequested()
        {
            MissionRequested?.Invoke();
        }

        private void FindLocalReferences()
        {
            if (tapButton == null)
            {
                tapButton = GetComponent<Button>();
            }

            if (missionButton == null)
            {
                Transform missionButtonTransform =
                    transform.Find("MissionButton");

                if (missionButtonTransform != null)
                {
                    missionButton =
                        missionButtonTransform
                            .GetComponent<Button>();
                }
            }
        }

        private void OnValidate()
        {
            FindLocalReferences();
        }
    }
}
