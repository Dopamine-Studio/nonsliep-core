using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Nonsliep.Core.Boot
{
    /// <summary>
    /// Waits for <see cref="SettingsInitializer"/> to finish applying settings before continuing.
    /// </summary>
    public sealed class SettingsBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        [SerializeField] private string displayName = "Settings Initialization";
        [SerializeField] private bool critical = true;
        [SerializeField] private bool treatMissingInitializerAsSuccess;
        [SerializeField] private float timeoutSeconds = 10f;
        [SerializeField] private bool useUnscaledTime = true;
        [Header("Events")]
        [SerializeField] private UnityEvent onWaitingForInitializer;
        [SerializeField] private UnityEvent onSettingsReady;
        [SerializeField] private UnityEvent onSettingsTimeout;

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

            if (!Settings.SettingsInitializer.Exists)
            {
                onWaitingForInitializer?.Invoke();
            }

            while (!Settings.SettingsInitializer.Exists)
            {
                if (HasTimedOut(ref elapsed))
                {
                    if (treatMissingInitializerAsSuccess)
                    {
                        _succeeded = true;
                        onSettingsReady?.Invoke();
                    }
                    else
                    {
                        _succeeded = false;
                        onSettingsTimeout?.Invoke();
                    }

                    yield break;
                }

                yield return null;
            }

            var initializer = Settings.SettingsInitializer.Instance;
            if (initializer == null)
            {
                if (treatMissingInitializerAsSuccess)
                {
                    _succeeded = true;
                    onSettingsReady?.Invoke();
                }
                else
                {
                    _succeeded = false;
                    onSettingsTimeout?.Invoke();
                }

                yield break;
            }

            if (initializer.IsReady)
            {
                _succeeded = true;
                onSettingsReady?.Invoke();
                yield break;
            }

            bool done = false;

            void OnCompleted()
            {
                done = true;
            }

            initializer.InitialApplicationCompleted += OnCompleted;

            elapsed = 0f;
            while (!done)
            {
                if (HasTimedOut(ref elapsed))
                {
                    initializer.InitialApplicationCompleted -= OnCompleted;

                    _succeeded = false;
                    onSettingsTimeout?.Invoke();
                    yield break;
                }

                yield return null;
            }

            initializer.InitialApplicationCompleted -= OnCompleted;

            _succeeded = true;
            onSettingsReady?.Invoke();
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
    }
}
