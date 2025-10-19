// Assets/Scripts/AppLifecycleGuard.cs
using UnityEngine;

public class AppLifecycleGuard : MonoBehaviour
{
    // 씬이 바뀌어도 계속 살려둔다.
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            foreach (var cam in Camera.allCameras) cam.enabled = false;

#if DOTWEEN_PRESENT // DOTween이 있을 때만
            DG.Tweening.DOTween.PauseAll();
#endif
        }
        else
        {
            foreach (var cam in Camera.allCameras) cam.enabled = true;

#if DOTWEEN_PRESENT
            DG.Tweening.DOTween.PlayAll();
#endif
        }
    }

    // iOS에서 홈 복귀 시엔 Focus가 먼저/나중에 올 수 있어 둘 다 잡아두면 안전
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            foreach (var cam in Camera.allCameras) cam.enabled = false;
#if DOTWEEN_PRESENT
            DG.Tweening.DOTween.PauseAll();
#endif
        }
        else
        {
            foreach (var cam in Camera.allCameras) cam.enabled = true;
#if DOTWEEN_PRESENT
            DG.Tweening.DOTween.PlayAll();
#endif
        }
    }
}
