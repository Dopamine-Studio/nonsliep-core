using UnityEngine;
using UnityEngine.UI;
using Nonsliep.Core.Platform;

namespace Nonsliep.Core.UI.Platform
{
    /// <summary>
    /// Simple helper to show the native leaderboard UI when clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PlatformLeaderboardButton : MonoBehaviour
    {
        [SerializeField] private string overrideLeaderboardId;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(ShowLeaderboard);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(ShowLeaderboard);
            }
        }

        private void ShowLeaderboard()
        {
            if (PlatformLeaderboardService.Instance == null)
            {
                Debug.LogWarning("PlatformLeaderboardService instance missing in scene.");
                return;
            }

            string targetId = string.IsNullOrEmpty(overrideLeaderboardId) ? "<default>" : overrideLeaderboardId;

#if UNITY_EDITOR
            Debug.Log($"[Leaderboard] Requesting native UI for {targetId}.");
#endif

            PlatformLeaderboardService.Instance.ShowNativeLeaderboardUI(overrideLeaderboardId);
        }
    }
}