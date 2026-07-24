using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "FinalRampConfig",
        menuName = "Picker 3D/Final Ramp Configuration")]
    public sealed class FinalRampConfig : ScriptableObject
    {
        [Header("Ramp Movement")]
        [SerializeField, Min(0.01f)] private float initialRampSpeed = 4f;
        [SerializeField, Min(0.01f)] private float speedAddedPerTap = 0.75f;
        [SerializeField, Min(0.01f)] private float maximumRampSpeed = 14f;
        [SerializeField, Range(-180f, 180f)] private float playerRampZAngle = -35f;
        [SerializeField, Min(0f)] private float playerRampVerticalOffset = 0.02f;

        [Header("Gate")]
        [SerializeField, Min(0f)] private float gateOpenDuration = 0.6f;
        [SerializeField] private AnimationCurve transitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Flight")]
        [SerializeField, Min(0f)] private float minimumLaunchSpeed = 5f;
        [SerializeField, Min(0f)] private float maximumLaunchSpeed = 12f;
        [SerializeField, Min(0f)] private float upwardLaunchSpeed = 4f;
        [SerializeField, Min(0f)] private float spinSpeed = 6f;
        [SerializeField, Min(0.1f)] private float rewardSettleDuration = 0.75f;
        [SerializeField, Min(0.1f)] private float maximumFlightDuration = 6f;

        public float InitialRampSpeed => initialRampSpeed;
        public float SpeedAddedPerTap => speedAddedPerTap;
        public float MaximumRampSpeed => maximumRampSpeed;
        public float PlayerRampZAngle => playerRampZAngle;
        public float PlayerRampVerticalOffset => playerRampVerticalOffset;
        public float GateOpenDuration => gateOpenDuration;
        public AnimationCurve TransitionCurve => transitionCurve;
        public float MinimumLaunchSpeed => minimumLaunchSpeed;
        public float MaximumLaunchSpeed => maximumLaunchSpeed;
        public float UpwardLaunchSpeed => upwardLaunchSpeed;
        public float SpinSpeed => spinSpeed;
        public float RewardSettleDuration => rewardSettleDuration;
        public float MaximumFlightDuration => maximumFlightDuration;
        private void OnValidate()
        {
            initialRampSpeed = Mathf.Max(0.01f, initialRampSpeed);
            speedAddedPerTap = Mathf.Max(0.01f, speedAddedPerTap);
            maximumRampSpeed = Mathf.Max(initialRampSpeed, maximumRampSpeed);
            playerRampZAngle = Mathf.Clamp(playerRampZAngle, -180f, 180f);
            playerRampVerticalOffset = Mathf.Max(
                0f,
                playerRampVerticalOffset);
            gateOpenDuration = Mathf.Max(0f, gateOpenDuration);
            minimumLaunchSpeed = Mathf.Max(0f, minimumLaunchSpeed);
            maximumLaunchSpeed = Mathf.Max(
                minimumLaunchSpeed,
                maximumLaunchSpeed);
            upwardLaunchSpeed = Mathf.Max(0f, upwardLaunchSpeed);
            spinSpeed = Mathf.Max(0f, spinSpeed);
            rewardSettleDuration = Mathf.Max(0.1f, rewardSettleDuration);
            maximumFlightDuration = Mathf.Max(0.1f, maximumFlightDuration);
        }
    }
}
