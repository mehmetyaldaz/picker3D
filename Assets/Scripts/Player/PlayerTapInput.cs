using UnityEngine;
using UnityEngine.InputSystem;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerTapInput : MonoBehaviour
    {
        private int pendingTapCount;
        private bool wasPointerPressed;

        private void Update()
        {
            bool isPointerPressed = IsPointerPressed();

            if (isPointerPressed && !wasPointerPressed)
            {
                pendingTapCount++;
            }

            wasPointerPressed = isPointerPressed;
        }

        private bool IsPointerPressed()
        {
            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.leftButton.isPressed)
            {
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null &&
                touchscreen.primaryTouch.press.isPressed)
            {
                return true;
            }

            Pen pen = Pen.current;
            return pen != null && pen.tip.isPressed;
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
            wasPointerPressed = false;
        }
    }
}
