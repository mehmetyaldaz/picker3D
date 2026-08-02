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
        [SerializeField] private Button resetButton;
        [SerializeField] private Button addLevelButton;
        [SerializeField] private Button addGemButton;
        [SerializeField] private Button addOneLevelButton;

        public event Action Tapped;
        public event Action StoreRequested;
        public event Action MissionRequested;
        public event Action ResetRequested;
        public event Action AddLevelRequested;
        public event Action AddGemRequested;
        public event Action AddOneLevelRequested;

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

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(
                    HandleResetRequested);
            }

            if (addLevelButton != null)
            {
                addLevelButton.onClick.AddListener(
                    HandleAddLevelRequested);
            }

            if (addGemButton != null)
            {
                addGemButton.onClick.AddListener(
                    HandleAddGemRequested);
            }

            if (addOneLevelButton != null)
            {
                addOneLevelButton.onClick.AddListener(
                    HandleAddOneLevelRequested);
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

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(
                    HandleResetRequested);
            }

            if (addLevelButton != null)
            {
                addLevelButton.onClick.RemoveListener(
                    HandleAddLevelRequested);
            }

            if (addGemButton != null)
            {
                addGemButton.onClick.RemoveListener(
                    HandleAddGemRequested);
            }

            if (addOneLevelButton != null)
            {
                addOneLevelButton.onClick.RemoveListener(
                    HandleAddOneLevelRequested);
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

        private void HandleResetRequested()
        {
            ResetRequested?.Invoke();
        }

        private void HandleAddLevelRequested()
        {
            AddLevelRequested?.Invoke();
        }

        private void HandleAddGemRequested()
        {
            AddGemRequested?.Invoke();
        }

        private void HandleAddOneLevelRequested()
        {
            AddOneLevelRequested?.Invoke();
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

            if (resetButton == null)
            {
                Transform resetButtonTransform =
                    transform.Find("ResetButton");

                if (resetButtonTransform != null)
                {
                    resetButton =
                        resetButtonTransform
                            .GetComponent<Button>();
                }
            }

            if (addLevelButton == null)
            {
                Transform addLevelButtonTransform =
                    transform.Find("AddLevelButton");

                if (addLevelButtonTransform != null)
                {
                    addLevelButton =
                        addLevelButtonTransform
                            .GetComponent<Button>();
                }
            }

            if (addGemButton == null)
            {
                Transform addGemButtonTransform =
                    transform.Find("AddGemButtom") ??
                    transform.Find("AddGemButton");

                if (addGemButtonTransform != null)
                {
                    addGemButton =
                        addGemButtonTransform
                            .GetComponent<Button>();
                }
            }

            if (addOneLevelButton == null)
            {
                Transform addOneLevelButtonTransform =
                    transform.Find("AddOneLevelButton");

                if (addOneLevelButtonTransform != null)
                {
                    addOneLevelButton =
                        addOneLevelButtonTransform
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
