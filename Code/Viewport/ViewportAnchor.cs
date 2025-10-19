using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nonsliep.Core.Viewport
{
    [ExecuteAlways, RequireComponent(typeof(Transform))]
    public class ViewportAnchor : MonoBehaviour
    {
        public ViewportLayoutSystem layout;
        [Range(0, 1)] public float u = 0.5f;
        [Range(0, 1)] public float v = 2f / 3f;
        public Vector2 worldOffset = Vector2.zero;
        public float extraZ = 0f; // 카메라와의 거리 보정
        public Color gizmoColor = Color.yellow;
        public float gizmoSize = 0.15f;

        void OnEnable() { if (layout) layout.OnViewportChanged += Apply; }
        void OnDisable() { if (layout) layout.OnViewportChanged -= Apply; }

        void Apply(ViewportLayoutSystem.ViewportInfo info)
        {
            float zDist = Mathf.Abs(transform.position.z - info.cam.transform.position.z) + extraZ;
            Vector3 p = info.ViewportToWorld(new Vector2(u, v), zDist);
            transform.position = new Vector3(p.x + worldOffset.x, p.y + worldOffset.y, transform.position.z);
        }

        public void ForceApply()
        {
            Camera targetCam = null;
            if (layout)
            {
                targetCam = layout.cam ? layout.cam : Camera.main;
            }
            else
            {
                targetCam = Camera.main;
            }

            if (!targetCam) return;

            float zDist = Mathf.Abs(transform.position.z - targetCam.transform.position.z) + extraZ;
            Vector3 p = targetCam.ViewportToWorldPoint(new Vector3(u, v, zDist));
            transform.position = new Vector3(p.x + worldOffset.x, p.y + worldOffset.y, transform.position.z);
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoSize);

            Handles.color = gizmoColor;
            float crossSize = gizmoSize * 1.5f;
            Handles.DrawLine(transform.position - Vector3.right * crossSize, transform.position + Vector3.right * crossSize);
            Handles.DrawLine(transform.position - Vector3.up * crossSize, transform.position + Vector3.up * crossSize);
#endif
        }
    }
}