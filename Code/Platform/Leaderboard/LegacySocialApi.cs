using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif

namespace Nonsliep.Core.Platform
{
    internal static class LegacySocialApi
    {
#pragma warning disable 618
        public readonly struct LocalUserSnapshot
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly bool Underage;
            public readonly Texture2D Avatar;

            public LocalUserSnapshot(string id, string displayName, bool underage, Texture2D avatar)
            {
                Id = id;
                DisplayName = displayName;
                Underage = underage;
                Avatar = avatar;
            }
        }

        public readonly struct Profile
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly Texture2D Avatar;

            public Profile(IUserProfile profile)
            {
                Id = profile?.id ?? string.Empty;
                DisplayName = profile?.userName ?? string.Empty;
                Avatar = profile?.image;
            }
        }

        public static bool IsLocalUserAuthenticated => Social.localUser != null && Social.localUser.authenticated;

        public static bool TryGetLocalUser(out LocalUserSnapshot snapshot)
        {
            var localUser = Social.localUser;
            if (localUser == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new LocalUserSnapshot(localUser.id, localUser.userName, localUser.underage, localUser.image);
            return true;
        }

        public static void AuthenticateLocalUser(Action<bool> callback)
        {
            Social.localUser.Authenticate(callback);
        }

        public static ILeaderboard CreateLeaderboard()
        {
            return Social.CreateLeaderboard();
        }

        public static void ConfigureLeaderboardRange(ILeaderboard leaderboard, int from, int count)
        {
#pragma warning disable 618
            leaderboard.range = new UnityEngine.SocialPlatforms.Range(from, count);
#pragma warning restore 618
        }

        public static void LoadUsers(string[] userIds, Action<Profile[]> onComplete)
        {
            Social.LoadUsers(userIds, profiles =>
            {
                if (profiles == null || profiles.Length == 0)
                {
                    onComplete?.Invoke(Array.Empty<Profile>());
                    return;
                }

                var wrapped = new Profile[profiles.Length];
                for (int i = 0; i < profiles.Length; i++)
                {
                    wrapped[i] = new Profile(profiles[i]);
                }

                onComplete?.Invoke(wrapped);
            });
        }

        public static void ReportScore(long score, string leaderboardId, Action<bool> callback)
        {
            Social.ReportScore(score, leaderboardId, callback);
        }

#if UNITY_IOS
        public static void ShowGameCenterLeaderboard(string leaderboardId, TimeScope scope)
        {
            GameCenterPlatform.ShowLeaderboardUI(leaderboardId, scope);
        }

        public static void ConfigureGameCenterBanner(bool show)
        {
            GameCenterPlatform.ShowDefaultAchievementCompletionBanner(show);
        }
#endif
#pragma warning restore 618
    }
}