using System;
using Picker3D.Player;
using UnityEngine;

namespace Picker3D.Level
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RewardZone : MonoBehaviour
    {
        [SerializeField] private string zoneId = "reward_zone";
        [SerializeField, Min(0)] private int gemReward = 200;

        private bool hasBeenReached;

        public event Action<RewardZone> Reached;

        public string ZoneId => zoneId;
        public int GemReward => gemReward;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenReached ||
                other.GetComponentInParent<PlayerRampMovement>() == null)
            {
                return;
            }

            hasBeenReached = true;
            Reached?.Invoke(this);
        }

        private void OnValidate()
        {
            zoneId = (zoneId ?? string.Empty).Trim();
            gemReward = Mathf.Max(0, gemReward);

            Collider trigger = GetComponent<Collider>();

            if (trigger != null && !trigger.isTrigger)
            {
                Debug.LogWarning(
                    $"Collider on reward zone '{name}' must have Is Trigger enabled.",
                    this);
            }
        }
    }
}
