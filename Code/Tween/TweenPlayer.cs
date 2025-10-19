// TweenPlayer.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace Nonsliep.Core.Tween
{
    
    [DisallowMultipleComponent]
    [AddComponentMenu("Tween/Tween Player")]
    public class TweenPlayer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum TweenTrigger
        {
            OnAwake,
            OnEnable,
            OnClick,
            OnDisable,
            OnDestroy,
            OnLongPress
        }

        public enum TweenAnimation
        {
            Move,
            Rotate,
            PopIn,
            PopOut,
            FadeIn,
            FadeOut,
            SlideInLeft,
            SlideInRight,
            SlideInUp,
            SlideInDown,
            SlideOutLeft,
            SlideOutRight,
            SlideOutUp,
            SlideOutDown,
            PunchScale,
            ColorTo,
            PathMove,
            TmpCharPulse
        }

        public enum PlayMode
        {
            Sequential,
            Parallel
        }

        [Serializable]
        public class Preset
        {
            [Header("기본 정보")]
            [Tooltip("인스펙터에서 식별용 라벨")]
            public string label = "Default";

            [Tooltip("이 프리셋이 언제 실행될지 선택")]
            public TweenTrigger trigger = TweenTrigger.OnAwake;

            [Tooltip("애니메이션 유형")]
            public TweenAnimation type = TweenAnimation.PopIn;

            [Tooltip("같은 트리거의 다른 프리셋들과의 실행 방식")]
            public PlayMode playMode = PlayMode.Sequential;

            [Space]
            [Header("타이밍")]
            [Min(0f), Tooltip("재생 시간(초)")]
            public float duration = 0.5f;

            [Min(0f), Tooltip("시작 지연(초)")]
            public float delay = 0f;

            [Tooltip("Ease 곡선")]
            public Ease ease = Ease.OutQuad;

            [Space]
            [Header("공통 옵션")]
            [Tooltip("Time.timeScale의 영향을 받지 않도록 Unscaled로 재생")]
            public bool useUnscaledTime = true;

            [Tooltip("*Out 류에서 완료 시 비활성화")]
            public bool deactivateOnComplete = false;

            [Tooltip("실행 전에 초기 상태로 복구")]
            public bool restartFromInitial = true;

            [Space]
            [Header("Slide 옵션")]
            [Tooltip("AnchoredPosition 기준 이동량(+/-)")]
            public Vector2 slideDistance = new Vector2(300f, 0f);

            [Space]
            [Header("Pop 옵션")]
            [Range(0.2f, 2f), Tooltip("PopIn 시작 스케일 / PopOut 목표 스케일")]
            public float popFromScale = 0.85f;

            [Space]
            [Header("Punch 옵션")]
            [Tooltip("DOPunchScale 강도")]
            public float punchStrength = 0.15f;

            [Tooltip("흔들림 횟수")]
            public int punchVibrato = 8;

            [Range(0f, 1f), Tooltip("탄성")]
            public float punchElasticity = 0.6f;

            [Space]
            [Header("Fade/Pop 옵션")]
            [Tooltip("CanvasGroup이 없으면 자동 추가")]
            public bool autoAddCanvasGroup = true;

            [Space]
            [Header("ColorTo 옵션")]
            [Tooltip("목표 색상 (Image/TMP/SpriteRenderer/Renderer 중 우선순위로 자동 적용)")]
            public Color targetColor = Color.white;

            [Tooltip("Renderer(Material) 대상으로 색을 바꿀 때 사용할 색상 프로퍼티명 (기본 _Color)")]
            public string rendererColorProperty = "_Color";

            [Tooltip("ColorTo: 다시 실행 시 초기 색상과 토글")]
            public bool colorToggleWithInitial = false;

            [Space]
            [Header("Rotate 옵션")]
            [Tooltip("목표 회전 (Euler Degrees)")]
            public Vector3 rotateEuler = Vector3.zero;

            [Tooltip("회전을 초기값 기준 상대적으로 적용")]
            public bool rotateRelative = true;

            [Tooltip("로컬 회전 기준으로 적용")]
            public bool rotateUseLocal = true;

            [Tooltip("회전 진행도를 조절할 커브 (0~1)")]
            public AnimationCurve rotateCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            [Space]
            [Header("Move 옵션")]
            [Tooltip("목표 위치 (로컬/월드/Anchored 여부는 옵션으로 결정)")]
            public Vector3 movePosition = Vector3.zero;

            [Tooltip("이동을 초기값 기준 상대적으로 적용")]
            public bool moveRelative = true;

            [Tooltip("로컬 좌표 기준으로 적용")]
            public bool moveUseLocal = true;

            [Tooltip("RectTransform AnchoredPosition을 사용")]
            public bool moveUseAnchoredPosition = false;

            [Tooltip("이동 진행도를 조절할 커브 (0~1)")]
            public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            [Space]
            [Header("PathMove 옵션")]
            [Tooltip("경로 자산. 경로 좌표는 기본적으로 초기 앵커 좌표를 기준으로 한 오프셋")]
            public TweenPath pathAsset;

            [Tooltip("경로의 첫 점을 초기 위치로 강제 맞출지 여부")]
            public bool pathClampToOrigin = false;

            [Tooltip("경로의 끝 점을 시작 지점(Origin)으로 강제 맞출지 여부")]
            public bool pathEndAtOrigin = true;

            [Space]
            [Header("TMP 글자 옵션")]
        [Tooltip("애니메이션 대상 TMP 텍스트에서 사용할 문자 인덱스 목록 (0부터 시작)")]
        public int[] tmpCharIndices = new int[] { 0 };

        [Tooltip("공백/제어 문자를 건너뛴 가시 문자 인덱스 배열을 사용할지 여부")]
            public bool tmpCharUseVisibleIndex = true;

            [Range(0.1f, 4f), Tooltip("확대 시 목표 스케일 배수")]
            public float tmpCharTargetScale = 1.3f;

            [Min(0f), Tooltip("확대 상태를 유지할 시간(초)")]
            public float tmpCharHoldDuration = 0f;

            [Min(0f), Tooltip("원래 크기로 돌아가는 데 걸릴 시간(초). 0이면 duration과 동일")]
            public float tmpCharReturnDuration = 0f;

            [Tooltip("축소 단계에서 사용할 Ease 곡선")]
            public Ease tmpCharReturnEase = Ease.InQuad;

            [Tooltip("재생 직전에 TMP 문자 정보를 강제로 갱신")]
            public bool tmpCharForceRefresh = true;
        }

        [Header("Presets (Append/Join)")]
        public Preset[] presets = new Preset[]
        {
            new Preset()
            {
                label = "Default PopIn",
                trigger = TweenTrigger.OnEnable,
                type = TweenAnimation.PopIn,
                duration = 0.45f,
                ease = Ease.OutBack
            }
        };

        [Header("반복 옵션")]
        [Tooltip("애니메이션 완료 후 동일 트리거를 다시 재생")]
        public bool repeatEnabled = false;

        [Tooltip("반복 시 완료 후 원래 위치로 애니메이션하며 되돌아가기")]
        public bool repeatReturnToInitial = false;

        [Tooltip("반복 횟수 대신 무한 반복")]
        public bool repeatInfinite = false;

        [Min(0), Tooltip("반복 횟수 (0이면 무한 반복)")]
        public int repeatCount = 0;

        [Min(0f), Tooltip("반복 사이 대기 시간(초)")]
        public float repeatDelay = 0f;

        [Tooltip("반복 대기 시간에서도 Time.timeScale 무시")]
        public bool repeatUseUnscaledTime = true;

        [Header("Long Press")]
        [Tooltip("길게 누르기 동작을 활성화합니다.")]
        [SerializeField] private bool enableLongPress = false;

        [Min(0f), Tooltip("길게 누르기로 간주할 최소 누름 시간(초)")]
        [SerializeField] private float longPressDuration = 0.35f;

        [Tooltip("길게 누를 때 Time.timeScale의 영향을 무시합니다.")]
        [SerializeField] private bool longPressUseUnscaledTime = true;

        [Tooltip("길게 누름이 발생하면 클릭 트리거를 막습니다.")]
        [SerializeField] private bool blockClickWhenLongPressed = true;

        [Tooltip("길게 눌렀을 때 특정 이미지를 토글합니다.")]
        [SerializeField] private bool toggleImageOnLongPress = false;

        [Tooltip("토글 대상 Image. 지정하지 않으면 동작하지 않습니다.")]
        [SerializeField] private Image longPressTargetImage;

        [Tooltip("시작 시 토글 대상 이미지를 비활성화합니다.")]
        [SerializeField] private bool longPressTargetStartsHidden = true;

        [Tooltip("Image.enabled 대신 GameObject 활성/비활성으로 토글합니다.")]
        [SerializeField] private bool longPressToggleGameObject = false;

        private RectTransform _rt;
        private CanvasGroup _cg;
        private Button _btn;

        // ColorTo 대상 검색을 빠르게 하기 위해 캐시
        private Image _image;
        private TextMeshProUGUI _tmp;
        private SpriteRenderer _spriteRenderer;
        private Renderer _renderer;

        private bool _hasInitImageColor;
        private bool _hasInitTmpColor;
        private bool _hasInitSpriteRendererColor;
        private Color _initImageColor;
        private Color _initTmpColor;
        private Color _initSpriteRendererColor;
        private Dictionary<string, Color> _initRendererColors;

        private Vector2 _initAnchoredPos;
        private Vector3 _initScale;
        private float _initAlpha = 1f;
        private Quaternion _initLocalRotation;
        private Quaternion _initWorldRotation;
        private Vector3 _initLocalPosition;
        private Vector3 _initWorldPosition;

        private Sequence _runningSeq;
        private DG.Tweening.Tween _repeatDelayTween;
        private TweenTrigger? _repeatActiveTrigger;
        private int _repeatCompletedCount;
        private ResetMask _repeatResetMask;
        private bool _isReturning;
        private bool _isPointerDown;
        private float _pointerDownTime;
        private bool _longPressTriggered;
        private bool _suppressNextClick;

        [Flags]
        private enum ResetMask
        {
            None = 0,
            AnchoredPosition = 1 << 0,
            LocalPosition = 1 << 1,
            WorldPosition = 1 << 2,
            Scale = 1 << 3,
            CanvasAlpha = 1 << 4,
            ImageColor = 1 << 5,
            TmpColor = 1 << 6,
            SpriteRendererColor = 1 << 7,
            RendererColor = 1 << 8,
            LocalRotation = 1 << 9,
            WorldRotation = 1 << 10,
            TmpCharacter = 1 << 11
        }

        private const ResetMask ResetMaskAll =
            ResetMask.AnchoredPosition |
            ResetMask.LocalPosition |
            ResetMask.WorldPosition |
            ResetMask.Scale |
            ResetMask.CanvasAlpha |
            ResetMask.ImageColor |
            ResetMask.TmpColor |
            ResetMask.SpriteRendererColor |
            ResetMask.RendererColor |
            ResetMask.LocalRotation |
            ResetMask.WorldRotation |
            ResetMask.TmpCharacter;

        private const float ColorComparisonTolerance = 0.01f;

        private void Update()
        {
            if (!enableLongPress || !_isPointerDown || _longPressTriggered)
            {
                return;
            }

            float currentTime = longPressUseUnscaledTime ? Time.unscaledTime : Time.time;
            if (currentTime - _pointerDownTime < longPressDuration)
            {
                return;
            }

            _longPressTriggered = true;
            HandleLongPress();
        }

        private void Awake()
        {
            CacheRefs();
            SaveInitialState();
            InitializeLongPressTarget();
            RunByTrigger(TweenTrigger.OnAwake);
        }

        private void OnEnable()
        {
            RunByTrigger(TweenTrigger.OnEnable);
        }

        private void OnDisable()
        {
            if (_runningSeq != null && _runningSeq.IsActive())
            {
                _runningSeq.Kill();
                _runningSeq = null;
            }

            ResetLongPressState(fullReset: true);

            CancelRepeatDelay();
            _repeatActiveTrigger = null;
            _repeatCompletedCount = 0;
            _repeatResetMask = ResetMask.None;
            _isReturning = false;

            // OnDisable 트리거 실행
            RunByTrigger(TweenTrigger.OnDisable);
        }

        private void OnDestroy()
        {
            CancelRepeatDelay();
            _repeatActiveTrigger = null;
            _repeatCompletedCount = 0;
            _repeatResetMask = ResetMask.None;
            _isReturning = false;
            ResetLongPressState(fullReset: true);

            // OnDestroy 트리거 실행
            RunByTrigger(TweenTrigger.OnDestroy);
        }


        private void CacheRefs()
        {
            if (_rt == null)
            {
                _rt = GetComponent<RectTransform>();
            }

            if (_btn == null)
            {
                _btn = GetComponent<Button>();
            }

            _cg = GetComponent<CanvasGroup>();
            _image = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);
            _tmp = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>(true);
            _spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
            _renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>(true);

            if (_btn != null)
            {
                _btn.onClick.RemoveListener(OnClick);
                _btn.onClick.AddListener(OnClick);
            }
        }

        private void SaveInitialState()
        {
            if (_rt != null)
            {
                _initAnchoredPos = _rt.anchoredPosition;
            }
            _initScale = transform.localScale;
            _initAlpha = (_cg != null) ? _cg.alpha : 1f;
            _initLocalRotation = transform.localRotation;
            _initWorldRotation = transform.rotation;
            _initLocalPosition = transform.localPosition;
            _initWorldPosition = transform.position;

            if (_image != null)
            {
                _hasInitImageColor = true;
                _initImageColor = _image.color;
            }

            if (_tmp != null)
            {
                _hasInitTmpColor = true;
                _initTmpColor = _tmp.color;
            }

            if (_spriteRenderer != null)
            {
                _hasInitSpriteRendererColor = true;
                _initSpriteRendererColor = _spriteRenderer.color;
            }

            if (_renderer != null)
            {
                CacheRendererColor("_Color");
            }
        }

        private void ResetToInitial()
        {
            ApplyResetMask(ResetMaskAll);
        }

        private void HandleLongPress()
        {
            if (toggleImageOnLongPress && longPressTargetImage != null)
            {
                ToggleLongPressImage();
            }

            if (blockClickWhenLongPressed)
            {
                _suppressNextClick = true;
            }

            RunByTrigger(TweenTrigger.OnLongPress);
        }

        private void ToggleLongPressImage()
        {
            if (longPressTargetImage == null)
            {
                return;
            }

            GameObject target = longPressTargetImage.gameObject;

            if (longPressToggleGameObject)
            {
                bool nextActive = !target.activeSelf;
                target.SetActive(nextActive);
                if (nextActive && !longPressTargetImage.enabled)
                {
                    longPressTargetImage.enabled = true;
                }
            }
            else
            {
                if (!target.activeSelf)
                {
                    target.SetActive(true);
                }

                longPressTargetImage.enabled = !longPressTargetImage.enabled;
            }
        }

        private void InitializeLongPressTarget()
        {
            if (!enableLongPress)
            {
                return;
            }

            if (!toggleImageOnLongPress || longPressTargetImage == null)
            {
                return;
            }

            if (!longPressTargetStartsHidden)
            {
                return;
            }

            if (longPressToggleGameObject)
            {
                longPressTargetImage.gameObject.SetActive(false);
            }
            else
            {
                longPressTargetImage.enabled = false;
            }
        }

        private void ResetLongPressState(bool fullReset = false)
        {
            _isPointerDown = false;
            _pointerDownTime = 0f;
            _longPressTriggered = false;

            if (fullReset)
            {
                _suppressNextClick = false;
            }
        }

        private void OnClick()
        {
            if (enableLongPress && blockClickWhenLongPressed && _suppressNextClick)
            {
                _suppressNextClick = false;
                return;
            }

            _suppressNextClick = false;
            RunByTrigger(TweenTrigger.OnClick);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enableLongPress)
            {
                return;
            }

            _isPointerDown = true;
            _longPressTriggered = false;
            _pointerDownTime = longPressUseUnscaledTime ? Time.unscaledTime : Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enableLongPress)
            {
                return;
            }

            ResetLongPressState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enableLongPress)
            {
                return;
            }

            ResetLongPressState();
        }

        private void RunByTrigger(TweenTrigger trig, bool isRepeatInvocation = false)
        {
            if (presets == null || presets.Length <= 0)
            {
                return;
            }

            if (_runningSeq != null)
            {
                if (_runningSeq.IsActive())
                {
                    _runningSeq.Kill();
                }
                _runningSeq = null;
            }

            CancelRepeatDelay();
            _isReturning = false;

            _runningSeq = DOTween.Sequence();

            bool hasPreset = false;
            bool deactivateOnComplete = false;
            bool shouldRepeat = ShouldRepeatForTrigger(trig);

            if (!isRepeatInvocation)
            {
                if (shouldRepeat)
                {
                    _repeatActiveTrigger = trig;
                    _repeatCompletedCount = 0;
                }
                else if (_repeatActiveTrigger == trig)
                {
                    _repeatActiveTrigger = null;
                    _repeatCompletedCount = 0;
                }
            }

            if (shouldRepeat && !repeatReturnToInitial)
            {
                _repeatResetMask = ResetMask.None;
            }

            foreach (var p in presets)
            {
                if (p.trigger != trig)
                {
                    continue;
                }

                hasPreset = true;

                if (p.type == TweenAnimation.ColorTo)
                {
                    CacheColorTargets(p);
                }

                ResetMask mask = ResetMask.None;
                if (p.restartFromInitial)
                {
                    mask = GetResetMask(p);
                    ApplyResetMask(mask);
                    if (shouldRepeat && !repeatReturnToInitial)
                    {
                        _repeatResetMask |= mask;
                    }
                }

                // Fade/Pop 류는 CanvasGroup 자동부착 가능
                if (p.type == TweenAnimation.FadeIn || p.type == TweenAnimation.FadeOut || p.type == TweenAnimation.PopIn || p.type == TweenAnimation.PopOut)
                {
                    if (_cg == null && p.autoAddCanvasGroup)
                    {
                        _cg = gameObject.AddComponent<CanvasGroup>();
                        _cg.alpha = _initAlpha;
                    }
                }

                Sequence seq = BuildSequenceFromPreset(p);

                if (seq != null)
                {
                    if (p.delay > 0f)
                    {
                        if (p.playMode == PlayMode.Sequential)
                        {
                            _runningSeq.AppendInterval(p.delay);
                        }
                        else
                        {
                            _runningSeq.Join(DOTween.Sequence().AppendInterval(p.delay));
                        }
                    }

                    if (p.playMode == PlayMode.Sequential)
                    {
                        _runningSeq.Append(seq);
                    }
                    else
                    {
                        _runningSeq.Join(seq);
                    }

                    if (p.deactivateOnComplete)
                    {
                        deactivateOnComplete = true;
                    }
                }
            }

            if (!hasPreset)
            {
                _runningSeq.Kill();
                _runningSeq = null;
                return;
            }

            if (shouldRepeat)
            {
                if (repeatReturnToInitial)
                {
                    AttachRepeatWithReturn(trig, deactivateOnComplete);
                }
                else
                {
                    AttachRepeatRestart(trig, deactivateOnComplete, _repeatResetMask);
                }
            }
            else if (deactivateOnComplete)
            {
                _runningSeq.OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
        }

        private void AttachRepeatRestart(TweenTrigger trig, bool deactivateOnComplete, ResetMask repeatResetMask)
        {
            TweenTrigger repeatTrigger = trig;
            _runningSeq.OnComplete(() =>
            {
                if (!repeatEnabled || _repeatActiveTrigger != repeatTrigger)
                {
                    if (deactivateOnComplete)
                    {
                        gameObject.SetActive(false);
                    }
                    return;
                }

                if (!repeatInfinite && repeatCount > 0 && _repeatCompletedCount + 1 >= repeatCount)
                {
                    _repeatActiveTrigger = null;
                    _repeatCompletedCount = 0;

                    if (deactivateOnComplete)
                    {
                        gameObject.SetActive(false);
                    }
                    return;
                }

                _repeatCompletedCount++;
                ApplyResetMask(repeatResetMask);

                if (repeatDelay > 0f)
                {
                    _repeatDelayTween = DOVirtual.DelayedCall(repeatDelay, () =>
                    {
                        RunByTrigger(repeatTrigger, true);
                    }, repeatUseUnscaledTime);
                }
                else
                {
                    RunByTrigger(repeatTrigger, true);
                }
            });
        }

        private void AttachRepeatWithReturn(TweenTrigger trig, bool deactivateOnComplete)
        {
            TweenTrigger repeatTrigger = trig;
            _runningSeq.SetAutoKill(false);

            _runningSeq.OnComplete(() =>
            {
                if (!repeatEnabled || _repeatActiveTrigger != repeatTrigger)
                {
                    if (deactivateOnComplete)
                    {
                        gameObject.SetActive(false);
                    }
                    return;
                }

                bool hasMore = repeatInfinite || repeatCount <= 0 || _repeatCompletedCount < repeatCount;
                if (!hasMore)
                {
                    CancelRepeatDelay();
                    _repeatActiveTrigger = null;
                    _repeatCompletedCount = 0;
                    _isReturning = false;
                    if (deactivateOnComplete)
                    {
                        gameObject.SetActive(false);
                    }

                    if (_runningSeq != null && _runningSeq.IsActive())
                    {
                        _runningSeq.Kill();
                        _runningSeq = null;
                    }
                    return;
                }

                _repeatCompletedCount++;
                _isReturning = true;
                CancelRepeatDelay();

                if (_runningSeq != null && _runningSeq.IsActive())
                {
                    _runningSeq.PlayBackwards();
                }
            });

            _runningSeq.OnRewind(() =>
            {
                if (!_isReturning || _repeatActiveTrigger != repeatTrigger)
                {
                    return;
                }

                _isReturning = false;

                if (!repeatEnabled)
                {
                    return;
                }

                bool hasMore = repeatInfinite || repeatCount <= 0 || _repeatCompletedCount <= repeatCount;
                if (!hasMore)
                {
                    CancelRepeatDelay();
                    _repeatActiveTrigger = null;
                    _repeatCompletedCount = 0;

                    if (deactivateOnComplete)
                    {
                        gameObject.SetActive(false);
                    }

                    if (_runningSeq != null && _runningSeq.IsActive())
                    {
                        _runningSeq.Kill();
                        _runningSeq = null;
                    }
                    return;
                }

                if (repeatDelay > 0f)
                {
                    _repeatDelayTween = DOVirtual.DelayedCall(repeatDelay, () =>
                    {
                        if (!repeatEnabled || _repeatActiveTrigger != repeatTrigger)
                        {
                            return;
                        }

                        if (_runningSeq != null && _runningSeq.IsActive())
                        {
                            _runningSeq.PlayForward();
                        }
                    }, repeatUseUnscaledTime);
                }
                else if (_runningSeq != null && _runningSeq.IsActive())
                {
                    _runningSeq.PlayForward();
                }
            });
        }

        private bool ShouldRepeatForTrigger(TweenTrigger trig)
        {
            if (!repeatEnabled)
            {
                return false;
            }

            return trig != TweenTrigger.OnDisable && trig != TweenTrigger.OnDestroy;
        }

        private void CacheColorTargets(Preset preset)
        {
            if (_image != null && !_hasInitImageColor)
            {
                _hasInitImageColor = true;
                _initImageColor = _image.color;
            }

            if (_tmp != null && !_hasInitTmpColor)
            {
                _hasInitTmpColor = true;
                _initTmpColor = _tmp.color;
            }

            if (_spriteRenderer != null && !_hasInitSpriteRendererColor)
            {
                _hasInitSpriteRendererColor = true;
                _initSpriteRendererColor = _spriteRenderer.color;
            }

            if (_renderer != null)
            {
                string property = ResolveRendererColorProperty(preset);
                CacheRendererColor(property);
            }
        }

        private void CacheRendererColor(string property)
        {
            if (_renderer == null || _renderer.material == null || string.IsNullOrEmpty(property))
            {
                return;
            }

            _initRendererColors ??= new Dictionary<string, Color>();
            if (_initRendererColors.ContainsKey(property))
            {
                return;
            }

            if (_renderer.material.HasProperty(property))
            {
                _initRendererColors[property] = _renderer.material.GetColor(property);
            }
        }

        private string ResolveRendererColorProperty(Preset preset)
        {
            return string.IsNullOrEmpty(preset.rendererColorProperty) ? "_Color" : preset.rendererColorProperty;
        }

        private bool TryGetRendererInitialColor(string property, out Color color)
        {
            if (_initRendererColors != null && _initRendererColors.TryGetValue(property, out color))
            {
                return true;
            }

            if (_renderer != null && _renderer.material != null && _renderer.material.HasProperty(property))
            {
                color = _renderer.material.GetColor(property);
                return true;
            }

            color = default;
            return false;
        }

        private Color DetermineColorTarget(Preset preset, Color currentColor, Color initialColor, bool hasInitial)
        {
            if (!preset.colorToggleWithInitial || !hasInitial)
            {
                return preset.targetColor;
            }

            return ApproximatelyColor(currentColor, preset.targetColor)
                ? initialColor
                : preset.targetColor;
        }

        private static bool ApproximatelyColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= ColorComparisonTolerance &&
                Mathf.Abs(a.g - b.g) <= ColorComparisonTolerance &&
                Mathf.Abs(a.b - b.b) <= ColorComparisonTolerance &&
                Mathf.Abs(a.a - b.a) <= ColorComparisonTolerance;
        }

        private void CancelRepeatDelay()
        {
            if (_repeatDelayTween != null && _repeatDelayTween.IsActive())
            {
                _repeatDelayTween.Kill();
            }

            _repeatDelayTween = null;
        }

        private ResetMask GetResetMask(Preset preset)
        {
            ResetMask mask = ResetMask.None;

            switch (preset.type)
            {
                case TweenAnimation.PopIn:
                case TweenAnimation.PopOut:
                    mask |= ResetMask.Scale | ResetMask.CanvasAlpha;
                    break;

                case TweenAnimation.FadeIn:
                case TweenAnimation.FadeOut:
                    mask |= ResetMask.CanvasAlpha;
                    break;

                case TweenAnimation.SlideInLeft:
                case TweenAnimation.SlideInRight:
                case TweenAnimation.SlideInUp:
                case TweenAnimation.SlideInDown:
                case TweenAnimation.SlideOutLeft:
                case TweenAnimation.SlideOutRight:
                case TweenAnimation.SlideOutUp:
                case TweenAnimation.SlideOutDown:
                case TweenAnimation.PathMove:
                    mask |= ResetMask.AnchoredPosition;
                    break;

                case TweenAnimation.PunchScale:
                    mask |= ResetMask.Scale;
                    break;

                case TweenAnimation.ColorTo:
                    if (!preset.colorToggleWithInitial)
                    {
                        mask |= ResetMask.ImageColor | ResetMask.TmpColor | ResetMask.SpriteRendererColor | ResetMask.RendererColor;
                    }
                    break;

                case TweenAnimation.TmpCharPulse:
                    mask |= ResetMask.TmpCharacter;
                    break;

                case TweenAnimation.Rotate:
                    mask |= preset.rotateUseLocal ? ResetMask.LocalRotation : ResetMask.WorldRotation;
                    break;

                case TweenAnimation.Move:
                    if (_rt != null && preset.moveUseAnchoredPosition)
                    {
                        mask |= ResetMask.AnchoredPosition;
                    }
                    else
                    {
                        mask |= preset.moveUseLocal ? ResetMask.LocalPosition : ResetMask.WorldPosition;
                    }
                    break;

                default:
                    break;
            }

            return mask;
        }

        private void ApplyResetMask(ResetMask mask)
        {
            if (mask == ResetMask.None)
            {
                return;
            }

            if ((mask & ResetMask.AnchoredPosition) != 0 && _rt != null)
            {
                _rt.anchoredPosition = _initAnchoredPos;
            }

            if ((mask & ResetMask.LocalPosition) != 0)
            {
                transform.localPosition = _initLocalPosition;
            }

            if ((mask & ResetMask.WorldPosition) != 0)
            {
                transform.position = _initWorldPosition;
            }

            if ((mask & ResetMask.LocalRotation) != 0)
            {
                transform.localRotation = _initLocalRotation;
            }

            if ((mask & ResetMask.WorldRotation) != 0)
            {
                transform.rotation = _initWorldRotation;
            }

            if ((mask & ResetMask.Scale) != 0)
            {
                transform.localScale = _initScale;
            }

            if ((mask & ResetMask.CanvasAlpha) != 0 && _cg != null)
            {
                _cg.alpha = _initAlpha;
            }

            if ((mask & ResetMask.ImageColor) != 0 && _hasInitImageColor && _image != null)
            {
                _image.color = _initImageColor;
            }

            if ((mask & ResetMask.TmpColor) != 0 && _hasInitTmpColor && _tmp != null)
            {
                _tmp.color = _initTmpColor;
            }

            if ((mask & ResetMask.SpriteRendererColor) != 0 && _hasInitSpriteRendererColor && _spriteRenderer != null)
            {
                _spriteRenderer.color = _initSpriteRendererColor;
            }

            if ((mask & ResetMask.RendererColor) != 0 && _renderer != null && _renderer.material != null && _initRendererColors != null)
            {
                foreach (KeyValuePair<string, Color> kv in _initRendererColors)
                {
                    if (!string.IsNullOrEmpty(kv.Key) && _renderer.material.HasProperty(kv.Key))
                    {
                        _renderer.material.SetColor(kv.Key, kv.Value);
                    }
                }
            }

            if ((mask & ResetMask.TmpCharacter) != 0)
            {
                ResetTmpCharacters();
            }
        }

        private void ResetTmpCharacters()
        {
            if (_tmp == null)
            {
                return;
            }

            _tmp.ForceMeshUpdate(true, true);
            _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private Sequence BuildSequenceFromPreset(Preset p)
        {
            switch (p.type)
            {
                case TweenAnimation.PopIn:
                {
                    return TweenLibrary.PopIn(_rt, _cg, p.popFromScale, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.PopOut:
                {
                    return TweenLibrary.PopOut(_rt, _cg, p.popFromScale, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.FadeIn:
                {
                    if (_cg != null)
                    {
                        _cg.alpha = 0f;
                    }
                    return TweenLibrary.Fade(_cg, 1f, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.FadeOut:
                {
                    return TweenLibrary.Fade(_cg, 0f, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideInLeft:
                {
                    return TweenLibrary.SlideByAnchored(_rt, _initAnchoredPos, new Vector2(-Mathf.Abs(p.slideDistance.x), 0f), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideInRight:
                {
                    return TweenLibrary.SlideByAnchored(_rt, _initAnchoredPos, new Vector2(+Mathf.Abs(p.slideDistance.x), 0f), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideInUp:
                {
                    return TweenLibrary.SlideByAnchored(_rt, _initAnchoredPos, new Vector2(0f, +Mathf.Abs(p.slideDistance.y)), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideInDown:
                {
                    return TweenLibrary.SlideByAnchored(_rt, _initAnchoredPos, new Vector2(0f, -Mathf.Abs(p.slideDistance.y)), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideOutLeft:
                {
                    return TweenLibrary.SlideOutByAnchored(_rt, _initAnchoredPos, new Vector2(-Mathf.Abs(p.slideDistance.x), 0f), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideOutRight:
                {
                    return TweenLibrary.SlideOutByAnchored(_rt, _initAnchoredPos, new Vector2(+Mathf.Abs(p.slideDistance.x), 0f), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideOutUp:
                {
                    return TweenLibrary.SlideOutByAnchored(_rt, _initAnchoredPos, new Vector2(0f, +Mathf.Abs(p.slideDistance.y)), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.SlideOutDown:
                {
                    return TweenLibrary.SlideOutByAnchored(_rt, _initAnchoredPos, new Vector2(0f, -Mathf.Abs(p.slideDistance.y)), p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.PunchScale:
                {
                    return TweenLibrary.PunchScale(transform, p.punchStrength, p.punchVibrato, p.punchElasticity, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.ColorTo:
                {
                    // 우선순위: Image > TMP > SpriteRenderer > Renderer
                    if (_image != null)
                    {
                        Color current = _image.color;
                        bool hasInitial = _hasInitImageColor;
                        Color initial = hasInitial ? _initImageColor : current;
                        Color toColor = DetermineColorTarget(p, current, initial, hasInitial);
                        return TweenLibrary.Color(_image, toColor, p.duration, p.ease, p.useUnscaledTime);
                    }
                    else if (_tmp != null)
                    {
                        Color current = _tmp.color;
                        bool hasInitial = _hasInitTmpColor;
                        Color initial = hasInitial ? _initTmpColor : current;
                        Color toColor = DetermineColorTarget(p, current, initial, hasInitial);
                        return TweenLibrary.Color(_tmp, toColor, p.duration, p.ease, p.useUnscaledTime);
                    }
                    else if (_spriteRenderer != null)
                    {
                        Color current = _spriteRenderer.color;
                        bool hasInitial = _hasInitSpriteRendererColor;
                        Color initial = hasInitial ? _initSpriteRendererColor : current;
                        Color toColor = DetermineColorTarget(p, current, initial, hasInitial);
                        return TweenLibrary.Color(_spriteRenderer, toColor, p.duration, p.ease, p.useUnscaledTime);
                    }
                    else if (_renderer != null)
                    {
                        string property = ResolveRendererColorProperty(p);
                        if (_renderer.material == null || !_renderer.material.HasProperty(property))
                        {
                            Debug.LogWarning($"[TweenPlayer] Renderer에 '{property}' 색상 프로퍼티가 없습니다. (GameObject: {name})");
                            return null;
                        }

                        Color current = _renderer.material.GetColor(property);
                        bool hasInitial = TryGetRendererInitialColor(property, out Color initial);
                        if (!hasInitial)
                        {
                            initial = current;
                        }

                        Color toColor = DetermineColorTarget(p, current, initial, hasInitial);
                        return TweenLibrary.Color(_renderer, toColor, p.duration, p.ease, p.useUnscaledTime, property);
                    }
                    else
                    {
                        Debug.LogWarning($"[TweenPlayer] ColorTo 대상 컴포넌트를 찾지 못했습니다. (GameObject: {name})");
                        return null;
                    }
                }

                case TweenAnimation.Rotate:
                {
                    return TweenLibrary.Rotate(transform, _initLocalRotation, _initWorldRotation, p.rotateEuler, p.rotateRelative, p.rotateUseLocal, p.rotateCurve, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.Move:
                {
                    if (_rt != null && p.moveUseAnchoredPosition)
                    {
                        return TweenLibrary.MoveAnchored(_rt, _initAnchoredPos, p.movePosition, p.moveRelative, p.moveCurve, p.duration, p.ease, p.useUnscaledTime);
                    }

                    return TweenLibrary.Move(transform, _initLocalPosition, _initWorldPosition, p.movePosition, p.moveRelative, p.moveUseLocal, p.moveCurve, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.PathMove:
                {
                    return TweenLibrary.PathMove(_rt, _initAnchoredPos, p.pathAsset, p.pathClampToOrigin, p.pathEndAtOrigin, p.duration, p.ease, p.useUnscaledTime);
                }

                case TweenAnimation.TmpCharPulse:
                {
                    return BuildTmpCharPulseSequence(p);
                }

                default:
                {
                    return null;
                }
            }
        }

        private Sequence BuildTmpCharPulseSequence(Preset p)
        {
            if (_tmp == null)
            {
                Debug.LogWarning($"[TweenPlayer] TmpCharPulse를 실행할 TMP_Text를 찾지 못했습니다. (GameObject: {name})");
                return null;
            }

            if (p.tmpCharForceRefresh)
            {
                _tmp.ForceMeshUpdate(true, true);
            }
            else
            {
                _tmp.ForceMeshUpdate();
            }

            var textInfo = _tmp.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
            {
                Debug.LogWarning($"[TweenPlayer] TmpCharPulse 대상 문자를 찾을 수 없습니다. (GameObject: {name})");
                return null;
            }

            List<int> resolvedIndices = ResolveTmpCharacterIndices(p, textInfo, out bool hadInvalidRequest);
            if (resolvedIndices.Count == 0)
            {
                if (hadInvalidRequest)
                {
                    Debug.LogWarning($"[TweenPlayer] TmpCharPulse에서 유효한 문자를 찾지 못했습니다. 요청된 인덱스를 확인해주세요. (GameObject: {name})");
                }
                else
                {
                    Debug.LogWarning($"[TweenPlayer] TmpCharPulse로 사용할 문자가 없습니다. (GameObject: {name})");
                }
                return null;
            }

            float targetScale = Mathf.Max(0.01f, p.tmpCharTargetScale);
            float upDuration = Mathf.Max(0f, p.duration);
            float downDuration = p.tmpCharReturnDuration > 0f ? p.tmpCharReturnDuration : upDuration;
            float holdDuration = Mathf.Max(0f, p.tmpCharHoldDuration);

            var seq = DOTween.Sequence();

            var states = new List<TmpCharState>(resolvedIndices.Count);
            foreach (int index in resolvedIndices)
            {
                if (index < 0 || index >= textInfo.characterCount)
                {
                    continue;
                }

                var charInfo = textInfo.characterInfo[index];
                if (!charInfo.isVisible)
                {
                    Debug.LogWarning($"[TweenPlayer] TmpCharPulse 대상 문자가 가시 상태가 아닙니다. (resolvedIndex: {index}, GameObject: {name})");
                    continue;
                }

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length)
                {
                    continue;
                }

                var meshInfo = textInfo.meshInfo[materialIndex];
                if (meshInfo.vertices == null || meshInfo.vertices.Length < vertexIndex + 4 || meshInfo.mesh == null)
                {
                    continue;
                }

                var baseVertices = new Vector3[4];
                Array.Copy(meshInfo.vertices, vertexIndex, baseVertices, 0, 4);

                states.Add(new TmpCharState
                {
                    MaterialIndex = materialIndex,
                    VertexIndex = vertexIndex,
                    BaseVertices = baseVertices,
                    Center = (baseVertices[0] + baseVertices[2]) * 0.5f
                });
            }

            if (states.Count == 0)
            {
                Debug.LogWarning($"[TweenPlayer] TmpCharPulse에서 애니메이션할 유효한 문자를 찾지 못했습니다. (GameObject: {name})");
                return null;
            }

            foreach (var state in states)
            {
                float currentScale = 1f;
                ApplyScale(state, 1f);

                var charSeq = DOTween.Sequence();

                if (upDuration > 0f)
                {
                    var upTween = DOTween.To(() => currentScale, value =>
                    {
                        currentScale = value;
                        ApplyScale(state, value);
                    }, targetScale, upDuration)
                        .SetEase(p.ease)
                        .SetUpdate(p.useUnscaledTime);

                    charSeq.Append(upTween);
                }
                else
                {
                    currentScale = targetScale;
                    ApplyScale(state, currentScale);
                }

                if (holdDuration > 0f)
                {
                    charSeq.AppendInterval(holdDuration);
                }

                if (downDuration > 0f)
                {
                    var downTween = DOTween.To(() => currentScale, value =>
                    {
                        currentScale = value;
                        ApplyScale(state, value);
                    }, 1f, downDuration)
                        .SetEase(p.tmpCharReturnEase)
                        .SetUpdate(p.useUnscaledTime);

                    charSeq.Append(downTween);
                }
                else if (!Mathf.Approximately(currentScale, 1f))
                {
                    currentScale = 1f;
                    ApplyScale(state, currentScale);
                }

                seq.Join(charSeq);
            }

            seq.OnKill(() =>
            {
                if (_tmp == null)
                {
                    return;
                }

                foreach (var state in states)
                {
                    RestoreState(state);
                }
            });

            return seq;

            void ApplyScale(TmpCharState state, float scale)
            {
                if (_tmp == null)
                {
                    return;
                }

                var info = _tmp.textInfo;
                if (state.MaterialIndex < 0 || state.MaterialIndex >= info.meshInfo.Length)
                {
                    return;
                }

                var vertices = info.meshInfo[state.MaterialIndex].vertices;
                if (vertices == null || vertices.Length < state.VertexIndex + 4)
                {
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    vertices[state.VertexIndex + i] = (state.BaseVertices[i] - state.Center) * scale + state.Center;
                }

                var mesh = info.meshInfo[state.MaterialIndex].mesh;
                if (mesh != null)
                {
                    mesh.vertices = vertices;
                    _tmp.UpdateGeometry(mesh, state.MaterialIndex);
                }
            }

            void RestoreState(TmpCharState state)
            {
                if (_tmp == null)
                {
                    return;
                }

                var info = _tmp.textInfo;
                if (state.MaterialIndex < 0 || state.MaterialIndex >= info.meshInfo.Length)
                {
                    return;
                }

                var vertices = info.meshInfo[state.MaterialIndex].vertices;
                if (vertices == null || vertices.Length < state.VertexIndex + 4)
                {
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    vertices[state.VertexIndex + i] = state.BaseVertices[i];
                }

                var mesh = info.meshInfo[state.MaterialIndex].mesh;
                if (mesh != null)
                {
                    mesh.vertices = vertices;
                    _tmp.UpdateGeometry(mesh, state.MaterialIndex);
                }
            }
        }

        private List<int> ResolveTmpCharacterIndices(Preset preset, TMP_TextInfo textInfo, out bool hadInvalidRequest)
        {
            hadInvalidRequest = false;
            var result = new List<int>();

            if (textInfo == null || textInfo.characterCount <= 0)
            {
                return result;
            }

            var requested = preset.tmpCharIndices;
            if (requested == null || requested.Length == 0)
            {
                result.Add(0);
                return result;
            }

            if (preset.tmpCharUseVisibleIndex)
            {
                var visible = new List<int>();
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (textInfo.characterInfo[i].isVisible)
                    {
                        visible.Add(i);
                    }
                }

                for (int i = 0; i < requested.Length; i++)
                {
                    int rawIndex = requested[i];
                    if (rawIndex < 0 || rawIndex >= visible.Count)
                    {
                        hadInvalidRequest = true;
                        continue;
                    }

                    int resolved = visible[rawIndex];
                    if (!result.Contains(resolved))
                    {
                        result.Add(resolved);
                    }
                }
            }
            else
            {
                int maxIndex = textInfo.characterCount - 1;
                for (int i = 0; i < requested.Length; i++)
                {
                    int rawIndex = requested[i];
                    if (rawIndex < 0)
                    {
                        hadInvalidRequest = true;
                        continue;
                    }

                    if (rawIndex > maxIndex)
                    {
                        hadInvalidRequest = true;
                        rawIndex = maxIndex;
                    }

                    if (!result.Contains(rawIndex))
                    {
                        result.Add(rawIndex);
                    }
                }
            }

            return result;
        }

        private struct TmpCharState
        {
            public int MaterialIndex;
            public int VertexIndex;
            public Vector3[] BaseVertices;
            public Vector3 Center;
        }

    }
}
