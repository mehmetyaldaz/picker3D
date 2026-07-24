using UnityEngine;
using UnityEngine.InputSystem;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerTapInput : MonoBehaviour
    {
        private int pendingTapCount;

        private void Update()
        {
            bool wasPressedThisFrame = false;

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null &&
                touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                wasPressedThisFrame = true;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.leftButton.wasPressedThisFrame)
            {
                wasPressedThisFrame = true;
            }

            Pen pen = Pen.current;

            if (pen != null &&
                pen.tip.wasPressedThisFrame)
            {
                wasPressedThisFrame = true;
            }

            if (wasPressedThisFrame)
            {
                pendingTapCount++;
            }
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
            Clear();
        }
    }
}
