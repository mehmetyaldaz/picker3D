using UnityEngine;
using UnityEngine.InputSystem;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerDragInput : MonoBehaviour
    {
        private float accumulatedHorizontalDrag;

        private void Update()
        {
            float horizontalDelta = ReadPressedPointerDeltaX();

            if (Mathf.Approximately(horizontalDelta, 0f))
            {
                return;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            accumulatedHorizontalDrag +=
                horizontalDelta / screenWidth;
        }

        private float ReadPressedPointerDeltaX()
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null)
            {
                float movingTouchDelta = 0f;

                for (int index = 0;
                     index < touchscreen.touches.Count;
                     index++)
                {
                    var touch = touchscreen.touches[index];

                    if (!touch.press.isPressed)
                    {
                        continue;
                    }

                    float touchDelta =
                        touch.delta.ReadValue().x;

                    if (Mathf.Abs(touchDelta) >
                        Mathf.Abs(movingTouchDelta))
                    {
                        movingTouchDelta = touchDelta;
                    }
                }

                if (!Mathf.Approximately(
                        movingTouchDelta,
                        0f))
                {
                    return movingTouchDelta;
                }
            }

            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.leftButton.isPressed)
            {
                return mouse.delta.ReadValue().x;
            }

            Pen pen = Pen.current;

            if (pen != null && pen.tip.isPressed)
            {
                return pen.delta.ReadValue().x;
            }

            return 0f;
        }

        public float ConsumeHorizontalDrag()
        {
            float horizontalDrag = accumulatedHorizontalDrag;
            accumulatedHorizontalDrag = 0f;
            return horizontalDrag;
        }

        public void Clear()
        {
            accumulatedHorizontalDrag = 0f;
        }

        private void OnDisable()
        {
            Clear();
        }

        public string GetDebugState()
        {
            bool touchPressed =
                Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.isPressed;
            bool mousePressed =
                Mouse.current != null &&
                Mouse.current.leftButton.isPressed;
            bool penPressed =
                Pen.current != null &&
                Pen.current.tip.isPressed;

            return
                $"componentEnabled={enabled} activeInHierarchy={gameObject.activeInHierarchy} touchPressed={touchPressed} mousePressed={mousePressed} penPressed={penPressed} accumulatedDrag={accumulatedHorizontalDrag:F4}";
        }
    }
}
