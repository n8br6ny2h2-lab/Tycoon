using UnityEngine;

namespace FactoryKing.Runtime.InputHandling
{
    /// <summary>
    /// Unified touch + mouse input (legacy Input Manager; works on iOS and in the Editor).
    /// Kept behind this small facade so it can be swapped for the Input System package without touching gameplay.
    /// </summary>
    public static class PointerInput
    {
        public static int PointerCount
        {
            get
            {
                if (Input.touchCount > 0) return Input.touchCount;
                return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) ? 1 : 0;
            }
        }

        // touchCount (not touchSupported) so Unity Remote touches from a phone also work inside the Editor.
        public static bool IsTouch => Input.touchCount > 0;

        public static Vector2 Position(int index)
        {
            if (IsTouch) return Input.GetTouch(Mathf.Min(index, Input.touchCount - 1)).position;
            return Input.mousePosition;
        }

        /// <summary>True on the frame the primary pointer went down.</summary>
        public static bool PrimaryDown
        {
            get
            {
                if (IsTouch) return Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began;
                return Input.GetMouseButtonDown(0);
            }
        }

        /// <summary>True on the frame the primary pointer was released.</summary>
        public static bool PrimaryUp
        {
            get
            {
                if (IsTouch)
                {
                    var t = Input.GetTouch(0);
                    return Input.touchCount == 1 && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
                }
                return Input.GetMouseButtonUp(0);
            }
        }

        /// <summary>Mouse wheel / trackpad zoom delta (positive = zoom in).</summary>
        public static float ScrollDelta => Input.mouseScrollDelta.y;
    }
}
