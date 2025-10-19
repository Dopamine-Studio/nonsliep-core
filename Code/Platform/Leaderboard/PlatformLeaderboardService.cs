#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Serialization;

namespace Nonsliep.Core.Platform
{
    /// <summary>
    /// Fetches leaderboard entries from the underlying platform service (Google Play Games on Android, Game Center on iOS).
    /// Attach this to a bootstrapped object to provide a singleton-style access point.
    /// </summary>
    public class PlatformLeaderboardService : MonoBehaviour
    {
        public static PlatformLeaderboardService Instance { get; private set; }

        [Header("Leaderboard Identifiers")]
        [SerializeField] private string androidLeaderboardId;
        [SerializeField] private string iosLeaderboardId;

        [Header("Fetch Options")]
        [FormerlySerializedAs("timeScope")]
        [SerializeField] private LeaderboardTimeRange timeRange = LeaderboardTimeRange.AllTime;
        [FormerlySerializedAs("userScope")]
        [SerializeField] private LeaderboardUserFilter userFilter = LeaderboardUserFilter.Global;
        [SerializeField] private int defaultMaxEntries = 10;

#if UNITY_ANDROID
        [Header("Android Options")]
        [SerializeField] private bool enablePlayGamesDebugLogs;
#endif

        // Queue callbacks while authentication is in flight to avoid racing Authenticate calls.
        private readonly List<Action> _pendingAuthSuccess = new();
        private readonly List<Action<string>> _pendingAuthFailure = new();

        private bool _isAuthenticating;
#if UNITY_ANDROID
        private bool _playGamesConfigured;
#endif

        public enum LeaderboardTimeRange
        {
            Today,
            Week,
            AllTime
        }

        public enum LeaderboardUserFilter
        {
            Global,
            Friends
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID
            ConfigurePlayGames();
#endif
        }

        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            AuthenticateIfNeeded(null, null);
#endif
        }

        /// <summary>
        /// Fetches leaderboard entries for the current platform.
        /// </summary>
        /// <param name="onSuccess">Invoked with the fetched entries.</param>
        /// <param name="onFailure">Invoked with an error message if the fetch fails.</param>
        /// <param name="maxEntries">Optional override for the number of entries to load.</param>
        /// <param name="overrideLeaderboardId">Optional override for the leaderboard identifier.</param>
        public void FetchLeaderboardEntries(Action<List<LeaderboardEntry>> onSuccess, Action<string> onFailure = null, int maxEntries = 0, string overrideLeaderboardId = null)
        {
            int limit = ResolveEntryLimit(maxEntries);
#if UNITY_ANDROID || UNITY_IOS
            if (onSuccess == null)
            {
                Debug.LogWarning("PlatformLeaderboardService: FetchLeaderboardEntries called without a success callback.");
                return;
            }

            string leaderboardId = ResolveLeaderboardId(overrideLeaderboardId);
            if (string.IsNullOrEmpty(leaderboardId))
            {
                onFailure?.Invoke("Leaderboard ID is not configured for this platform.");
                return;
            }

            AuthenticateIfNeeded(() => LoadScores(leaderboardId, limit, onSuccess, onFailure), onFailure);
#else
            _ = limit;
            onFailure?.Invoke("Leaderboards are not supported on this platform in the current build.");
#endif
        }

        /// <summary>
        /// Displays the native leaderboard UI provided by the platform.
        /// </summary>
        public void ShowNativeLeaderboardUI(string overrideLeaderboardId = null)
        {
#if UNITY_ANDROID || UNITY_IOS
            string leaderboardId = ResolveLeaderboardId(overrideLeaderboardId);
            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogWarning("PlatformLeaderboardService: Leaderboard ID is not configured for this platform.");
                return;
            }

            AuthenticateIfNeeded(() =>
            {
#if UNITY_ANDROID
                PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
#elif UNITY_IOS
                LegacySocialApi.ShowGameCenterLeaderboard(leaderboardId, ResolveTimeScope());
#endif
            }, error => Debug.LogWarning($"PlatformLeaderboardService: {error}"));
#else
            Debug.LogWarning("PlatformLeaderboardService: Leaderboard UI is not supported on this platform.");
#endif
        }

        /// <summary>
        /// Reports a score to the configured leaderboard. Call after the player clears a stage.
        /// </summary>
        public void SubmitScore(long scoreValue, Action onSuccess = null, Action<string> onFailure = null, string overrideLeaderboardId = null)
        {
#if UNITY_ANDROID || UNITY_IOS
            string leaderboardId = ResolveLeaderboardId(overrideLeaderboardId);
            if (string.IsNullOrEmpty(leaderboardId))
            {
                onFailure?.Invoke("Leaderboard ID is not configured for this platform.");
                return;
            }

            AuthenticateIfNeeded(() => ReportScore(scoreValue, leaderboardId, onSuccess, onFailure), onFailure);
#else
            Debug.LogWarning("PlatformLeaderboardService: Leaderboard submissions are not supported on this platform.");
            onFailure?.Invoke("Leaderboards are not supported on this platform in the current build.");
#endif
        }

        /// <summary>
        /// Reports a packed score that encodes moves/time using the provided utility helper.
        /// </summary>
        public void SubmitCompositeScore(int moves, float timeSeconds, Action onSuccess = null, Action<string> onFailure = null, string overrideLeaderboardId = null)
        {
            long packedScore = LeaderboardScoreUtility.EncodeCompositeScore(moves, timeSeconds);
            SubmitScore(packedScore, onSuccess, onFailure, overrideLeaderboardId);
        }

        private int ResolveEntryLimit(int maxEntries)
        {
            return maxEntries > 0 ? maxEntries : Mathf.Max(1, defaultMaxEntries);
        }

#pragma warning disable 618
        private TimeScope ResolveTimeScope()
        {
            return timeRange switch
            {
                LeaderboardTimeRange.Today => TimeScope.Today,
                LeaderboardTimeRange.Week => TimeScope.Week,
                _ => TimeScope.AllTime
            };
        }

        private UserScope ResolveUserScope()
        {
            return userFilter switch
            {
                LeaderboardUserFilter.Friends => UserScope.FriendsOnly,
                _ => UserScope.Global
            };
        }
#pragma warning restore 618

#if UNITY_ANDROID || UNITY_IOS
        private void LoadScores(string leaderboardId, int maxEntries, Action<List<LeaderboardEntry>> onSuccess, Action<string> onFailure)
        {
            var leaderboard = LegacySocialApi.CreateLeaderboard();
            leaderboard.id = leaderboardId;
            leaderboard.timeScope = ResolveTimeScope();
            leaderboard.userScope = ResolveUserScope();
            LegacySocialApi.ConfigureLeaderboardRange(leaderboard, 1, maxEntries);

            leaderboard.LoadScores(success =>
            {
                if (!success)
                {
                    onFailure?.Invoke("Failed to load leaderboard scores.");
                    return;
                }

                var scores = leaderboard.scores;
                if (scores == null || scores.Length == 0)
                {
                    onSuccess?.Invoke(new List<LeaderboardEntry>());
                    return;
                }

                var uniqueIds = new HashSet<string>();
                var userIds = new List<string>(scores.Length);
                for (int i = 0; i < scores.Length; i++)
                {
                    if (!string.IsNullOrEmpty(scores[i].userID) && uniqueIds.Add(scores[i].userID))
                    {
                        userIds.Add(scores[i].userID);
                    }
                }

                LegacySocialApi.LoadUsers(userIds.ToArray(), profiles =>
                {
                    var profileLookup = new Dictionary<string, LegacySocialApi.Profile>(profiles.Length);
                    for (int i = 0; i < profiles.Length; i++)
                    {
                        var profile = profiles[i];
                        if (!string.IsNullOrEmpty(profile.Id))
                        {
                            profileLookup[profile.Id] = profile;
                        }
                    }

                    var entries = new List<LeaderboardEntry>(scores.Length);
                    for (int i = 0; i < scores.Length; i++)
                    {
                        var score = scores[i];
                        bool hasProfile = profileLookup.TryGetValue(score.userID, out var profile);
                        string displayName = hasProfile && !string.IsNullOrEmpty(profile.DisplayName)
                            ? profile.DisplayName
                            : score.userID;
                        Texture2D avatar = hasProfile ? profile.Avatar : null;

                        entries.Add(new LeaderboardEntry(
                            score.userID,
                            displayName,
                            score.formattedValue,
                            score.value,
                            score.rank,
                            avatar));
                    }

                    onSuccess?.Invoke(entries);
                });
            });
        }

        private void AuthenticateIfNeeded(Action onSuccess, Action<string> onFailure)
        {
            if (LegacySocialApi.IsLocalUserAuthenticated)
            {
                onSuccess?.Invoke();
                return;
            }

            if (_isAuthenticating)
            {
                if (onSuccess != null)
                {
                    _pendingAuthSuccess.Add(onSuccess);
                }

                if (onFailure != null)
                {
                    _pendingAuthFailure.Add(onFailure);
                }

                return;
            }

            _isAuthenticating = true;

            if (onSuccess != null)
            {
                _pendingAuthSuccess.Add(onSuccess);
            }

            if (onFailure != null)
            {
                _pendingAuthFailure.Add(onFailure);
            }

            LegacySocialApi.AuthenticateLocalUser(success =>
            {
                _isAuthenticating = false;

                if (success)
                {
                    for (int i = 0; i < _pendingAuthSuccess.Count; i++)
                    {
                        _pendingAuthSuccess[i]?.Invoke();
                    }
                }
                else
                {
                    string message = "Failed to authenticate local user for leaderboards.";
                    for (int i = 0; i < _pendingAuthFailure.Count; i++)
                    {
                        _pendingAuthFailure[i]?.Invoke(message);
                    }
                }

                _pendingAuthSuccess.Clear();
                _pendingAuthFailure.Clear();
            });
        }

        private void ReportScore(long scoreValue, string leaderboardId, Action onSuccess, Action<string> onFailure)
        {
            LegacySocialApi.ReportScore(scoreValue, leaderboardId, success =>
            {
                if (!success)
                {
                    onFailure?.Invoke("Failed to report score to leaderboard.");
                    return;
                }

                onSuccess?.Invoke();
            });
        }
#endif

#if UNITY_ANDROID
        private void ConfigurePlayGames()
        {
            if (_playGamesConfigured)
            {
                return;
            }

            PlayGamesPlatform.DebugLogEnabled = enablePlayGamesDebugLogs;
            PlayGamesPlatform.Activate();
            _playGamesConfigured = true;
        }
#endif

        private string ResolveLeaderboardId(string overrideLeaderboardId)
        {
            if (!string.IsNullOrEmpty(overrideLeaderboardId))
            {
                return overrideLeaderboardId;
            }

#if UNITY_ANDROID
            return androidLeaderboardId;
#elif UNITY_IOS
            return iosLeaderboardId;
#else
            return string.Empty;
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [Serializable]
        public class LeaderboardEntry
        {
            public string UserId { get; }
            public string DisplayName { get; }
            public string FormattedValue { get; }
            public long Value { get; }
            public int Rank { get; }
            public Texture2D Avatar { get; }

            public LeaderboardEntry(string userId, string displayName, string formattedValue, long value, int rank, Texture2D avatar)
            {
                UserId = userId;
                DisplayName = displayName;
                FormattedValue = formattedValue;
                Value = value;
                Rank = rank;
                Avatar = avatar;
            }
        }
    }
}