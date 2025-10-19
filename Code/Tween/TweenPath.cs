// TweenPath.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonsliep.Core.Tween
{
    [CreateAssetMenu(menuName = "Tween/Tween Path", fileName = "TweenPath")]
    public class TweenPath : ScriptableObject
    {

        public enum PathInterpolation
        {
            Linear,
            CatmullRom
        }

        [SerializeField]
        private List<Vector2> points = new List<Vector2>
        {
            Vector2.zero,
            new Vector2(0f, 100f)
        };

        [SerializeField]
        [Tooltip("경로가 닫힌 형태인지 여부")]
        private bool closed;

        [SerializeField]
        [Tooltip("점 사이를 어떤 방식으로 보간할지 결정")]
        private PathInterpolation interpolation = PathInterpolation.CatmullRom;

        [SerializeField]
        [Tooltip("길이 계산 및 미리보기용 세그먼트 샘플 수 (높을수록 부드럽지만 비용 증가)")]
        [Range(4, 64)]
        private int lengthSampling = 24;

        public IReadOnlyList<Vector2> Points => points;
        public bool Closed => closed;
        public PathInterpolation InterpolationMode => interpolation;
        public int PointCount => points?.Count ?? 0;

        private float[] _segmentLengths;
        private float _totalLength;

        private void OnValidate()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            int segmentCount = GetSegmentCount();
            if (segmentCount <= 0)
            {
                _segmentLengths = Array.Empty<float>();
                _totalLength = 0f;
                return;
            }

            _segmentLengths = new float[segmentCount];
            _totalLength = 0f;

            for (int i = 0; i < segmentCount; i++)
            {
                float len = EstimateSegmentLength(i);
                _segmentLengths[i] = len;
                _totalLength += len;
            }

            if (_totalLength <= Mathf.Epsilon)
            {
                _totalLength = 1f; // fallback to avoid div by zero
            }
        }

        public Vector2 Evaluate(float t)
        {
            if (PointCount <= 0)
            {
                return Vector2.zero;
            }

            if (PointCount == 1)
            {
                return points[0];
            }

            if (_segmentLengths == null || _segmentLengths.Length != GetSegmentCount())
            {
                RebuildCache();
            }

            t = Mathf.Clamp01(t);
            float target = t * _totalLength;
            float accumulated = 0f;

            for (int i = 0; i < _segmentLengths.Length; i++)
            {
                float segLen = _segmentLengths[i];
                if (segLen <= Mathf.Epsilon)
                {
                    continue;
                }

                if (accumulated + segLen >= target)
                {
                    float segT = (target - accumulated) / segLen;
                    segT = Mathf.Clamp01(segT);
                    return EvaluateSegment(i, segT);
                }

                accumulated += segLen;
            }

            // fallback - return last point / wrap
            return EvaluateSegment(_segmentLengths.Length - 1, 1f);
        }

        public Vector2 GetPoint(int index)
        {
            if (PointCount == 0)
            {
                return Vector2.zero;
            }

            if (index < 0)
            {
                index = 0;
            }
            else if (index >= PointCount)
            {
                index = PointCount - 1;
            }

            return points[index];
        }

        public void EnsureMinimumPoints()
        {
            if (points == null)
            {
                points = new List<Vector2>();
            }

            if (points.Count == 0)
            {
                points.Add(Vector2.zero);
            }

            if (points.Count == 1)
            {
                points.Add(Vector2.up * 100f);
            }
        }

        private int GetSegmentCount()
        {
            if (PointCount < 2)
            {
                return 0;
            }

            return closed ? PointCount : PointCount - 1;
        }

        private Vector2 GetSegmentEndPoint(int segmentIndex)
        {
            if (closed)
            {
                int next = (segmentIndex + 1) % PointCount;
                return points[next];
            }
            else
            {
                int next = Mathf.Min(segmentIndex + 1, PointCount - 1);
                return points[next];
            }
        }

        private Vector2 EvaluateSegment(int segmentIndex, float t)
        {
            if (interpolation == PathInterpolation.CatmullRom && PointCount >= 4)
            {
                return EvaluateCatmullRom(segmentIndex, t);
            }

            // fallback to linear
            Vector2 a = points[Mathf.Clamp(segmentIndex, 0, PointCount - 1)];
            Vector2 b = GetSegmentEndPoint(segmentIndex);
            return Vector2.LerpUnclamped(a, b, t);
        }

        private Vector2 EvaluateCatmullRom(int segmentIndex, float t)
        {
            int p1 = segmentIndex;
            int p2 = segmentIndex + 1;

            if (closed)
            {
                p1 = WrapIndex(p1);
                p2 = WrapIndex(p2);
            }
            else
            {
                p1 = Mathf.Clamp(p1, 0, PointCount - 1);
                p2 = Mathf.Clamp(p2, 0, PointCount - 1);
            }

            int p0 = p1 - 1;
            int p3 = p2 + 1;

            if (closed)
            {
                p0 = WrapIndex(p0);
                p3 = WrapIndex(p3);
            }
            else
            {
                p0 = Mathf.Clamp(p0, 0, PointCount - 1);
                p3 = Mathf.Clamp(p3, 0, PointCount - 1);
            }

            Vector2 P0 = points[p0];
            Vector2 P1 = points[p1];
            Vector2 P2 = points[p2];
            Vector2 P3 = points[p3];

            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2f * P1)
                + (-P0 + P2) * t
                + (2f * P0 - 5f * P1 + 4f * P2 - P3) * t2
                + (-P0 + 3f * P1 - 3f * P2 + P3) * t3);
        }

        private float EstimateSegmentLength(int segmentIndex)
        {
            if (interpolation == PathInterpolation.CatmullRom && PointCount >= 4)
            {
                int samples = Mathf.Clamp(lengthSampling, 4, 128);
                Vector2 prev = EvaluateSegment(segmentIndex, 0f);
                float length = 0f;
                for (int i = 1; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    Vector2 current = EvaluateSegment(segmentIndex, t);
                    length += Vector2.Distance(prev, current);
                    prev = current;
                }
                return Mathf.Max(length, Mathf.Epsilon);
            }

            Vector2 a = points[Mathf.Clamp(segmentIndex, 0, PointCount - 1)];
            Vector2 b = GetSegmentEndPoint(segmentIndex);
            float len = Vector2.Distance(a, b);
            return Mathf.Max(len, Mathf.Epsilon);
        }

        private int WrapIndex(int index)
        {
            if (PointCount == 0)
            {
                return 0;
            }

            int m = index % PointCount;
            if (m < 0)
            {
                m += PointCount;
            }
            return m;
        }
    }
}
