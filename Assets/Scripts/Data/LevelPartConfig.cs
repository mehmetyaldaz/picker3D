using UnityEngine;

namespace Picker3D.Data
{
    [CreateAssetMenu(
        fileName = "LevelPartConfig",
        menuName = "Picker 3D/Level Part Configuration")]
    public sealed class LevelPartConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int requiredBallCount = 3;
        [SerializeField, Min(0f)] private float dropSettleDuration = 2f;

        [Header("Success Transition")]
        [SerializeField, Min(0f)] private float dropboxRaiseDuration = 1f;
        [SerializeField, Min(0f)] private float bridgeExtendDuration = 0.5f;
        [SerializeField, Min(0f)] private float gateOpenDuration = 0.75f;
        [SerializeField] private AnimationCurve transitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Color bridgeColor = Color.green;

        public int RequiredBallCount => requiredBallCount;
        public float DropSettleDuration => dropSettleDuration;
        public float DropboxRaiseDuration => dropboxRaiseDuration;
        public float BridgeExtendDuration => bridgeExtendDuration;
        public float GateOpenDuration => gateOpenDuration;
        public AnimationCurve TransitionCurve => transitionCurve;
        public Color BridgeColor => bridgeColor;

        private void OnValidate()
        {
            requiredBallCount = Mathf.Max(1, requiredBallCount);
            dropSettleDuration = Mathf.Max(0f, dropSettleDuration);
            dropboxRaiseDuration = Mathf.Max(0f, dropboxRaiseDuration);
            bridgeExtendDuration = Mathf.Max(0f, bridgeExtendDuration);
            gateOpenDuration = Mathf.Max(0f, gateOpenDuration);

            if (transitionCurve == null || transitionCurve.length == 0)
            {
                transitionCurve = AnimationCurve.EaseInOut(
                    0f,
                    0f,
                    1f,
                    1f);
            }
        }
    }
}
