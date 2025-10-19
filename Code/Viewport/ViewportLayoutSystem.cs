using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nonsliep.Core.Viewport
{
    [ExecuteAlways]
    public class ViewportLayoutSystem : MonoBehaviour
    {
        public Camera cam;
        public event Action<ViewportInfo> OnViewportChanged;

        [Serializable]
        public struct ViewportInfo
        {
            public Camera cam;
            public Vector3 BL, BR, TL, TR; // 화면 4모서리 월드 좌표
            public float Width, Height;    // 월드 폭/높이
            public float OrthoSize;        // 세로 절반(ortho)
            public float Aspect;           // 가로/세로
            public Func<Vector2, float, Vector3> ViewportToWorld; // (u,v,zDist)
        }

        ViewportInfo _info;
        int _w, _h; float _ortho; Vector3 _camPos; Quaternion _camRot;

        void Awake() { if (!cam) cam = Camera.main; }

        void LateUpdate()
        {
            if (!cam) return;

            bool changed = false;
            if (_w != Screen.width || _h != Screen.height) changed = true;
            if (cam.orthographic && !Mathf.Approximately(_ortho, cam.orthographicSize)) changed = true;
            if (_camPos != cam.transform.position || _camRot != cam.transform.rotation) changed = true;

            if (!changed) return;

            _w = Screen.width; _h = Screen.height;
            _ortho = cam.orthographic ? cam.orthographicSize : _ortho;
            _camPos = cam.transform.position; _camRot = cam.transform.rotation;

            float zBL = Mathf.Abs(_camPos.z - cam.transform.position.z); // 동일평면 가정
            Vector3 BL = cam.ViewportToWorldPoint(new Vector3(0, 0, zBL));
            Vector3 BR = cam.ViewportToWorldPoint(new Vector3(1, 0, zBL));
            Vector3 TL = cam.ViewportToWorldPoint(new Vector3(0, 1, zBL));
            Vector3 TR = cam.ViewportToWorldPoint(new Vector3(1, 1, zBL));

            _info = new ViewportInfo
            {
                cam = cam,
                BL = BL,
                BR = BR,
                TL = TL,
                TR = TR,
                Width = BR.x - BL.x,
                Height = TL.y - BL.y,
                OrthoSize = cam.orthographic ? cam.orthographicSize : 0f,
                Aspect = cam.aspect,
                ViewportToWorld = (uv, zDist) => cam.ViewportToWorldPoint(new Vector3(uv.x, uv.y, zDist))
            };

            OnViewportChanged?.Invoke(_info);
        }
    }
}