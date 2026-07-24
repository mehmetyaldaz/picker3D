using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class SpinnerPowerUpPickup : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0f)] private float rotationSpeed = 90f;
        [SerializeField, Min(0f)] private float bobHeight = 0.15f;
        [SerializeField, Min(0f)] private float bobSpeed = 2f;

        private Vector3 visualStartLocalPosition;
        private bool isConsumed;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            visualStartLocalPosition = visualRoot.localPosition;
        }

        private void Update()
        {
            if (isConsumed || visualRoot == null)
            {
                return;
            }

            visualRoot.Rotate(
                Vector3.up,
                rotationSpeed * Time.deltaTime,
                Space.Self);
            Vector3 localPosition = visualStartLocalPosition;
            localPosition.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualRoot.localPosition = localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isConsumed)
            {
                return;
            }

            PlayerSpinnerPowerUp spinnerPowerUp =
                other.GetComponentInParent<PlayerSpinnerPowerUp>();

            if (spinnerPowerUp == null || !spinnerPowerUp.Activate())
            {
                return;
            }

            isConsumed = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void Reset()
        {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
            visualRoot = transform;
        }

        private void OnValidate()
        {
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            bobHeight = Mathf.Max(0f, bobHeight);
            bobSpeed = Mathf.Max(0f, bobSpeed);

            Collider pickupCollider = GetComponent<Collider>();

            if (pickupCollider != null && !pickupCollider.isTrigger)
            {
                Debug.LogWarning(
                    $"Collider on spinner pickup '{name}' must be Is Trigger.",
                    this);
            }
        }
    }
}
