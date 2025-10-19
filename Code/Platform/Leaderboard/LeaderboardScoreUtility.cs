using UnityEngine;

namespace Nonsliep.Core.Platform
{
    /// <summary>
    /// Helper methods to pack multiple performance metrics (moves, time) into a single long value accepted by platform leaderboards.
    /// Lower moves/time are treated as better scores by default.
    /// </summary>
    public static class LeaderboardScoreUtility
    {
        private const int TimePrecisionMilliseconds = 1000; // store time with millisecond precision
        private const int MaxStoredTimeMilliseconds = 999_999_999; // cap to keep value within range
        private const int ScorePadding = 1_000_000_000;

        /// <summary>
        /// Encodes gameplay stats into a single long where lower moves/time result in higher leaderboard values.
        /// </summary>
        public static long EncodeCompositeScore(int moves, float timeSeconds)
        {
            int clampedMoves = Mathf.Clamp(moves, 0, 999_999);
            int timeMilliseconds = Mathf.Clamp(Mathf.RoundToInt(timeSeconds * TimePrecisionMilliseconds), 0, MaxStoredTimeMilliseconds);

            long packed = (long)clampedMoves * ScorePadding + timeMilliseconds;
            return long.MaxValue - packed;
        }

        /// <summary>
        /// Decodes the composite value back into moves/time for client-side display.
        /// </summary>
        public static void DecodeCompositeScore(long encodedScore, out int moves, out float timeSeconds)
        {
            long packed = long.MaxValue - encodedScore;
            moves = (int)(packed / ScorePadding);
            int timeMilliseconds = (int)(packed % ScorePadding);
            timeSeconds = timeMilliseconds / (float)TimePrecisionMilliseconds;
        }
    }
}