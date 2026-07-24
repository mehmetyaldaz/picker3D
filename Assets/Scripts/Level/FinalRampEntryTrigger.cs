using System;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FinalRampEntryTrigger : MonoBehaviour
    {
        public event Action<PlayerRampMovement> PlayerEntered;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerRampMovement rampMovement =
                other.GetComponentInParent<PlayerRampMovement>();

            if (rampMovement != null)
            {
                PlayerEntered?.Invoke(rampMovement);
            }
        }

        private void OnValidate()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null && !trigger.isTrigger)
            {
                Debug.LogWarning(
                    $"Collider on '{name}' must have Is Trigger enabled.",
                    this);
            }
        }
    }
}
