using UnityEngine;

namespace Picker3D.Player
{
    [CreateAssetMenu(
        fileName = "PlayerConfig",
        menuName = "Picker 3D/Player Configuration")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Automatic Movement")]
        [SerializeField, Min(0f)] private float forwardSpeed = 4f;

        [Header("Horizontal Drag")]
        [Tooltip("World-space distance produced by dragging across the full screen width.")]
        [SerializeField, Min(0f)] private float horizontalSensitivity = 8f;

        [Tooltip("Maximum horizontal movement speed in world units per second.")]
        [SerializeField, Min(0f)] private float maximumHorizontalSpeed = 12f;

        [Header("Ball Release")]
        [Tooltip("Forward speed added to each collectible when the picker releases it.")]
        [SerializeField, Min(0f)] private float ballReleaseImpulse = 3.5f;

        public float ForwardSpeed => forwardSpeed;
        public float HorizontalSensitivity => horizontalSensitivity;
        public float MaximumHorizontalSpeed => maximumHorizontalSpeed;
        public float BallReleaseImpulse => ballReleaseImpulse;

        private void OnValidate()
        {
            forwardSpeed = Mathf.Max(0f, forwardSpeed);
            horizontalSensitivity = Mathf.Max(0f, horizontalSensitivity);
            maximumHorizontalSpeed = Mathf.Max(0f, maximumHorizontalSpeed);
            ballReleaseImpulse = Mathf.Max(0f, ballReleaseImpulse);
        }
    }
}
