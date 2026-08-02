using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public sealed class CollectibleAppearance : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] targetRenderers;

        private MaterialPropertyBlock propertyBlock;

        private void Reset()
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void ApplyColor(Color color)
        {
            if (!ValidateRenderers())
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            for (int index = 0; index < targetRenderers.Length; index++)
            {
                Renderer targetRenderer = targetRenderers[index];

                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        private bool ValidateRenderers()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                return true;
            }

            targetRenderers = GetComponentsInChildren<Renderer>(true);

            if (targetRenderers.Length > 0)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(CollectibleAppearance)} on '{name}' requires at least one Renderer.",
                this);
            return false;
        }

        private void OnValidate()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }
    }
}
