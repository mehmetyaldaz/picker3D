using UnityEngine;
using UnityEngine.InputSystem;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerDragInput : MonoBehaviour
    {
        private float accumulatedHorizontalDrag;
        private Vector2 previousPointerPosition;
        private Pointer activePointer;

        private void Update()
        {
            if (activePointer == null)
            {
                activePointer = FindPressedPointer();

                if (activePointer == null)
                {
                    return;
                }

                previousPointerPosition =
                    activePointer.position.ReadValue();
                return;
            }

            if (!activePointer.added ||
                !activePointer.press.isPressed)
            {
                activePointer = null;
                return;
            }

            Vector2 currentPosition =
                activePointer.position.ReadValue();

            float screenWidth = Mathf.Max(1f, Screen.width);
            accumulatedHorizontalDrag +=
                (currentPosition.x - previousPointerPosition.x) / screenWidth;
            previousPointerPosition = currentPosition;
        }

        private Pointer FindPressedPointer()
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (IsPressed(touchscreen))
            {
                return touchscreen;
            }

            Mouse mouse = Mouse.current;

            if (IsPressed(mouse))
            {
                return mouse;
            }

            Pen pen = Pen.current;

            if (IsPressed(pen))
            {
                return pen;
            }

            Pointer currentPointer = Pointer.current;
            return IsPressed(currentPointer)
                ? currentPointer
                : null;
        }

        private bool IsPressed(Pointer pointer)
        {
            return pointer != null &&
                   pointer.added &&
                   pointer.press.isPressed;
        }

        public float ConsumeHorizontalDrag()
        {
            float horizontalDrag = accumulatedHorizontalDrag;
            accumulatedHorizontalDrag = 0f;
            return horizontalDrag;
        }

        public void Clear()
        {
            ResetDrag();
        }

        private void OnDisable()
        {
            ResetDrag();
        }

        private void ResetDrag()
        {
            accumulatedHorizontalDrag = 0f;
            activePointer = null;
            previousPointerPosition = default;
        }
    }
}
