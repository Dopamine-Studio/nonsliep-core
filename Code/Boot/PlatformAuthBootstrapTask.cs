using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Nonsliep.Core.Platform;

namespace Nonsliep.Core.Boot
{
    /// <summary>
    /// Performs platform authentication during the bootstrap flow.
    /// </summary>
    public sealed class PlatformAuthBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        [SerializeField] private string displayName = "Platform Authentication";
        [SerializeField] private bool critical = true;
        [SerializeField] private bool authenticateIfNeeded = true;
        [SerializeField] private bool skipIfAlreadyAuthenticated = true;
        [SerializeField] private bool treatUnavailableAsSuccess = true;
        [SerializeField] private float timeoutSeconds = 20f;
        [SerializeField] private bool useUnscaledTime = true;
        [Header("Events")]
        [SerializeField] private UnityEvent onAuthenticationStarted;
        [SerializeField] private UnityEvent onAuthenticationSucceeded;
        [SerializeField] private StringEvent onAuthenticationFailed;

        private bool _succeeded;

        public string TaskName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public bool IsCritical => critical;
        public bool WasSuccessful => _succeeded;

        public void ResetState()
        {
            _succeeded = false;
        }

        public IEnumerator Execute()
        {
            float elapsed = 0f;

            while (!PlatformAuthManager.Exists)
            {
                if (HasTimedOut(ref elapsed))
                {
                    HandleUnavailable("PlatformAuthManager instance not found.");
                    yield break;
                }
                yield return null;
            }

            var manager = PlatformAuthManager.Instance;
            if (manager == null)
            {
                HandleUnavailable("PlatformAuthManager instance missing.");
                yield break;
            }

            if (!manager.IsProviderAvailable)
            {
                HandleUnavailable("Authentication provider is not available on this platform.");
                yield break;
            }

            if (skipIfAlreadyAuthenticated && manager.IsAuthenticated)
            {
                _succeeded = true;
                onAuthenticationSucceeded?.Invoke();
                yield break;
            }

            bool done = manager.IsAuthenticated;
            bool success = manager.IsAuthenticated;
            string failureMessage = null;

            void OnAuthenticated()
            {
                done = true;
                success = true;
            }

            void OnFailed(string error)
            {
                done = true;
                success = false;
                failureMessage = string.IsNullOrEmpty(error) ? "Authentication failed." : error;
            }

            manager.Authenticated += OnAuthenticated;
            manager.AuthenticationFailed += OnFailed;

            if (!done && authenticateIfNeeded)
            {
                onAuthenticationStarted?.Invoke();
                manager.Authenticate();
            }

            elapsed = 0f;
            while (!done)
            {
                if (HasTimedOut(ref elapsed))
                {
                    failureMessage = "Authentication timed out.";
                    success = false;
                    break;
                }

                yield return null;
            }

            manager.Authenticated -= OnAuthenticated;
            manager.AuthenticationFailed -= OnFailed;

            if (success)
            {
                _succeeded = true;
                onAuthenticationSucceeded?.Invoke();
            }
            else
            {
                _succeeded = false;
                onAuthenticationFailed?.Invoke(failureMessage);
            }
        }

        private void HandleUnavailable(string message)
        {
            if (treatUnavailableAsSuccess)
            {
                _succeeded = true;
                onAuthenticationSucceeded?.Invoke();
            }
            else
            {
                _succeeded = false;
                onAuthenticationFailed?.Invoke(message);
            }
        }

        private bool HasTimedOut(ref float elapsed)
        {
            if (timeoutSeconds <= 0f)
            {
                return false;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            return elapsed >= timeoutSeconds;
        }

        [System.Serializable]
        private sealed class StringEvent : UnityEvent<string> { }
    }
}
