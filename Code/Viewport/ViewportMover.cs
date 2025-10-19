using System.Collections.Generic;
using UnityEngine;

namespace Nonsliep.Core.Viewport
{
    public class ViewportMover : MonoBehaviour
    {
        public enum MovementMode
        {
            None,
            TwoPoint,
            BounceInArea
        }

        [Header("Mode")]
        public MovementMode mode = MovementMode.None;
        [Tooltip("TwoPoint 모드 이동 속도")]
        public float twoPointSpeed = 1f;
        [Tooltip("BounceInArea 모드 이동 속도")]
        public float bounceSpeed = 1f;

        [Header("Two Point Settings")]
        public Transform pointA;
        public Transform pointB;
        public bool loop = true;

        [Header("Bounce Settings")]
        [Tooltip("Four corner points that define the rectangular bounds (world space).")]
        public Transform[] areaCorners = new Transform[4];
        [Tooltip("튜닝용: 함께 움직이는 기준 오브젝트. 이 오브젝트가 벽 밖으로 나가려 하면 튕깁니다.")]
        public List<Transform> bounceReferences = new();
        public Vector2 initialVelocity = new Vector2(1f, 0.5f);

        float _twoPointDistance;
        float _twoPointTime;
        Vector2 _velocity;

        void Awake()
        {
            RecalculateTwoPointState(true);
            InitTrackedObjects();
        }

        void OnValidate()
        {
            RecalculateTwoPointState(true);
            InitTrackedObjects();
        }

        void RecalculateTwoPointState(bool snapToSegment)
        {
            _twoPointDistance = 0f;

            if (!pointA || !pointB) return;

            Vector3 a = pointA.position;
            Vector3 b = pointB.position;
            Vector3 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq <= Mathf.Epsilon) return;

            _twoPointDistance = Mathf.Sqrt(lenSq);

            float t = Mathf.Clamp01(Vector3.Dot(transform.position - a, ab) / lenSq);

            if (snapToSegment)
            {
                Vector3 projected = Vector3.Lerp(a, b, t);
                transform.position = projected;
            }

            _twoPointTime = t;
        }

        void InitTrackedObjects()
        {
            if (bounceReferences == null) bounceReferences = new List<Transform>();
            if (!bounceReferences.Contains(transform)) bounceReferences.Insert(0, transform);
            if (_velocity == Vector2.zero) _velocity = initialVelocity;
        }

        public void SetMode(MovementMode newMode)
        {
            mode = newMode;

            if (mode == MovementMode.None)
            {
                enabled = false;
                return;
            }

            enabled = true;

            switch (mode)
            {
                case MovementMode.TwoPoint:
                    RecalculateTwoPointState(true);
                    break;
                case MovementMode.BounceInArea:
                    _velocity = initialVelocity;
                    break;
            }
        }

        public void ReinitializeMovement()
        {
            if (mode == MovementMode.TwoPoint)
            {
                RecalculateTwoPointState(true);
            }

            if (mode == MovementMode.BounceInArea)
            {
                _velocity = initialVelocity;
            }
        }

        void Update()
        {
            switch (mode)
            {
                case MovementMode.TwoPoint:
                    UpdateTwoPoint();
                    break;
                case MovementMode.BounceInArea:
                    UpdateBounce();
                    break;
            }
        }

        void UpdateTwoPoint()
        {
            if (!pointA || !pointB)
                return;

            if (_twoPointDistance <= 0f)
            {
                RecalculateTwoPointState(false);
                if (_twoPointDistance <= 0f)
                    return;
            }

            _twoPointTime += Time.deltaTime * Mathf.Max(0f, twoPointSpeed);
            float t = loop ? Mathf.PingPong(_twoPointTime, 1f) : Mathf.Repeat(_twoPointTime, 1f);
            transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
        }

        void UpdateBounce()
        {
            if (_velocity == Vector2.zero) _velocity = initialVelocity;
            if (!TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)) return;

            Vector3 current = transform.position;
            float effectiveSpeed = Mathf.Max(0f, bounceSpeed);
            Vector3 delta = new Vector3(_velocity.x, _velocity.y, 0f) * (effectiveSpeed * Time.deltaTime);

            // Horizontal bounds check
            if (WouldExitBounds(axis: 0, current, delta, minX, maxX))
            {
                _velocity.x *= -1f;
                delta.x = _velocity.x * (effectiveSpeed * Time.deltaTime);
            }

            // Vertical bounds check
            if (WouldExitBounds(axis: 1, current, delta, minY, maxY))
            {
                _velocity.y *= -1f;
                delta.y = _velocity.y * (effectiveSpeed * Time.deltaTime);
            }

            transform.position = current + delta;
        }

        bool WouldExitBounds(int axis, Vector3 origin, Vector3 delta, float min, float max)
        {
            if (bounceReferences == null || bounceReferences.Count == 0) return false;

            foreach (var tracked in bounceReferences)
            {
                if (!tracked) continue;

                Vector3 predicted = tracked.position + delta;
                float value = axis == 0 ? predicted.x : predicted.y;

                if (value < min || value > max)
                {
                    return true;
                }
            }

            return false;
        }

        bool TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (areaCorners == null || areaCorners.Length == 0) return false;

            bool initialized = false;

            foreach (var corner in areaCorners)
            {
                if (!corner) continue;

                Vector3 pos = corner.position;
                if (!initialized)
                {
                    minX = maxX = pos.x;
                    minY = maxY = pos.y;
                    initialized = true;
                }
                else
                {
                    minX = Mathf.Min(minX, pos.x);
                    maxX = Mathf.Max(maxX, pos.x);
                    minY = Mathf.Min(minY, pos.y);
                    maxY = Mathf.Max(maxY, pos.y);
                }
            }

            return initialized;
        }

        void OnDrawGizmosSelected()
        {
            if (!TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)) return;

            Gizmos.color = Color.cyan;
            float z = transform.position.z;
            Vector3 bl = new(minX, minY, z);
            Vector3 br = new(maxX, minY, z);
            Vector3 tl = new(minX, maxY, z);
            Vector3 tr = new(maxX, maxY, z);

            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}