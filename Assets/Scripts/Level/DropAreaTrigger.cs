using System;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DropAreaTrigger : MonoBehaviour
    {
        [SerializeField] private Collider playerTrigger;

        private bool hasTriggered;

        public event Action<PlayerMovement> PlayerEntered;

        private void Reset()
        {
            playerTrigger = GetComponent<Collider>();
            playerTrigger.isTrigger = true;
        }

        private void Awake()
        {
            if (playerTrigger == null)
            {
                playerTrigger = GetComponent<Collider>();
            }

            if (playerTrigger == null || !playerTrigger.isTrigger)
            {
                Debug.LogError(
                    $"{nameof(DropAreaTrigger)} on '{name}' requires an Is Trigger collider.",
                    this);
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered)
            {
                return;
            }

            PlayerMovement playerMovement =
                other.GetComponentInParent<PlayerMovement>();

            if (playerMovement == null)
            {
                return;
            }

            hasTriggered = true;
            PlayerEntered?.Invoke(playerMovement);
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        private void OnValidate()
        {
            if (playerTrigger == null)
            {
                playerTrigger = GetComponent<Collider>();
            }

            if (playerTrigger != null && !playerTrigger.isTrigger)
            {
                Debug.LogWarning(
                    $"{nameof(DropAreaTrigger)} collider on '{name}' must have Is Trigger enabled.",
                    this);
            }
        }
    }
}
