using System;
using System.Collections;
using System.Collections.Generic;
using Picker3D.Core;
using Picker3D.Cosmetics;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Picker3D.UI
{
    public sealed class StoreScreen : UIScreenView
    {
        [Header("Navigation")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button skinsButton;
        [SerializeField] private Button colorsButton;

        [Header("Sections")]
        [SerializeField] private GameObject skinsSection;
        [SerializeField] private GameObject colorSection;

        [Header("Cosmetic State")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject selectedOverlay;
        [SerializeField] private Button unlockRandomButton;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private GameObject outOfStockButton;

        [Header("Unlock Animation")]
        [SerializeField] private GameObject animationOverlay;
        [SerializeField, Min(2)] private int animationStepCount = 12;
        [SerializeField, Min(0.01f)] private float firstStepDuration = 0.06f;
        [SerializeField, Min(0.01f)] private float lastStepDuration = 0.3f;
        [SerializeField, Min(0f)] private float resultHoldDuration = 0.75f;

        [Header("Systems")]
        [SerializeField] private GemWallet gemWallet;
        [SerializeField] private PlayerCosmeticController
            cosmeticController;

        [Header("Prices")]
        [SerializeField, Min(0)] private int skinPrice = 3000;
        [SerializeField, Min(0)] private int colorPrice = 2000;

        private Button[] skinItemButtons;
        private Button[] colorItemButtons;
        private GameObject[] lockedOverlayItems;
        private GameObject[] selectedOverlayItems;
        private GameObject[] animationOverlayItems;
        private UnityAction[] skinItemActions;
        private UnityAction[] colorItemActions;
        private CosmeticCategory activeCategory =
            CosmeticCategory.Skin;
        private string lastLoggedCosmeticState;
        private Coroutine unlockAnimationRoutine;
        private CosmeticCategory pendingUnlockCategory;
        private int pendingUnlockIndex = -1;
        private bool hasPendingUnlock;
        private bool unlockAnimationPlaying;

        public event Action CloseRequested;

        private void OnEnable()
        {
            FindLocalReferences();
            CacheItemViews();

            if (!unlockAnimationPlaying &&
                animationOverlay != null)
            {
                animationOverlay.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(
                    HandleCloseRequested);
            }

            if (skinsButton != null)
            {
                skinsButton.onClick.AddListener(ShowSkins);
            }

            if (colorsButton != null)
            {
                colorsButton.onClick.AddListener(ShowColors);
            }

            if (unlockRandomButton != null)
            {
                unlockRandomButton.onClick.AddListener(
                    HandleUnlockRandom);
            }

            if (gemWallet != null)
            {
                gemWallet.BalanceChanged +=
                    HandleGemBalanceChanged;
            }

            RegisterItemListeners();
        }

        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    HandleCloseRequested);
            }

            if (skinsButton != null)
            {
                skinsButton.onClick.RemoveListener(ShowSkins);
            }

            if (colorsButton != null)
            {
                colorsButton.onClick.RemoveListener(ShowColors);
            }

            if (unlockRandomButton != null)
            {
                unlockRandomButton.onClick.RemoveListener(
                    HandleUnlockRandom);
            }

            if (gemWallet != null)
            {
                gemWallet.BalanceChanged -=
                    HandleGemBalanceChanged;
            }

            UnregisterItemListeners();
            StopUnlockAnimation(true);
        }

        protected override void OnShown()
        {
            FindLocalReferences();
            CacheItemViews();
            RegisterItemListeners();
            ShowSkins();
        }

        private void HandleCloseRequested()
        {
            if (unlockAnimationPlaying)
            {
                return;
            }

            CloseRequested?.Invoke();
        }

        private void ShowSkins()
        {
            ShowCategory(CosmeticCategory.Skin);
        }

        private void ShowColors()
        {
            ShowCategory(CosmeticCategory.Color);
        }

        private void ShowCategory(
            CosmeticCategory category)
        {
            if (unlockAnimationPlaying)
            {
                return;
            }

            activeCategory = category;
            SetSectionVisibility(
                category == CosmeticCategory.Skin);
            RefreshCosmeticState();
        }

        private void SetSectionVisibility(bool showSkins)
        {
            if (skinsSection != null)
            {
                skinsSection.SetActive(showSkins);
            }

            if (colorSection != null)
            {
                colorSection.SetActive(!showSkins);
            }
        }

        private void HandleUnlockRandom()
        {
            if (unlockAnimationPlaying ||
                cosmeticController == null ||
                gemWallet == null ||
                !cosmeticController.TryGetRandomLockedIndex(
                    activeCategory,
                    out int itemIndex))
            {
                RefreshCosmeticState();
                return;
            }

            int price = GetActivePrice();

            if (!gemWallet.TrySpendGems(price))
            {
                RefreshCosmeticState();
                return;
            }

            pendingUnlockCategory = activeCategory;
            pendingUnlockIndex = itemIndex;
            hasPendingUnlock = true;
            unlockAnimationPlaying = true;
            SetStoreInteraction(false);

            if (animationOverlay == null)
            {
                Debug.LogWarning(
                    $"{nameof(StoreScreen)} could not find AnimationOverlay; the cosmetic will be unlocked without the preview animation.",
                    this);
                CompletePendingUnlock();
                StopUnlockAnimation(false);
                return;
            }

            animationOverlay.SetActive(true);
            unlockAnimationRoutine = StartCoroutine(
                PlayUnlockAnimationRoutine(
                    pendingUnlockCategory,
                    pendingUnlockIndex));
        }

        private IEnumerator PlayUnlockAnimationRoutine(
            CosmeticCategory category,
            int unlockedItemIndex)
        {
            List<int> candidateIndices =
                GetAnimationCandidateIndices(category);

            if (!candidateIndices.Contains(
                    unlockedItemIndex))
            {
                candidateIndices.Add(unlockedItemIndex);
            }

            int stepCount = Mathf.Max(
                2,
                animationStepCount);
            int previousItemIndex = -1;

            for (int stepIndex = 0;
                 stepIndex < stepCount;
                 stepIndex++)
            {
                bool isFinalStep =
                    stepIndex == stepCount - 1;
                int previewItemIndex =
                    isFinalStep
                        ? unlockedItemIndex
                        : GetRandomAnimationIndex(
                            candidateIndices,
                            previousItemIndex);

                ShowAnimationPreview(
                    category,
                    previewItemIndex);
                previousItemIndex = previewItemIndex;

                float progress =
                    stepCount > 1
                        ? (float)stepIndex /
                          (stepCount - 1)
                        : 1f;
                float stepDuration = Mathf.Lerp(
                    firstStepDuration,
                    lastStepDuration,
                    progress * progress);

                yield return new WaitForSecondsRealtime(
                    stepDuration);
            }

            CompletePendingUnlock();
            RefreshCosmeticState();

            if (resultHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    resultHoldDuration);
            }

            unlockAnimationRoutine = null;
            StopUnlockAnimation(false);
        }

        private List<int> GetAnimationCandidateIndices(
            CosmeticCategory category)
        {
            Button[] itemButtons =
                GetItemButtons(category);
            int candidateCount = Mathf.Min(
                cosmeticController.GetItemCount(category),
                itemButtons != null
                    ? itemButtons.Length
                    : 0,
                animationOverlayItems != null
                    ? animationOverlayItems.Length
                    : 0);
            List<int> candidateIndices = new();

            for (int index = 0;
                 index < candidateCount;
                 index++)
            {
                if (itemButtons[index] != null &&
                    animationOverlayItems[index] != null &&
                    !cosmeticController.IsUnlocked(
                        category,
                        index))
                {
                    candidateIndices.Add(index);
                }
            }

            return candidateIndices;
        }

        private int GetRandomAnimationIndex(
            List<int> candidateIndices,
            int previousItemIndex)
        {
            if (candidateIndices.Count == 0)
            {
                return pendingUnlockIndex;
            }

            if (candidateIndices.Count == 1)
            {
                return candidateIndices[0];
            }

            int itemIndex;

            do
            {
                itemIndex =
                    candidateIndices[
                        UnityEngine.Random.Range(
                            0,
                            candidateIndices.Count)];
            }
            while (itemIndex == previousItemIndex);

            return itemIndex;
        }

        private void ShowAnimationPreview(
            CosmeticCategory category,
            int itemIndex)
        {
            HideAllAnimationOverlayItems();

            if (animationOverlayItems == null ||
                itemIndex < 0 ||
                itemIndex >= animationOverlayItems.Length ||
                animationOverlayItems[itemIndex] == null ||
                cosmeticController.IsUnlocked(
                    category,
                    itemIndex))
            {
                return;
            }

            animationOverlayItems[itemIndex].SetActive(true);
        }

        private void CompletePendingUnlock()
        {
            if (!hasPendingUnlock)
            {
                return;
            }

            bool unlocked =
                cosmeticController != null &&
                cosmeticController.Unlock(
                    pendingUnlockCategory,
                    pendingUnlockIndex);

            if (unlocked)
            {
                Debug.Log(
                    $"Unlocked {pendingUnlockCategory} {pendingUnlockIndex + 1}.",
                    this);
            }

            hasPendingUnlock = false;
            pendingUnlockIndex = -1;
        }

        private void StopUnlockAnimation(
            bool completePendingUnlock)
        {
            if (unlockAnimationRoutine != null)
            {
                StopCoroutine(unlockAnimationRoutine);
                unlockAnimationRoutine = null;
            }

            if (completePendingUnlock)
            {
                CompletePendingUnlock();
            }

            HideAllAnimationOverlayItems();

            if (animationOverlay != null)
            {
                animationOverlay.SetActive(false);
            }

            unlockAnimationPlaying = false;
            SetStoreInteraction(true);
            RefreshCosmeticState();
        }

        private void HideAllAnimationOverlayItems()
        {
            if (animationOverlayItems == null)
            {
                return;
            }

            foreach (GameObject overlayItem
                     in animationOverlayItems)
            {
                if (overlayItem != null)
                {
                    overlayItem.SetActive(false);
                }
            }
        }

        private void SetStoreInteraction(bool isInteractable)
        {
            if (closeButton != null)
            {
                closeButton.interactable = isInteractable;
            }

            if (skinsButton != null)
            {
                skinsButton.interactable = isInteractable;
            }

            if (colorsButton != null)
            {
                colorsButton.interactable = isInteractable;
            }

            SetItemButtonInteraction(
                skinItemButtons,
                isInteractable);
            SetItemButtonInteraction(
                colorItemButtons,
                isInteractable);
        }

        private void SetItemButtonInteraction(
            Button[] buttons,
            bool isInteractable)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    button.interactable = isInteractable;
                }
            }
        }

        private void HandleItemPressed(
            CosmeticCategory category,
            int itemIndex)
        {
            if (cosmeticController == null)
            {
                Debug.LogError(
                    $"{nameof(StoreScreen)} cannot select {category} {itemIndex + 1}: cosmetic controller is missing.",
                    this);
                return;
            }

            bool selectionChanged =
                cosmeticController.ToggleSelection(
                category,
                itemIndex);

            Debug.Log(
                selectionChanged
                    ? $"Store selected/toggled {category} {itemIndex + 1}."
                    : $"Store rejected {category} {itemIndex + 1}; the item is still locked or has no material.",
                this);
            RefreshCosmeticState();
        }

        private void HandleGemBalanceChanged(int balance)
        {
            RefreshCosmeticState();
        }

        private void RefreshCosmeticState()
        {
            if (cosmeticController == null)
            {
                return;
            }

            Button[] activeButtons =
                GetItemButtons(activeCategory);
            int itemCount = Mathf.Min(
                cosmeticController.GetItemCount(
                    activeCategory),
                activeButtons != null
                    ? activeButtons.Length
                    : 0,
                lockedOverlayItems != null
                    ? lockedOverlayItems.Length
                    : 0,
                selectedOverlayItems != null
                    ? selectedOverlayItems.Length
                    : 0);
            bool hasLockedItem = false;
            List<int> unlockedItemNumbers = new();

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(true);
            }

            if (selectedOverlay != null)
            {
                selectedOverlay.SetActive(true);
            }

            for (int index = 0;
                 index < itemCount;
                 index++)
            {
                bool isUnlocked =
                    cosmeticController.IsUnlocked(
                        activeCategory,
                        index);
                bool isSelected =
                    cosmeticController.IsSelected(
                        activeCategory,
                        index);

                lockedOverlayItems[index].SetActive(
                    !isUnlocked);
                selectedOverlayItems[index].SetActive(
                    isSelected);
                activeButtons[index].interactable =
                    !unlockAnimationPlaying;
                hasLockedItem |= !isUnlocked;

                if (isUnlocked)
                {
                    unlockedItemNumbers.Add(index + 1);
                }
            }

            LogCosmeticState(unlockedItemNumbers);

            int price = GetActivePrice();

            if (priceText != null)
            {
                priceText.text = price.ToString();
            }

            if (unlockRandomButton != null)
            {
                unlockRandomButton.gameObject.SetActive(
                    hasLockedItem);
                unlockRandomButton.interactable =
                    !unlockAnimationPlaying &&
                    hasLockedItem &&
                    gemWallet != null &&
                    gemWallet.TotalGems >= price;
            }

            if (outOfStockButton != null)
            {
                outOfStockButton.SetActive(
                    !hasLockedItem);
            }
        }

        private int GetActivePrice()
        {
            return activeCategory == CosmeticCategory.Skin
                ? skinPrice
                : colorPrice;
        }

        private void LogCosmeticState(
            List<int> unlockedItemNumbers)
        {
            string unlockedItems =
                unlockedItemNumbers.Count > 0
                    ? string.Join(", ", unlockedItemNumbers)
                    : "none";
            string state =
                $"{activeCategory}: {unlockedItems}";

            if (state == lastLoggedCosmeticState)
            {
                return;
            }

            lastLoggedCosmeticState = state;
            Debug.Log(
                $"Unlocked {activeCategory} items: {unlockedItems}.",
                this);
        }

        private Button[] GetItemButtons(
            CosmeticCategory category)
        {
            return category == CosmeticCategory.Skin
                ? skinItemButtons
                : colorItemButtons;
        }

        private void RegisterItemListeners()
        {
            UnregisterItemListeners();
            skinItemActions =
                CreateItemListeners(
                    skinItemButtons,
                    CosmeticCategory.Skin);
            colorItemActions =
                CreateItemListeners(
                    colorItemButtons,
                    CosmeticCategory.Color);
        }

        private UnityAction[] CreateItemListeners(
            Button[] buttons,
            CosmeticCategory category)
        {
            if (buttons == null)
            {
                return null;
            }

            UnityAction[] actions =
                new UnityAction[buttons.Length];

            for (int index = 0;
                 index < buttons.Length;
                 index++)
            {
                int capturedIndex = index;
                UnityAction action = () =>
                    HandleItemPressed(
                        category,
                        capturedIndex);
                actions[index] = action;
                buttons[index]?.onClick.AddListener(action);
            }

            return actions;
        }

        private void UnregisterItemListeners()
        {
            RemoveItemListeners(
                skinItemButtons,
                skinItemActions);
            RemoveItemListeners(
                colorItemButtons,
                colorItemActions);
            skinItemActions = null;
            colorItemActions = null;
        }

        private void RemoveItemListeners(
            Button[] buttons,
            UnityAction[] actions)
        {
            if (buttons == null || actions == null)
            {
                return;
            }

            int itemCount = Mathf.Min(
                buttons.Length,
                actions.Length);

            for (int index = 0;
                 index < itemCount;
                 index++)
            {
                if (buttons[index] != null &&
                    actions[index] != null)
                {
                    buttons[index].onClick.RemoveListener(
                        actions[index]);
                }
            }
        }

        private void CacheItemViews()
        {
            skinItemButtons =
                GetDirectChildComponents<Button>(
                    skinsSection);
            colorItemButtons =
                GetDirectChildComponents<Button>(
                    colorSection);
            lockedOverlayItems =
                GetDirectChildObjects(lockedOverlay);
            selectedOverlayItems =
                GetDirectChildObjects(selectedOverlay);
            animationOverlayItems =
                GetDirectChildObjects(animationOverlay);

            DisableOverlayRaycasts(lockedOverlay);
            DisableOverlayRaycasts(selectedOverlay);
            DisableOverlayRaycasts(animationOverlay);
            HideAllAnimationOverlayItems();
        }

        private void DisableOverlayRaycasts(
            GameObject overlayRoot)
        {
            if (overlayRoot == null)
            {
                return;
            }

            Graphic[] graphics =
                overlayRoot.GetComponentsInChildren<Graphic>(
                    true);

            foreach (Graphic graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }

        private T[] GetDirectChildComponents<T>(
            GameObject root)
            where T : Component
        {
            if (root == null)
            {
                return Array.Empty<T>();
            }

            T[] components =
                new T[root.transform.childCount];

            for (int index = 0;
                 index < components.Length;
                 index++)
            {
                components[index] =
                    root.transform
                        .GetChild(index)
                        .GetComponent<T>();
            }

            return components;
        }

        private GameObject[] GetDirectChildObjects(
            GameObject root)
        {
            if (root == null)
            {
                return Array.Empty<GameObject>();
            }

            GameObject[] children =
                new GameObject[root.transform.childCount];

            for (int index = 0;
                 index < children.Length;
                 index++)
            {
                children[index] =
                    root.transform
                        .GetChild(index)
                        .gameObject;
            }

            return children;
        }

        private void FindLocalReferences()
        {
            closeButton ??=
                FindDirectChildComponent<Button>(
                    "CloseButton");
            skinsButton ??=
                FindDirectChildComponent<Button>(
                    "SkinsButton");
            colorsButton ??=
                FindDirectChildComponent<Button>(
                    "ColorsButton");
            skinsSection ??=
                FindDirectChild("SkinsSection");
            colorSection ??=
                FindDirectChild("ColorSection");
            lockedOverlay ??=
                FindDirectChild("LockedOverLay");
            selectedOverlay ??=
                FindDirectChild("SelectedOverLay");
            unlockRandomButton ??=
                FindDirectChildComponent<Button>(
                    "UnlockRandomButton");
            outOfStockButton ??=
                FindDirectChild("OutOfStockButton");
            animationOverlay ??=
                FindDirectChildIgnoreCase(
                    "AnimationOverlay");

            if (priceText == null &&
                unlockRandomButton != null)
            {
                priceText =
                    unlockRandomButton
                        .GetComponentInChildren<TMP_Text>(
                            true);
            }
        }

        private GameObject FindDirectChild(
            string childName)
        {
            Transform child =
                transform.Find(childName);
            return child != null
                ? child.gameObject
                : null;
        }

        private T FindDirectChildComponent<T>(
            string childName)
            where T : Component
        {
            GameObject child =
                FindDirectChild(childName);
            return child != null
                ? child.GetComponent<T>()
                : null;
        }

        private GameObject FindDirectChildIgnoreCase(
            string childName)
        {
            for (int index = 0;
                 index < transform.childCount;
                 index++)
            {
                Transform child =
                    transform.GetChild(index);

                if (string.Equals(
                        child.name,
                        childName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            skinPrice = Mathf.Max(0, skinPrice);
            colorPrice = Mathf.Max(0, colorPrice);
            animationStepCount = Mathf.Max(
                2,
                animationStepCount);
            firstStepDuration = Mathf.Max(
                0.01f,
                firstStepDuration);
            lastStepDuration = Mathf.Max(
                0.01f,
                lastStepDuration);
            resultHoldDuration = Mathf.Max(
                0f,
                resultHoldDuration);
            FindLocalReferences();
        }
    }
}
