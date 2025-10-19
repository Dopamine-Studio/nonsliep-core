using UnityEngine;

namespace Nonsliep.Core.Viewport
{
    [ExecuteAlways]
    public class ViewportRangeX : MonoBehaviour
    {
        public ViewportLayoutSystem layout;
        [Range(0, 1)] public float uMin = 0.2f, uMax = 0.8f;
        [Range(0, 1)] public float vFixed = 2f / 3f;
        public bool autoMove = true;
        public float speed = 1.2f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 0~1 맵핑

        float t; ViewportLayoutSystem.ViewportInfo _last;

        void OnEnable() { if (layout) layout.OnViewportChanged += OnView; }
        void OnDisable() { if (layout) layout.OnViewportChanged -= OnView; }
        void OnView(ViewportLayoutSystem.ViewportInfo info) { _last = info; UpdateNow(); }

        void Update() { if (autoMove) { t += Time.deltaTime * speed; UpdateNow(); } }

        void UpdateNow()
        {
            if (_last.cam == null) return;
            float zDist = Mathf.Abs(transform.position.z - _last.cam.transform.position.z);

            float s = (Mathf.Sin(t) * 0.5f + 0.5f);
            s = Mathf.Clamp01(curve.Evaluate(s));

            float u = Mathf.Lerp(uMin, uMax, s);
            Vector3 pos = _last.ViewportToWorld(new Vector2(u, vFixed), zDist);
            transform.position = new Vector3(pos.x, transform.position.y, transform.position.z);
        }
    }
}