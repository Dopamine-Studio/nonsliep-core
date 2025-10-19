using UnityEngine;

namespace Nonsliep.Core.Viewport
{
    [ExecuteAlways]
    public class ViewportBoundsBuilder : MonoBehaviour
    {
        public ViewportLayoutSystem layout;
        public Transform leftWall, rightWall, bottomWall; // BoxCollider2D 포함
        public float thickness = 1f;
        public float horizontalOffset = 0f;
        public float bottomVerticalOffset = 0f;

        void OnEnable() { if (layout) layout.OnViewportChanged += Apply; }
        void OnDisable() { if (layout) layout.OnViewportChanged -= Apply; }

        void Apply(ViewportLayoutSystem.ViewportInfo v)
        {
            float midY = v.BL.y + v.Height * 0.5f;
            float horizontal = horizontalOffset;
            if (leftWall)
            {
                leftWall.position = new Vector3(v.BL.x - thickness * 0.5f - horizontal, midY, leftWall.position.z);
                var c = leftWall.GetComponent<BoxCollider2D>(); if (c) c.size = new Vector2(thickness, v.Height + thickness * 2f);
            }
            if (rightWall)
            {
                rightWall.position = new Vector3(v.BR.x + thickness * 0.5f + horizontal, midY, rightWall.position.z);
                var c = rightWall.GetComponent<BoxCollider2D>(); if (c) c.size = new Vector2(thickness, v.Height + thickness * 2f);
            }
            if (bottomWall)
            {
                float bottomWidth = Mathf.Max(thickness, v.Width + thickness * 2f + horizontal * 2f);
                bottomWall.position = new Vector3(v.BL.x + v.Width * 0.5f, v.BL.y - thickness * 0.5f - bottomVerticalOffset, bottomWall.position.z);
                var c = bottomWall.GetComponent<BoxCollider2D>(); if (c) c.size = new Vector2(bottomWidth, thickness);
            }
        }
    }
}