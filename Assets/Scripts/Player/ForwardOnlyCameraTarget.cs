using UnityEngine;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class ForwardOnlyCameraTarget : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float forwardOffset;
        [SerializeField] private bool followVerticalMovement = true;
        [SerializeField] private float verticalOffset;

        private float fixedX;
        private float fixedY;
        private float horizontalPlayerOffset;
        private bool followHorizontalDuringLevelTransfer;

        private void Awake()
        {
            fixedX = transform.position.x;
            fixedY = transform.position.y;

            if (player != null)
            {
                horizontalPlayerOffset =
                    transform.position.x - player.position.x;
            }

            SnapToPlayer();
        }

        private void LateUpdate()
        {
            if (followHorizontalDuringLevelTransfer &&
                player != null)
            {
                fixedX =
                    player.position.x + horizontalPlayerOffset;
            }

            SnapToPlayer();
        }

        public void BeginLevelTransfer()
        {
            followHorizontalDuringLevelTransfer = true;
        }

        public void CompleteLevelTransfer()
        {
            followHorizontalDuringLevelTransfer = false;
            RecenterForCurrentPlayer();
        }

        public void RecenterForCurrentPlayer()
        {
            if (player == null)
            {
                return;
            }

            fixedX = player.position.x + horizontalPlayerOffset;
            SnapToPlayer();
        }

        private void SnapToPlayer()
        {
            if (player == null)
            {
                return;
            }

            transform.position = new Vector3(
                fixedX,
                followVerticalMovement
                    ? player.position.y + verticalOffset
                    : fixedY,
                player.position.z + forwardOffset);
        }

        private void OnValidate()
        {
            if (player == null)
            {
                Debug.LogWarning(
                    $"{nameof(ForwardOnlyCameraTarget)} on '{name}' requires a Player transform.",
                    this);
            }
        }
    }
}
