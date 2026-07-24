using System;
using System.Collections.Generic;
using UnityEngine;

namespace Picker3D.UI
{
    [DisallowMultipleComponent]
    public sealed class UIScreenRouter : MonoBehaviour
    {
        [SerializeField] private UIScreenView[] screens;

        private readonly Dictionary<UIScreenId, UIScreenView>
            screensById = new();

        public event Action<UIScreenId, UIScreenId> ScreenChanged;

        public UIScreenId CurrentScreenId { get; private set; } =
            UIScreenId.None;

        private void Awake()
        {
            BuildScreenRegistry();
            HideAll();
        }

        public bool Show(UIScreenId screenId)
        {
            if (screenId == UIScreenId.None)
            {
                HideAll();
                return true;
            }

            if (!screensById.TryGetValue(
                    screenId,
                    out UIScreenView nextScreen))
            {
                Debug.LogError(
                    $"{nameof(UIScreenRouter)} on '{name}' has no screen registered for {screenId}.",
                    this);
                return false;
            }

            UIScreenId previousScreenId = CurrentScreenId;

            foreach (UIScreenView screen in screensById.Values)
            {
                if (screen == nextScreen)
                {
                    screen.Show();
                }
                else
                {
                    screen.Hide();
                }
            }

            CurrentScreenId = screenId;
            ScreenChanged?.Invoke(previousScreenId, CurrentScreenId);
            return true;
        }

        public void HideAll()
        {
            UIScreenId previousScreenId = CurrentScreenId;

            foreach (UIScreenView screen in screensById.Values)
            {
                screen.Hide();
            }

            CurrentScreenId = UIScreenId.None;

            if (previousScreenId != CurrentScreenId)
            {
                ScreenChanged?.Invoke(
                    previousScreenId,
                    CurrentScreenId);
            }
        }

        private void BuildScreenRegistry()
        {
            screensById.Clear();

            if (screens == null || screens.Length == 0)
            {
                screens = GetComponentsInChildren<UIScreenView>(true);
            }

            for (int index = 0; index < screens.Length; index++)
            {
                UIScreenView screen = screens[index];

                if (screen == null)
                {
                    continue;
                }

                if (screen.ScreenId == UIScreenId.None)
                {
                    Debug.LogWarning(
                        $"UI screen '{screen.name}' has no screen ID.",
                        screen);
                    continue;
                }

                if (!screensById.TryAdd(
                        screen.ScreenId,
                        screen))
                {
                    Debug.LogError(
                        $"{nameof(UIScreenRouter)} on '{name}' has more than one {screen.ScreenId} screen.",
                        this);
                }
            }
        }

        private void OnValidate()
        {
            if (screens == null || screens.Length == 0)
            {
                screens = GetComponentsInChildren<UIScreenView>(true);
            }
        }
    }
}
