// OutZonesGizmo2D.cs
using UnityEngine;

namespace Nonsliep.Core.Viewport
{
    [ExecuteAlways]
    public sealed class OutZonesGizmo2D : MonoBehaviour
    {
        public Color fill = new Color(1f, 0f, 0f, 0.12f);
        public Color outline = new Color(1f, 0f, 0f, 0.9f);

        void OnDrawGizmos()
        {
            foreach (var col in GetComponentsInChildren<BoxCollider2D>(true))
            {
                var t = col.transform;
                var pos = (Vector2)t.position + col.offset;
                var size = col.size;
                var rot = t.rotation;

                // 채우기
                Gizmos.color = fill;
                Matrix4x4 m = Matrix4x4.TRS(new Vector3(pos.x, pos.y, t.position.z), rot, Vector3.one);
                Gizmos.matrix = m;
                Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0f));

                // 외곽선
                Gizmos.color = outline;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0f));
            }
            // 원래 행렬로 되돌리기
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
