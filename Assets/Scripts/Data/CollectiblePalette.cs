using System;
using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "CollectiblePalette",
        menuName = "Picker 3D/Collectible Color Palette")]
    public sealed class CollectiblePalette : ScriptableObject
    {
        [SerializeField] private Color[] colors =
        {
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.magenta
        };

        public int ColorCount => colors != null ? colors.Length : 0;

        public Color GetRandomColor(System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (colors == null || colors.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(CollectiblePalette)} '{name}' has no colors.",
                    this);
                return Color.white;
            }

            return colors[random.Next(colors.Length)];
        }

        private void OnValidate()
        {
            if (colors == null || colors.Length == 0)
            {
                Debug.LogWarning(
                    $"{nameof(CollectiblePalette)} '{name}' requires at least one color.",
                    this);
            }
        }
    }
}
