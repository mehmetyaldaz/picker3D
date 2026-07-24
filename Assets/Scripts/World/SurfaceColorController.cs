using UnityEngine;

namespace Picker3D.World
{
    [DisallowMultipleComponent]
    public sealed class SurfaceColorController : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] targetRenderers;

        private MaterialPropertyBlock propertyBlock;

        public void ApplyColor(Color color)
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(SurfaceColorController)} on '{name}' requires at least one Renderer.",
                    this);
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            for (int index = 0; index < targetRenderers.Length; index++)
            {
                Renderer targetRenderer = targetRenderers[index];

                if (targetRenderer == null)
                {
                    Debug.LogWarning(
                        $"{nameof(SurfaceColorController)} on '{name}' has a missing Renderer at index {index}.",
                        this);
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnValidate()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(SurfaceColorController)} on '{name}' has no target Renderers.",
                    this);
            }
        }
    }
}
