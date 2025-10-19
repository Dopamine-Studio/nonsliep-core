using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nonsliep.Core.Platform;

namespace Nonsliep.Core.UI
{
    /// <summary>
    /// Simple helper that wires a button to the PlatformAuthManager and keeps a status label in sync.
    /// Attach to a Button, assign optional label text, and it will disable itself once signed in.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PlatformLoginButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private string signedInText = "Game Center Connected";
        [SerializeField] private string signedOutText = "Sign in with Game Center";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            SubscribeToAuthEvents(true);
            RefreshVisuals();
        }

        private void OnDisable()
        {
            SubscribeToAuthEvents(false);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!PlatformAuthManager.Exists)
            {
                Debug.LogWarning("PlatformAuthManager is not present in the scene.");
                return;
            }

            PlatformAuthManager.Instance.Authenticate();
#else
            Debug.LogWarning("Platform authentication is only available on Android or iOS builds.");
#endif
        }

        private void RefreshVisuals()
        {
            bool isSignedIn = PlatformAuthManager.Exists && PlatformAuthManager.Instance.IsAuthenticated;

            if (statusLabel != null)
            {
                statusLabel.text = isSignedIn ? signedInText : signedOutText;
            }

            if (_button != null)
            {
                _button.interactable = !isSignedIn;
            }
        }

        private void SubscribeToAuthEvents(bool subscribe)
        {
            if (!PlatformAuthManager.Exists)
            {
                return;
            }

            if (subscribe)
            {
                PlatformAuthManager.Instance.Authenticated += OnAuthenticated;
                PlatformAuthManager.Instance.AuthenticationFailed += OnAuthenticationFailed;
            }
            else
            {
                PlatformAuthManager.Instance.Authenticated -= OnAuthenticated;
                PlatformAuthManager.Instance.AuthenticationFailed -= OnAuthenticationFailed;
            }
        }

        private void OnAuthenticated()
        {
            RefreshVisuals();
        }

        private void OnAuthenticationFailed(string _)
        {
            RefreshVisuals();
        }
    }
}