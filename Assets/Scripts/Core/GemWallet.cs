using System;
using UnityEngine;

namespace Picker3D.Core
{
    [DisallowMultipleComponent]
    public sealed class GemWallet : MonoBehaviour
    {
        private const string DefaultPlayerPrefsKey =
            "picker3d_total_gems";

        [SerializeField] private string playerPrefsKey =
            DefaultPlayerPrefsKey;
        [SerializeField, Min(0)] private int startingGems;

        public event Action<int> BalanceChanged;
        public event Action<int, int, int> GemsAdded;

        public int TotalGems { get; private set; }

        private void Awake()
        {
            string key = GetSafePlayerPrefsKey();
            TotalGems = Mathf.Max(
                0,
                PlayerPrefs.GetInt(key, startingGems));
        }

        public void AddGems(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int previousBalance = TotalGems;
            long nextBalance = (long)TotalGems + amount;
            TotalGems = (int)Math.Min(
                nextBalance,
                int.MaxValue);
            int addedAmount = TotalGems - previousBalance;

            if (addedAmount <= 0)
            {
                return;
            }

            Save();
            GemsAdded?.Invoke(
                previousBalance,
                addedAmount,
                TotalGems);
            BalanceChanged?.Invoke(TotalGems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0 || TotalGems < amount)
            {
                return false;
            }

            TotalGems -= amount;
            Save();
            BalanceChanged?.Invoke(TotalGems);
            return true;
        }

        [ContextMenu("Reset Gems")]
        private void ResetGems()
        {
            TotalGems = Mathf.Max(0, startingGems);
            Save();
            BalanceChanged?.Invoke(TotalGems);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(
                GetSafePlayerPrefsKey(),
                TotalGems);
            PlayerPrefs.Save();
        }

        private string GetSafePlayerPrefsKey()
        {
            string trimmedKey =
                (playerPrefsKey ?? string.Empty).Trim();

            return string.IsNullOrEmpty(trimmedKey)
                ? DefaultPlayerPrefsKey
                : trimmedKey;
        }

        private void OnValidate()
        {
            startingGems = Mathf.Max(0, startingGems);
        }
    }
}
