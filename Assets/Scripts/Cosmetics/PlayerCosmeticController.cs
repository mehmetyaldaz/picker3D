using UnityEngine;

namespace Picker3D.Cosmetics
{
    [DisallowMultipleComponent]
    public sealed class PlayerCosmeticController : MonoBehaviour
    {
        private const string UnlockKeyPrefix =
            "picker3d_cosmetic_unlocked_";
        private const string SelectedCosmeticKey =
            "picker3d_selected_cosmetic";

        [Header("Player Renderers")]
        [SerializeField] private MeshRenderer leftRenderer;
        [SerializeField] private MeshRenderer rightRenderer;
        [SerializeField] private MeshRenderer backRenderer;

        [Header("Store Materials")]
        [SerializeField] private Material[] skinMaterials =
            new Material[9];
        [SerializeField] private Material[] colorMaterials =
            new Material[9];

        private Material[] originalLeftMaterials;
        private Material[] originalRightMaterials;
        private Material[] originalBackMaterials;
        private string selectedCosmeticId;
        private bool originalsCaptured;

        private void Awake()
        {
            FindRendererReferences();
            CaptureOriginalMaterials();
            RestoreSavedSelection();
        }

        public int GetItemCount(CosmeticCategory category)
        {
            Material[] materials = GetMaterials(category);
            return materials != null ? materials.Length : 0;
        }

        public bool IsUnlocked(
            CosmeticCategory category,
            int itemIndex)
        {
            return IsValidItem(category, itemIndex) &&
                   PlayerPrefs.GetInt(
                       GetUnlockKey(category, itemIndex),
                       0) == 1;
        }

        public bool IsSelected(
            CosmeticCategory category,
            int itemIndex)
        {
            return IsValidItem(category, itemIndex) &&
                   selectedCosmeticId ==
                   GetCosmeticId(category, itemIndex);
        }

        public bool TryGetRandomLockedIndex(
            CosmeticCategory category,
            out int itemIndex)
        {
            itemIndex = -1;
            int itemCount = GetItemCount(category);
            int lockedCount = 0;

            for (int index = 0; index < itemCount; index++)
            {
                if (IsValidItem(category, index) &&
                    !IsUnlocked(category, index))
                {
                    lockedCount++;
                }
            }

            if (lockedCount == 0)
            {
                return false;
            }

            int selectedLockedIndex =
                Random.Range(0, lockedCount);

            for (int index = 0; index < itemCount; index++)
            {
                if (!IsValidItem(category, index) ||
                    IsUnlocked(category, index))
                {
                    continue;
                }

                if (selectedLockedIndex == 0)
                {
                    itemIndex = index;
                    return true;
                }

                selectedLockedIndex--;
            }

            return false;
        }

        public bool Unlock(
            CosmeticCategory category,
            int itemIndex)
        {
            if (!IsValidItem(category, itemIndex) ||
                IsUnlocked(category, itemIndex))
            {
                return false;
            }

            PlayerPrefs.SetInt(
                GetUnlockKey(category, itemIndex),
                1);
            PlayerPrefs.Save();
            return true;
        }

        public bool ToggleSelection(
            CosmeticCategory category,
            int itemIndex)
        {
            if (!IsUnlocked(category, itemIndex))
            {
                Debug.LogWarning(
                    $"Cannot select {category} {itemIndex + 1}: it is locked or its material is missing.",
                    this);
                return false;
            }

            string cosmeticId =
                GetCosmeticId(category, itemIndex);

            if (selectedCosmeticId == cosmeticId)
            {
                ClearSelection();
                Debug.Log(
                    $"Deselected {category} {itemIndex + 1}; restored the player's original materials.",
                    this);
                return true;
            }

            Material material =
                GetMaterials(category)[itemIndex];

            if (material == null)
            {
                return false;
            }

            ApplyMaterial(material);
            selectedCosmeticId = cosmeticId;
            PlayerPrefs.SetString(
                SelectedCosmeticKey,
                selectedCosmeticId);
            PlayerPrefs.Save();
            Debug.Log(
                $"Selected {category} {itemIndex + 1} and applied '{material.name}' to the player.",
                this);
            return true;
        }

        private void ClearSelection()
        {
            RestoreOriginalMaterials();
            selectedCosmeticId = string.Empty;
            PlayerPrefs.DeleteKey(SelectedCosmeticKey);
            PlayerPrefs.Save();
        }

        private void RestoreSavedSelection()
        {
            selectedCosmeticId = PlayerPrefs.GetString(
                SelectedCosmeticKey,
                string.Empty);

            if (string.IsNullOrEmpty(selectedCosmeticId))
            {
                RestoreOriginalMaterials();
                return;
            }

            if (!TryFindItem(
                    selectedCosmeticId,
                    out CosmeticCategory category,
                    out int itemIndex) ||
                !IsUnlocked(category, itemIndex))
            {
                ClearSelection();
                return;
            }

            Material material =
                GetMaterials(category)[itemIndex];

            if (material == null)
            {
                ClearSelection();
                return;
            }

            ApplyMaterial(material);
        }

        private bool TryFindItem(
            string cosmeticId,
            out CosmeticCategory category,
            out int itemIndex)
        {
            foreach (CosmeticCategory candidateCategory
                     in System.Enum.GetValues(
                         typeof(CosmeticCategory)))
            {
                int itemCount =
                    GetItemCount(candidateCategory);

                for (int index = 0;
                     index < itemCount;
                     index++)
                {
                    if (GetCosmeticId(
                            candidateCategory,
                            index) == cosmeticId)
                    {
                        category = candidateCategory;
                        itemIndex = index;
                        return true;
                    }
                }
            }

            category = default;
            itemIndex = -1;
            return false;
        }

        private bool IsValidItem(
            CosmeticCategory category,
            int itemIndex)
        {
            Material[] materials = GetMaterials(category);
            return materials != null &&
                   itemIndex >= 0 &&
                   itemIndex < materials.Length &&
                   materials[itemIndex] != null;
        }

        private Material[] GetMaterials(
            CosmeticCategory category)
        {
            return category == CosmeticCategory.Skin
                ? skinMaterials
                : colorMaterials;
        }

        private string GetUnlockKey(
            CosmeticCategory category,
            int itemIndex)
        {
            return UnlockKeyPrefix +
                   GetCosmeticId(category, itemIndex);
        }

        private string GetCosmeticId(
            CosmeticCategory category,
            int itemIndex)
        {
            string categoryName =
                category == CosmeticCategory.Skin
                    ? "skin"
                    : "color";
            return $"{categoryName}_{itemIndex + 1:00}";
        }

        private void ApplyMaterial(Material material)
        {
            CaptureOriginalMaterials();
            ApplyMaterial(leftRenderer, material);
            ApplyMaterial(rightRenderer, material);
            ApplyMaterial(backRenderer, material);
        }

        private void ApplyMaterial(
            MeshRenderer targetRenderer,
            Material material)
        {
            if (targetRenderer == null ||
                material == null)
            {
                return;
            }

            Material[] currentMaterials =
                targetRenderer.sharedMaterials;

            for (int index = 0;
                 index < currentMaterials.Length;
                 index++)
            {
                currentMaterials[index] = material;
            }

            targetRenderer.sharedMaterials =
                currentMaterials;
        }

        private void CaptureOriginalMaterials()
        {
            if (originalsCaptured)
            {
                return;
            }

            FindRendererReferences();

            if (leftRenderer == null ||
                rightRenderer == null ||
                backRenderer == null)
            {
                return;
            }

            originalLeftMaterials =
                leftRenderer.sharedMaterials;
            originalRightMaterials =
                rightRenderer.sharedMaterials;
            originalBackMaterials =
                backRenderer.sharedMaterials;
            originalsCaptured = true;
        }

        private void RestoreOriginalMaterials()
        {
            CaptureOriginalMaterials();

            if (!originalsCaptured)
            {
                return;
            }

            leftRenderer.sharedMaterials =
                originalLeftMaterials;
            rightRenderer.sharedMaterials =
                originalRightMaterials;
            backRenderer.sharedMaterials =
                originalBackMaterials;
        }

        private void FindRendererReferences()
        {
            if (leftRenderer == null)
            {
                leftRenderer =
                    FindChildRenderer("Left");
            }

            if (rightRenderer == null)
            {
                rightRenderer =
                    FindChildRenderer("Right");
            }

            if (backRenderer == null)
            {
                backRenderer =
                    FindChildRenderer("Back");
            }
        }

        private MeshRenderer FindChildRenderer(
            string childName)
        {
            Transform child = transform.Find(childName);
            return child != null
                ? child.GetComponent<MeshRenderer>()
                : null;
        }

        private void OnValidate()
        {
            FindRendererReferences();

            if (skinMaterials == null ||
                skinMaterials.Length != 9 ||
                colorMaterials == null ||
                colorMaterials.Length != 9)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerCosmeticController)} on '{name}' expects exactly 9 skin and 9 color materials.",
                    this);
            }
        }
    }
}
