using UnityEngine;
using UnityEngine.InputSystem;

namespace Picker3D.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerDragInput : MonoBehaviour
    {
        private const float FreshGestureMovementThresholdPixels = 1f;

        private enum PointerSource
        {
            None,
            Mouse,
            Touch,
            Pen
        }

        private float accumulatedHorizontalDrag;
        private Vector2 previousPointerPosition;
        private bool isTrackingPointer;
        private PointerSource activePointerSource;
        private float gestureTotalNormalizedDrag;
        private int gestureMovementFrames;
        private bool waitingForPointerRelease;
        private Vector2 waitingPointerPosition;
        private PointerSource waitingPointerSource;

        private void Update()
        {
            if (waitingForPointerRelease)
            {
                if (!TryGetPressedPointerPosition(
                        out Vector2 waitingCurrentPosition,
                        out PointerSource waitingCurrentSource))
                {
                    waitingForPointerRelease = false;
                    waitingPointerPosition = default;
                    waitingPointerSource = PointerSource.None;
                    Debug.Log(
                        "[MovementDebug][DragInput] Stale pointer released; fresh drag input is ready.",
                        this);
                    return;
                }

                bool pointerSourceChanged =
                    waitingCurrentSource != waitingPointerSource;
                bool pointerMoved =
                    (waitingCurrentPosition -
                     waitingPointerPosition).sqrMagnitude >=
                    FreshGestureMovementThresholdPixels *
                    FreshGestureMovementThresholdPixels;

                if (!pointerSourceChanged && !pointerMoved)
                {
                    return;
                }

                waitingForPointerRelease = false;
                waitingPointerPosition = default;
                waitingPointerSource = PointerSource.None;
                previousPointerPosition = waitingCurrentPosition;
                isTrackingPointer = true;
                activePointerSource = waitingCurrentSource;
                gestureTotalNormalizedDrag = 0f;
                gestureMovementFrames = 0;
                Debug.Log(
                    $"[MovementDebug][DragInput] Stale pointer recovery accepted a fresh gesture | source={waitingCurrentSource} position={waitingCurrentPosition} {GetPressedDeviceState()}",
                    this);
                return;
            }

            if (!TryGetPressedPointerPosition(
                    out Vector2 pointerPosition,
                    out PointerSource pointerSource))
            {
                if (isTrackingPointer)
                {
                    Debug.Log(
                        $"[MovementDebug][DragInput] Gesture ended | source={activePointerSource} movementFrames={gestureMovementFrames} totalNormalizedDrag={gestureTotalNormalizedDrag:F4} {GetPressedDeviceState()}",
                        this);
                }

                isTrackingPointer = false;
                activePointerSource = PointerSource.None;
                gestureTotalNormalizedDrag = 0f;
                gestureMovementFrames = 0;
                return;
            }

            if (!isTrackingPointer)
            {
                previousPointerPosition = pointerPosition;
                isTrackingPointer = true;
                activePointerSource = pointerSource;
                gestureTotalNormalizedDrag = 0f;
                gestureMovementFrames = 0;
                Debug.Log(
                    $"[MovementDebug][DragInput] Gesture started | source={pointerSource} position={pointerPosition} {GetPressedDeviceState()}",
                    this);
                return;
            }

            if (activePointerSource != pointerSource)
            {
                Debug.LogWarning(
                    $"[MovementDebug][DragInput] Pointer source changed during gesture | {activePointerSource} -> {pointerSource} previousPosition={previousPointerPosition} currentPosition={pointerPosition} {GetPressedDeviceState()}",
                    this);
                activePointerSource = pointerSource;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            float normalizedDelta =
                (pointerPosition.x - previousPointerPosition.x) /
                screenWidth;
            accumulatedHorizontalDrag += normalizedDelta;
            gestureTotalNormalizedDrag += normalizedDelta;

            if (!Mathf.Approximately(
                    normalizedDelta,
                    0f))
            {
                gestureMovementFrames++;
            }

            previousPointerPosition = pointerPosition;
        }

        private bool TryGetPressedPointerPosition(
            out Vector2 pointerPosition,
            out PointerSource pointerSource)
        {
            Mouse mouse = Mouse.current;

            if (mouse != null &&
                mouse.leftButton.isPressed)
            {
                pointerPosition =
                    mouse.position.ReadValue();
                pointerSource = PointerSource.Mouse;
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null &&
                touchscreen.primaryTouch.press.isPressed)
            {
                pointerPosition =
                    touchscreen.primaryTouch.position.ReadValue();
                pointerSource = PointerSource.Touch;
                return true;
            }

            Pen pen = Pen.current;

            if (pen != null && pen.tip.isPressed)
            {
                pointerPosition = pen.position.ReadValue();
                pointerSource = PointerSource.Pen;
                return true;
            }

            pointerPosition = default;
            pointerSource = PointerSource.None;
            return false;
        }

        public float ConsumeHorizontalDrag()
        {
            float horizontalDrag = accumulatedHorizontalDrag;
            accumulatedHorizontalDrag = 0f;
            return horizontalDrag;
        }

        public void Clear()
        {
            if (isTrackingPointer ||
                !Mathf.Approximately(
                    accumulatedHorizontalDrag,
                    0f))
            {
                Debug.Log(
                    $"[MovementDebug][DragInput] Clear called | {GetDebugState()}",
                    this);
            }

            ResetDrag();
        }

        public void RequireFreshPointerPress()
        {
            ResetDrag();
            waitingForPointerRelease =
                TryGetPressedPointerPosition(
                    out waitingPointerPosition,
                    out waitingPointerSource);

            Debug.Log(
                $"[MovementDebug][DragInput] Fresh pointer required | waitingForRelease={waitingForPointerRelease} {GetPressedDeviceState()}",
                this);
        }

        private void OnEnable()
        {
            Debug.Log(
                $"[MovementDebug][DragInput] Enabled | activeInHierarchy={gameObject.activeInHierarchy} screen={Screen.width}x{Screen.height}",
                this);
        }

        private void OnDisable()
        {
            Debug.LogWarning(
                $"[MovementDebug][DragInput] Disabled | {GetDebugState()}",
                this);
            waitingForPointerRelease = false;
            waitingPointerPosition = default;
            waitingPointerSource = PointerSource.None;
            ResetDrag();
        }

        public string GetDebugState()
        {
            return
                $"componentEnabled={enabled} activeInHierarchy={gameObject.activeInHierarchy} waitingForRelease={waitingForPointerRelease} tracking={isTrackingPointer} source={activePointerSource} accumulatedDrag={accumulatedHorizontalDrag:F4} {GetPressedDeviceState()}";
        }

        private string GetPressedDeviceState()
        {
            bool mousePressed =
                Mouse.current != null &&
                Mouse.current.leftButton.isPressed;
            bool touchPressed =
                Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.isPressed;
            bool penPressed =
                Pen.current != null &&
                Pen.current.tip.isPressed;

            return
                $"mousePressed={mousePressed} touchPressed={touchPressed} penPressed={penPressed}";
        }

        private void ResetDrag()
        {
            accumulatedHorizontalDrag = 0f;
            previousPointerPosition = default;
            isTrackingPointer = false;
            activePointerSource = PointerSource.None;
            gestureTotalNormalizedDrag = 0f;
            gestureMovementFrames = 0;
        }
    }
}
