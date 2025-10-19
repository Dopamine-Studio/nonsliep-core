// TweenLibrary.cs
using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace Nonsliep.Core.Tween
{
    public static class TweenLibrary
    {
        // ----------------------------
        // 공통 헬퍼
        // ----------------------------
        public static T SetEaseAndUpdate<T>(this T tween, Ease ease, bool useUnscaledTime) where T : DG.Tweening.Tween
        {
            if (ease != Ease.Unset)
            {
                tween.SetEase(ease);
            }

            if (useUnscaledTime)
            {
                tween.SetUpdate(true);
            }

            return tween;
        }

        public static Sequence NewSequence(bool useUnscaledTime)
        {
            Sequence s = DOTween.Sequence();
            if (useUnscaledTime)
            {
                s.SetUpdate(true);
            }
            return s;
        }

        // ----------------------------
        // Transform / RectTransform 계열
        // ----------------------------
        public static Sequence PopIn(RectTransform rt, CanvasGroup cg, float fromScale, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (rt != null)
            {
                rt.localScale = Vector3.one * fromScale;

                DG.Tweening.Tween s1 = rt.DOScale(1f, duration);
                if (ease != Ease.Unset)
                {
                    s1.SetEase(ease);
                }
                else
                {
                    s1.SetEase(Ease.OutBack);
                }

                if (useUnscaledTime)
                {
                    s1.SetUpdate(true);
                }

                seq.Join(s1);
            }

            if (cg != null)
            {
                cg.alpha = 0f;
                DG.Tweening.Tween a = cg.DOFade(1f, duration * 0.6f).SetEase(Ease.OutQuad);
                if (useUnscaledTime)
                {
                    a.SetUpdate(true);
                }
                seq.Join(a);
            }

            return seq;
        }

        public static Sequence PopOut(RectTransform rt, CanvasGroup cg, float toScale, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (rt != null)
            {
                DG.Tweening.Tween s1 = rt.DOScale(toScale, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(s1);
            }

            if (cg != null)
            {
                DG.Tweening.Tween a = cg.DOFade(0f, duration * 0.8f).SetEase(Ease.InQuad);
                if (useUnscaledTime)
                {
                    a.SetUpdate(true);
                }
                seq.Join(a);
            }

            return seq;
        }

        public static Sequence Fade(CanvasGroup cg, float toAlpha, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (cg != null)
            {
                DG.Tweening.Tween a = cg.DOFade(toAlpha, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(a);
            }

            return seq;
        }

        public static Sequence SlideAnchored(RectTransform rt, Vector2 from, Vector2 to, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (rt != null)
            {
                rt.anchoredPosition = from;
                DG.Tweening.Tween t = rt.DOAnchorPos(to, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence SlideByAnchored(RectTransform rt, Vector2 origin, Vector2 delta, float duration, Ease ease, bool useUnscaledTime)
        {
            return SlideAnchored(rt, origin + delta, origin, duration, ease, useUnscaledTime);
        }

        public static Sequence SlideOutByAnchored(RectTransform rt, Vector2 origin, Vector2 delta, float duration, Ease ease, bool useUnscaledTime)
        {
            return SlideAnchored(rt, origin, origin + delta, duration, ease, useUnscaledTime);
        }

        public static Sequence MoveAnchored(RectTransform rt, Vector2 initial, Vector3 target, bool relative, AnimationCurve curve, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (rt == null)
            {
                return seq;
            }

            Vector2 from = initial;
            Vector2 to = relative ? from + new Vector2(target.x, target.y) : new Vector2(target.x, target.y);

            rt.anchoredPosition = from;

            float progress = 0f;
            AnimationCurve curveToUse = (curve != null && curve.length >= 2) ? curve : null;

            DG.Tweening.Tween t = DOTween.To(() => progress, value => progress = value, 1f, Mathf.Max(0.0001f, duration));
            t.SetEaseAndUpdate(ease, useUnscaledTime);
            t.OnUpdate(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eval);
            });
            t.OnKill(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eval);
            });

            seq.Join(t);

            return seq;
        }

        public static Sequence Rotate(Transform tr, Quaternion initialLocalRotation, Quaternion initialWorldRotation, Vector3 targetEuler, bool relative, bool useLocal, AnimationCurve curve, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr == null)
            {
                return seq;
            }

            Quaternion from = useLocal ? initialLocalRotation : initialWorldRotation;
            Quaternion to = relative ? from * Quaternion.Euler(targetEuler) : Quaternion.Euler(targetEuler);

            if (useLocal)
            {
                tr.localRotation = from;
            }
            else
            {
                tr.rotation = from;
            }

            float progress = 0f;
            AnimationCurve curveToUse = (curve != null && curve.length >= 2) ? curve : null;

            DG.Tweening.Tween t = DOTween.To(() => progress, value => progress = value, 1f, Mathf.Max(0.0001f, duration));
            t.SetEaseAndUpdate(ease, useUnscaledTime);
            t.OnUpdate(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                Quaternion rot = Quaternion.SlerpUnclamped(from, to, eval);
                if (useLocal)
                {
                    tr.localRotation = rot;
                }
                else
                {
                    tr.rotation = rot;
                }
            });
            t.OnKill(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                Quaternion rot = Quaternion.SlerpUnclamped(from, to, eval);
                if (useLocal)
                {
                    tr.localRotation = rot;
                }
                else
                {
                    tr.rotation = rot;
                }
            });

            seq.Join(t);

            return seq;
        }

        public static Sequence Move(Transform tr, Vector3 initialLocalPosition, Vector3 initialWorldPosition, Vector3 target, bool relative, bool useLocal, AnimationCurve curve, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr == null)
            {
                return seq;
            }

            Vector3 from = useLocal ? initialLocalPosition : initialWorldPosition;
            Vector3 to = relative ? from + target : target;

            if (useLocal)
            {
                tr.localPosition = from;
            }
            else
            {
                tr.position = from;
            }

            float progress = 0f;
            AnimationCurve curveToUse = (curve != null && curve.length >= 2) ? curve : null;

            DG.Tweening.Tween t = DOTween.To(() => progress, value => progress = value, 1f, Mathf.Max(0.0001f, duration));
            t.SetEaseAndUpdate(ease, useUnscaledTime);
            t.OnUpdate(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                Vector3 pos = Vector3.LerpUnclamped(from, to, eval);
                if (useLocal)
                {
                    tr.localPosition = pos;
                }
                else
                {
                    tr.position = pos;
                }
            });
            t.OnKill(() =>
            {
                float eval = Mathf.Clamp01(progress);
                if (curveToUse != null)
                {
                    eval = curveToUse.Evaluate(eval);
                }

                Vector3 pos = Vector3.LerpUnclamped(from, to, eval);
                if (useLocal)
                {
                    tr.localPosition = pos;
                }
                else
                {
                    tr.position = pos;
                }
            });

            seq.Join(t);

            return seq;
        }

        public static Sequence PathMove(RectTransform rt, Vector2 origin, TweenPath path, bool clampToOrigin, bool endAtOrigin, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (rt != null && path != null && path.PointCount >= 2)
            {
                path.RebuildCache();

                float progress = 0f;
                Vector2 firstPoint = path.Evaluate(0f);
                Vector2 finalPoint = path.Evaluate(1f);

                float canvasScale = 1f;
                Canvas canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                    if (rootCanvas != null && rootCanvas.renderMode != RenderMode.WorldSpace)
                    {
                        canvasScale = Mathf.Approximately(rootCanvas.scaleFactor, 0f) ? 1f : rootCanvas.scaleFactor;
                    }
                }

                Vector2 ApplyCanvasScale(Vector2 value)
                {
                    if (Mathf.Approximately(canvasScale, 1f))
                    {
                        return value;
                    }

                    return value / canvasScale;
                }

                Vector2 AdjustOffset(Vector2 raw)
                {
                    if (clampToOrigin)
                    {
                        raw -= firstPoint;
                    }

                    if (endAtOrigin)
                    {
                        Vector2 finalAdjusted = clampToOrigin ? (finalPoint - firstPoint) : finalPoint;
                        raw -= finalAdjusted;
                    }

                    return raw;
                }

                Vector2 startOffset = ApplyCanvasScale(AdjustOffset(firstPoint));
                rt.anchoredPosition = origin + startOffset;

                DG.Tweening.Tween t = DOTween.To(() => progress, value => progress = value, 1f, Mathf.Max(0.0001f, duration));

                t.SetEaseAndUpdate(ease, useUnscaledTime);
                t.OnUpdate(() =>
                {
                    float clampedProgress = Mathf.Clamp01(progress);
                    Vector2 offset = ApplyCanvasScale(AdjustOffset(path.Evaluate(clampedProgress)));
                    rt.anchoredPosition = origin + offset;
                });
                t.OnKill(() =>
                {
                    Vector2 finalOffset = ApplyCanvasScale(AdjustOffset(path.Evaluate(progress >= 1f ? 1f : Mathf.Clamp01(progress))));
                    rt.anchoredPosition = origin + finalOffset;
                });

                seq.Join(t);
            }

            return seq;
        }

        public static Sequence PunchScale(Transform tr, float strength, int vibrato, float elasticity, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr != null)
            {
                DG.Tweening.Tween t = tr.DOPunchScale(Vector3.one * strength, duration, vibrato, elasticity).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        // ----------------------------
        // 3D/월드 공용 (원하면 확장)
        // ----------------------------
        public static Sequence Move(Transform tr, Vector3 to, float duration, Ease ease, bool useUnscaledTime, bool local = false)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr != null)
            {
                DG.Tweening.Tween t = local
                    ? tr.DOLocalMove(to, duration)
                    : tr.DOMove(to, duration);

                t.SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence Scale(Transform tr, Vector3 to, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr != null)
            {
                DG.Tweening.Tween t = tr.DOScale(to, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence Rotate(Transform tr, Vector3 toEuler, float duration, Ease ease, bool useUnscaledTime, RotateMode mode = RotateMode.Fast)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tr != null)
            {
                DG.Tweening.Tween t = tr.DORotate(toEuler, duration, mode).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        // ----------------------------
        // Color 트윈 (UI/2D/3D)
        // ----------------------------
        public static Sequence Color(Image img, Color toColor, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (img != null)
            {
                DG.Tweening.Tween t = img.DOColor(toColor, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence Color(TextMeshProUGUI tmp, Color toColor, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (tmp != null)
            {
                DG.Tweening.Tween t = tmp.DOColor(toColor, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence Color(SpriteRenderer sr, Color toColor, float duration, Ease ease, bool useUnscaledTime)
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (sr != null)
            {
                DG.Tweening.Tween t = sr.DOColor(toColor, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }

        public static Sequence Color(Renderer r, Color toColor, float duration, Ease ease, bool useUnscaledTime, string colorProperty = "_Color")
        {
            Sequence seq = NewSequence(useUnscaledTime);

            if (r != null && r.material != null)
            {
                // 기본적으로 표준 쉐이더의 _Color를 사용. 필요 시 프로퍼티 이름 지정 가능.
                DG.Tweening.Tween t = r.material.DOColor(toColor, colorProperty, duration).SetEaseAndUpdate(ease, useUnscaledTime);
                seq.Join(t);
            }

            return seq;
        }
    }
}