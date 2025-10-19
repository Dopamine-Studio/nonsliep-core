using UnityEngine;

namespace Nonsliep.Core.UI
{
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect _lastSafeArea;
        Vector2Int _lastScreen;

        RectTransform _rt;

        void Awake()
        {
            _rt = (RectTransform)transform;
            Apply();
        }

        void OnEnable() => Apply();
        void Update()
        {
            if (_lastScreen.x != Screen.width || _lastScreen.y != Screen.height || _lastSafeArea != Screen.safeArea)
                Apply();
        }

        void Apply()
        {
            var safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width; anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width; anchorMax.y /= Screen.height;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}