using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerTapInput : MonoBehaviour
    {
        private int pendingTapCount;
        private int lastTouchTapFrame = -1;

        private void OnEnable()
        {
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }

            EnhancedTouch.onFingerDown += HandleFingerDown;
        }

        private void Update()
        {
            if (lastTouchTapFrame == Time.frameCount)
            {
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.leftButton.wasPressedThisFrame)
            {
                pendingTapCount++;
                return;
            }

            Pen pen = Pen.current;

            if (pen != null && pen.tip.wasPressedThisFrame)
            {
                pendingTapCount++;
            }
        }

        private void HandleFingerDown(Finger finger)
        {
            pendingTapCount++;
            lastTouchTapFrame = Time.frameCount;
        }

        public int ConsumeTapCount()
        {
            int tapCount = pendingTapCount;
            pendingTapCount = 0;
            return tapCount;
        }

        public void Clear()
        {
            pendingTapCount = 0;
        }

        private void OnDisable()
        {
            EnhancedTouch.onFingerDown -= HandleFingerDown;
            Clear();
            lastTouchTapFrame = -1;
        }
    }
}
