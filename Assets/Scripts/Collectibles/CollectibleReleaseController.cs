using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Collectibles
{
    [DisallowMultipleComponent]
    public class CollectibleReleaseController : MonoBehaviour
    {
        [SerializeField] private CollectibleCollector ballCollector;
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private Transform releaseTarget;

        public void ReleaseCollectedItems()
        {
            if (!ValidateReferences())
            {
                return;
            }

            ballCollector.ReleaseAll(
                releaseTarget.position,
                playerConfig.BallReleaseImpulse);
        }

        [ContextMenu("Release Collected Items")]
        private void ReleaseCollectedItemsFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Collectible release can only be tested while the game is playing.",
                    this);
                return;
            }

            ReleaseCollectedItems();
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (ballCollector == null)
            {
                Debug.LogError(
                    $"{nameof(CollectibleReleaseController)} on '{name}' requires a {nameof(CollectibleCollector)} reference.",
                    this);
                isValid = false;
            }

            if (playerConfig == null)
            {
                Debug.LogError(
                    $"{nameof(CollectibleReleaseController)} on '{name}' requires a {nameof(PlayerConfig)} asset.",
                    this);
                isValid = false;
            }

            if (releaseTarget == null)
            {
                Debug.LogError(
                    $"{nameof(CollectibleReleaseController)} on '{name}' requires a release target Transform.",
                    this);
                isValid = false;
            }

            return isValid;
        }

        protected virtual void OnValidate()
        {
            if (ballCollector == null)
            {
                ballCollector = GetComponentInChildren<CollectibleCollector>();
            }
        }
    }
}
