using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms;
#if UNITY_ANDROID
using GooglePlayGames;
#endif

namespace Nonsliep.Core.Platform
{
    /// <summary>
    /// High-level wrapper that routes authentication requests to the underlying platform service
    /// (Google Play Games on Android, Game Center on iOS). Expose a unified workflow for gameplay systems.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlatformAuthManager : Scene.SceneSingleton<PlatformAuthManager>
    {
        [Header("Behaviour")]
        [SerializeField] private bool authenticateOnStart = true;

    #if UNITY_IOS
        [SerializeField] private bool showGameCenterAchievementBanner = true;
    #endif

    #if UNITY_ANDROID
        [Header("Android Options")]
        [SerializeField] private bool enablePlayGamesDebugLogs = false;
    #endif

        [Header("Events")]
        [SerializeField] private UnityEvent onAuthenticated;
        [SerializeField] private StringEvent onAuthenticationFailed;

        private IPlatformAuthProvider _provider = NullAuthProvider.Instance;

        public event Action Authenticated;
        public event Action<string> AuthenticationFailed;

        public bool IsAuthenticated => _provider.IsAuthenticated;
        public bool IsProviderAvailable => _provider.IsAvailable;

        protected override void OnSingletonReady()
        {
            _provider = CreateProvider();
            _provider.Authenticated += HandleAuthenticated;
            _provider.AuthenticationFailed += HandleAuthenticationFailed;
            _provider.Initialize();
        }

        private void Start()
        {
            if (authenticateOnStart && _provider.IsAvailable)
            {
                Authenticate();
            }
        }

        public void Authenticate()
        {
            if (!_provider.IsAvailable)
            {
                Debug.LogWarning("PlatformAuthManager: Authentication not available on this platform.");
                return;
            }

            _provider.Authenticate();
        }

        public bool TryGetLocalPlayer(out PlatformUserProfile profile)
        {
            return _provider.TryGetLocalPlayer(out profile);
        }

        public void LoadAvatar(Action<Texture2D> onCompleted)
        {
            _provider.LoadAvatar(onCompleted);
        }

        protected override void OnSingletonDestroyed()
        {
            if (_provider != null)
            {
                _provider.Authenticated -= HandleAuthenticated;
                _provider.AuthenticationFailed -= HandleAuthenticationFailed;
            }
        }

        private IPlatformAuthProvider CreateProvider()
        {
    #if UNITY_IOS
            return new GameCenterAuthProvider(showGameCenterAchievementBanner);
    #elif UNITY_ANDROID
            return new GooglePlayGamesAuthProvider(enablePlayGamesDebugLogs);
    #else
            return NullAuthProvider.Instance;
    #endif
        }

        private void HandleAuthenticated()
        {
            onAuthenticated?.Invoke();
            Authenticated?.Invoke();
        }

        private void HandleAuthenticationFailed(string error)
        {
            onAuthenticationFailed?.Invoke(error);
            AuthenticationFailed?.Invoke(error);
        }

        [Serializable]
        public readonly struct PlatformUserProfile
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly bool Underage;
            public readonly Texture2D Avatar;

            public PlatformUserProfile(string id, string displayName, bool underage, Texture2D avatar)
            {
                Id = id;
                DisplayName = displayName;
                Underage = underage;
                Avatar = avatar;
            }
        }

        [Serializable]
        public class StringEvent : UnityEvent<string> { }

        private interface IPlatformAuthProvider
        {
            bool IsAvailable { get; }
            bool IsAuthenticated { get; }
            event Action Authenticated;
            event Action<string> AuthenticationFailed;
            void Initialize();
            void Authenticate();
            bool TryGetLocalPlayer(out PlatformUserProfile profile);
            void LoadAvatar(Action<Texture2D> onCompleted);
        }

        private sealed class NullAuthProvider : IPlatformAuthProvider
        {
            public static readonly NullAuthProvider Instance = new();
            public bool IsAvailable => false;
            public bool IsAuthenticated => false;
            public event Action Authenticated
            {
                add { }
                remove { }
            }
            private event Action<string> _authenticationFailed;
            public event Action<string> AuthenticationFailed
            {
                add => _authenticationFailed += value;
                remove => _authenticationFailed -= value;
            }
            public void Initialize() { }
            public void Authenticate()
            {
                _authenticationFailed?.Invoke("Authentication not supported on this platform.");
            }
            public bool TryGetLocalPlayer(out PlatformUserProfile profile)
            {
                profile = default;
                return false;
            }
            public void LoadAvatar(Action<Texture2D> onCompleted)
            {
                onCompleted?.Invoke(null);
            }
        }

    #if UNITY_IOS
        private sealed class GameCenterAuthProvider : IPlatformAuthProvider
        {
            public bool IsAvailable => true;
            public bool IsAuthenticated => LegacySocialApi.IsLocalUserAuthenticated;
            public event Action Authenticated;
            public event Action<string> AuthenticationFailed;

            private readonly bool _showAchievementBanner;
            private bool _isAuthenticating;

            public GameCenterAuthProvider(bool showAchievementBanner)
            {
                _showAchievementBanner = showAchievementBanner;
            }

            public void Initialize()
            {
                if (_showAchievementBanner)
                {
                    LegacySocialApi.ConfigureGameCenterBanner(true);
                }
            }

            public void Authenticate()
            {
                if (LegacySocialApi.IsLocalUserAuthenticated)
                {
                    Authenticated?.Invoke();
                    return;
                }

                if (_isAuthenticating)
                {
                    return;
                }

                _isAuthenticating = true;

                LegacySocialApi.AuthenticateLocalUser(success =>
                {
                    _isAuthenticating = false;

                    if (success)
                    {
                        Authenticated?.Invoke();
                        return;
                    }

                    string message = "Failed to authenticate with Game Center.";
                    AuthenticationFailed?.Invoke(message);
                    Debug.LogWarning(message);
                });
            }

            public bool TryGetLocalPlayer(out PlatformUserProfile profile)
            {
                if (!IsAuthenticated)
                {
                    profile = default;
                    return false;
                }

                if (!LegacySocialApi.TryGetLocalUser(out var snapshot))
                {
                    profile = default;
                    return false;
                }

                profile = new PlatformUserProfile(snapshot.Id, snapshot.DisplayName, snapshot.Underage, snapshot.Avatar);
                return true;
            }

            public void LoadAvatar(Action<Texture2D> onCompleted)
            {
                if (!IsAuthenticated || !LegacySocialApi.TryGetLocalUser(out var snapshot))
                {
                    onCompleted?.Invoke(null);
                    return;
                }

                string userId = snapshot.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    onCompleted?.Invoke(snapshot.Avatar);
                    return;
                }

                LegacySocialApi.LoadUsers(new[] { userId }, profiles =>
                {
                    Texture2D avatar = snapshot.Avatar;
                    if (profiles.Length > 0)
                    {
                        var profile = profiles[0];
                        avatar = profile.Avatar ?? avatar;
                    }

                    onCompleted?.Invoke(avatar);
                });
            }
        }
    #endif

    #if UNITY_ANDROID
        private sealed class GooglePlayGamesAuthProvider : IPlatformAuthProvider
        {
            public bool IsAvailable => true;
            public bool IsAuthenticated => LegacySocialApi.IsLocalUserAuthenticated;
            public event Action Authenticated;
            public event Action<string> AuthenticationFailed;

            private readonly bool _enableDebugLogs;
            private bool _isAuthenticating;
            private bool _configured;

            public GooglePlayGamesAuthProvider(bool enableDebugLogs)
            {
                _enableDebugLogs = enableDebugLogs;
            }

            public void Initialize()
            {
                if (_configured)
                {
                    return;
                }

                // Latest Google Play Games SDK auto-configures the platform, so only enable logs and activate.
                PlayGamesPlatform.DebugLogEnabled = _enableDebugLogs;
                PlayGamesPlatform.Activate();
                _configured = true;
            }

            public void Authenticate()
            {
                if (LegacySocialApi.IsLocalUserAuthenticated)
                {
                    Authenticated?.Invoke();
                    return;
                }

                if (_isAuthenticating)
                {
                    return;
                }

                _isAuthenticating = true;

                LegacySocialApi.AuthenticateLocalUser(success =>
                {
                    _isAuthenticating = false;

                    if (success)
                    {
                        Authenticated?.Invoke();
                        return;
                    }

                    string message = "Failed to authenticate with Google Play Games.";
                    AuthenticationFailed?.Invoke(message);
                    Debug.LogWarning(message);
                });
            }

            public bool TryGetLocalPlayer(out PlatformUserProfile profile)
            {
                if (!IsAuthenticated)
                {
                    profile = default;
                    return false;
                }

                if (!LegacySocialApi.TryGetLocalUser(out var snapshot))
                {
                    profile = default;
                    return false;
                }

                profile = new PlatformUserProfile(snapshot.Id, snapshot.DisplayName, snapshot.Underage, snapshot.Avatar);
                return true;
            }

            public void LoadAvatar(Action<Texture2D> onCompleted)
            {
                if (!IsAuthenticated || !LegacySocialApi.TryGetLocalUser(out var snapshot))
                {
                    onCompleted?.Invoke(null);
                    return;
                }

                string userId = snapshot.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    onCompleted?.Invoke(snapshot.Avatar);
                    return;
                }

                LegacySocialApi.LoadUsers(new[] { userId }, profiles =>
                {
                    Texture2D avatar = snapshot.Avatar;
                    if (profiles.Length > 0)
                    {
                        var profile = profiles[0];
                        avatar = profile.Avatar ?? avatar;
                    }

                    onCompleted?.Invoke(avatar);
                });
            }
        }
    #endif
    }
}
