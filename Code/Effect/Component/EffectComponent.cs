using System;
using System.Collections.Generic;
using UnityEngine;
using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;
using UnityEngine.UI;
using UISwitcher;

namespace Nonsliep.Core.Effect
{
    [DisallowMultipleComponent]
    public class EffectComponent : MonoBehaviour, IEffectReceiver, IEffectPayloadPreparer
    {
        public EffectSet baseSet;
        public List<EffectEntryConfig> overrides = new();

        [SerializeField] private LifecycleTriggers lifecycle = new();
        [SerializeField] private UITriggers uiTriggers = new();

        private Dictionary<EffectType, EffectSet.Entry> _cache;
        private Dictionary<EffectType, PlacementConfig> _placementCache;
        private bool _hasQueuedPayload;
        private bool _uiListenersAttached;
        private bool _pointerActive;
        private bool _valueChangedWhilePointerActive;

        private void Awake()
        {
            BuildCache();
            PlayLifecycle(lifecycle.onAwake);
        }

        private void OnValidate()
        {
            BuildCache();
        }

        private void OnEnable()
        {
            if (!_uiListenersAttached)
            {
                AttachUiListeners();
            }
            if (_hasQueuedPayload)
            {
                // Payload will be consumed in ReceiveEffect; skip default playback.
                return;
            }
            PlayLifecycle(lifecycle.onEnable);
        }

        private void OnDisable()
        {
            DetachUiListeners();
            PlayLifecycle(lifecycle.onDisable);
        }

        private void OnDestroy()
        {
            PlayLifecycle(lifecycle.onDestroy);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<EffectType, EffectSet.Entry>();
            _placementCache = null;

            if (overrides != null && overrides.Count > 0)
            {
                _placementCache = new Dictionary<EffectType, PlacementConfig>();
                foreach (var ov in overrides)
                {
                    if (ov == null) continue;
                    if (ov.HasAnyAssets)
                    {
                        _cache[ov.key] = ov.ToEntry();
                    }
                    _placementCache[ov.key] = ov.placement;
                }

                if (_placementCache.Count == 0)
                {
                    _placementCache = null;
                }
            }
        }

        private EffectSet.Entry Resolve(EffectType key)
        {
            if (_cache != null && _cache.TryGetValue(key, out var e) && e != null) return e;
            if (baseSet != null)
            {
                var fromSet = baseSet.Get(key);
                if (fromSet != null) return fromSet;
            }
            return null;
        }

        private PlacementConfig ResolvePlacementConfig(EffectType key)
        {
            if (_placementCache != null && _placementCache.TryGetValue(key, out var cfg))
            {
                return cfg;
            }

            return PlacementConfig.DefaultSelf;
        }

        public void Play(EffectType key, in EffectContext ctx)
        {
            var setEntry = Resolve(key);
            if (setEntry == null) return;

            var placement = ResolvePlacementConfig(key);
            var useCtx = ApplySfxConfig(setEntry, placement, ctx);

            if (setEntry.sfx != null)
            {
                Effects.Play(setEntry.sfx, useCtx);
            }
            if (setEntry.vfx != null)
            {
                PlayVfxWithConfig(setEntry.vfx, useCtx, placement);
            }
            if (setEntry.haptics != null)
            {
                Effects.PlayHaptics(setEntry.haptics, 1f);
            }
        }

        public void ReceiveEffect(in EffectPayload payload)
        {
            _hasQueuedPayload = false;
            var ctx = payload.ResolveContext();
            if (payload.items == null || payload.items.Length == 0) return;

            for (int i = 0; i < payload.items.Length; i++)
            {
                var item = payload.items[i];
                if (item.HasOverrides)
                {
                    PlayOverrideItem(item, ctx);
                }
                if (item.HasKeys && !item.SuppressDefault)
                {
                    for (int k = 0; k < item.keys.Length; k++)
                    {
                        Play(item.keys[k], ctx);
                    }
                }
            }
        }

        private static void PlayOverrideItem(in EffectPayloadItem item, in EffectContext ctx)
        {
            if (item.bundleOverride != null)
            {
                Effects.Play(item.bundleOverride, ctx);
            }

            if (item.sfxOverride != null)
            {
                Effects.Play(item.sfxOverride, ctx);
            }

            if (item.vfxOverride != null)
            {
                Effects.Play(item.vfxOverride, ctx);
            }

            if (item.hapticsOverride != null)
            {
                Effects.PlayHaptics(item.hapticsOverride, 1f);
            }
        }

        public void PrepareEffect(in EffectPayload payload)
        {
            if (!payload.HasAnyData)
            {
                return;
            }
            _hasQueuedPayload = true;
        }

        private void PlayLifecycle(LifecycleTriggers.Trigger trigger)
        {
            if (trigger == null || !trigger.enabled)
            {
                return;
            }

            Play(trigger.key, default);
        }

        private void AttachUiListeners()
        {
            if (uiTriggers == null) return;

            if (uiTriggers.button.enabled)
            {
                var button = uiTriggers.button.ResolveButton(gameObject);
                if (button != null)
                {
                    button.onClick.AddListener(OnButtonClicked);
                    _uiListenersAttached = true;
                }
            }

            if (uiTriggers.toggle.enabled)
            {
                var toggle = uiTriggers.toggle.ResolveToggle(gameObject);
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(OnToggleChanged);
                    _uiListenersAttached = true;
                }
                else
                {
                    var nullableToggle = uiTriggers.toggle.ResolveNullableToggle(gameObject);
                    if (nullableToggle != null)
                    {
                        nullableToggle.onValueChanged.AddListener(OnNullableToggleChanged);
                        _uiListenersAttached = true;
                    }
                }
            }

            if (uiTriggers.slider.enabled)
            {
                var slider = uiTriggers.slider.ResolveSlider(gameObject);
                if (slider != null)
                {
                    slider.onValueChanged.AddListener(OnSliderChanged);
                    _uiListenersAttached = true;
                }
            }

            if (uiTriggers.NeedsPointerHook)
            {
                var pointerHook = uiTriggers.pointer.ResolveHook(gameObject);
                if (pointerHook != null)
                {
                    pointerHook.PointerDown += OnPointerDown;
                    pointerHook.PointerUp += OnPointerUp;
                    pointerHook.DragEnd += OnPointerDragEnd;
                    _uiListenersAttached = true;
                }
            }
        }

        private void DetachUiListeners()
        {
            if (!_uiListenersAttached || uiTriggers == null) return;

            if (uiTriggers.button.cachedButton != null)
            {
                uiTriggers.button.cachedButton.onClick.RemoveListener(OnButtonClicked);
            }

            if (uiTriggers.toggle.cachedToggle != null)
            {
                uiTriggers.toggle.cachedToggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
            if (uiTriggers.toggle.cachedNullableToggle != null)
            {
                uiTriggers.toggle.cachedNullableToggle.onValueChanged.RemoveListener(OnNullableToggleChanged);
            }

            if (uiTriggers.slider.cachedSlider != null)
            {
                uiTriggers.slider.cachedSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (uiTriggers.pointer.cachedHook != null)
            {
                uiTriggers.pointer.cachedHook.PointerDown -= OnPointerDown;
                uiTriggers.pointer.cachedHook.PointerUp -= OnPointerUp;
                uiTriggers.pointer.cachedHook.DragEnd -= OnPointerDragEnd;
            }

            _uiListenersAttached = false;
        }

        private void OnButtonClicked()
        {
            if (!uiTriggers.button.enabled || !uiTriggers.button.playOnClick) return;
            Play(uiTriggers.button.clickEffect, default);
        }

        private void OnToggleChanged(bool isOn)
        {
            if (!uiTriggers.toggle.enabled) return;
            if (isOn && uiTriggers.toggle.playOnOn)
            {
                Play(uiTriggers.toggle.onEffect, default);
            }
            else if (!isOn && uiTriggers.toggle.playOnOff)
            {
                Play(uiTriggers.toggle.offEffect, default);
            }
        }

        private void OnNullableToggleChanged(bool isOn)
        {
            OnToggleChanged(isOn);
        }

        private void OnSliderChanged(float value)
        {
            if (!uiTriggers.slider.enabled) return;
            if (_pointerActive)
            {
                _valueChangedWhilePointerActive = true;
            }
            if (uiTriggers.slider.playOnValueChanged)
            {
                Play(uiTriggers.slider.changeEffect, default);
            }
        }

        private void OnPointerDown()
        {
            bool setPointerTracking = false;

            if (uiTriggers.pointer.enabled)
            {
                if (uiTriggers.pointer.playOnPointerDown)
                {
                    Play(uiTriggers.pointer.pointerDownEffect, default);
                }
                if (uiTriggers.pointer.NeedsTracking)
                {
                    setPointerTracking = true;
                }
            }

            if (uiTriggers.button.enabled)
            {
                if (uiTriggers.button.playOnPointerDown)
                {
                    Play(uiTriggers.button.pointerDownEffect, default);
                    setPointerTracking = true;
                }
                if (uiTriggers.button.playOnPointerUp)
                {
                    setPointerTracking = true;
                }
            }

            if (uiTriggers.slider.enabled)
            {
                if (uiTriggers.slider.playOnPointerDown)
                {
                    Play(uiTriggers.slider.pointerDownEffect, default);
                    setPointerTracking = true;
                }
                if (uiTriggers.slider.RequiresPointerTracking)
                {
                    setPointerTracking = true;
                }
            }

            if (setPointerTracking)
            {
                _pointerActive = true;
                _valueChangedWhilePointerActive = false;
            }
        }

        private void OnPointerUp()
        {
            bool played = false;

            if (uiTriggers.pointer.enabled && uiTriggers.pointer.playOnPointerUp)
            {
                if (!uiTriggers.pointer.requireValueChangeBeforePointerUp || _valueChangedWhilePointerActive)
                {
                    Play(uiTriggers.pointer.pointerUpEffect, default);
                    played = true;
                }
            }

            if (uiTriggers.button.enabled && uiTriggers.button.playOnPointerUp)
            {
                Play(uiTriggers.button.pointerUpEffect, default);
                played = true;
            }

            if (uiTriggers.slider.enabled && uiTriggers.slider.playOnPointerUp)
            {
                if (!uiTriggers.slider.requireValueChangeBeforePointerUp || _valueChangedWhilePointerActive)
                {
                    Play(uiTriggers.slider.pointerUpEffect, default);
                    played = true;
                }
            }

            if (_pointerActive || played)
            {
                _pointerActive = false;
                _valueChangedWhilePointerActive = false;
            }
        }

        private void OnPointerDragEnd()
        {
            if (uiTriggers.pointer.enabled && uiTriggers.pointer.playOnDragEnd)
            {
                if (!uiTriggers.pointer.requireValueChangeBeforeDragEnd || _valueChangedWhilePointerActive)
                {
                    Play(uiTriggers.pointer.pointerDragEndEffect, default);
                }
            }

            if (_pointerActive)
            {
                _pointerActive = false;
                _valueChangedWhilePointerActive = false;
            }
        }

        [Serializable]
        private class LifecycleTriggers
        {
            public Trigger onAwake = new();
            public Trigger onEnable = new();
            public Trigger onDisable = new();
            public Trigger onDestroy = new();

            [Serializable]
            public class Trigger
            {
                public bool enabled = false;
                public EffectType key = EffectType.Success;
            }
        }

        [Serializable]
        private class UITriggers
        {
            public ButtonTrigger button = new();
            public ToggleTrigger toggle = new();
            public SliderTrigger slider = new();
            public PointerTrigger pointer = new();

            public bool NeedsPointerHook => button.RequiresPointerHook || slider.RequiresPointerHook || pointer.NeedsTracking;

            [Serializable]
            public class ButtonTrigger
            {
                public bool enabled = false;
                public bool playOnClick = true;
                public EffectType clickEffect = EffectType.UIClick;
                public bool playOnPointerDown = false;
                public EffectType pointerDownEffect = EffectType.UIClick;
                public bool playOnPointerUp = false;
                public EffectType pointerUpEffect = EffectType.UIConfirm;
                [NonSerialized] public Button cachedButton;

                public Button ResolveButton(GameObject owner)
                {
                    if (cachedButton != null) return cachedButton;
                    cachedButton = owner.GetComponent<Button>();
                    return cachedButton;
                }

                public bool RequiresPointerHook => enabled && (playOnPointerDown || playOnPointerUp);
            }

            [Serializable]
            public class ToggleTrigger
            {
                public bool enabled = false;
                public bool playOnOn = true;
                public EffectType onEffect = EffectType.UIToggleOn;
                public bool playOnOff = true;
                public EffectType offEffect = EffectType.UIToggleOff;
                [NonSerialized] public Toggle cachedToggle;
                [NonSerialized] public UINullableToggle cachedNullableToggle;

                public Toggle ResolveToggle(GameObject owner)
                {
                    if (cachedToggle != null) return cachedToggle;
                    cachedToggle = owner.GetComponent<Toggle>();
                    return cachedToggle;
                }

                public UINullableToggle ResolveNullableToggle(GameObject owner)
                {
                    if (cachedNullableToggle != null) return cachedNullableToggle;
                    cachedNullableToggle = owner.GetComponent<UINullableToggle>();
                    return cachedNullableToggle;
                }
            }

            [Serializable]
            public class SliderTrigger
            {
                public bool enabled = false;
                public bool playOnPointerDown = false;
                public EffectType pointerDownEffect = EffectType.UISliderStart;
                public bool playOnPointerUp = false;
                public EffectType pointerUpEffect = EffectType.UISliderEnd;
                public bool requireValueChangeBeforePointerUp = false;
                public bool playOnValueChanged = true;
                public EffectType changeEffect = EffectType.UISliderStart;
                [NonSerialized] public Slider cachedSlider;

                public Slider ResolveSlider(GameObject owner)
                {
                    if (cachedSlider != null) return cachedSlider;
                    cachedSlider = owner.GetComponent<Slider>();
                    return cachedSlider;
                }

                public bool RequiresPointerHook => enabled && (playOnPointerDown || playOnPointerUp || requireValueChangeBeforePointerUp);
                public bool RequiresPointerTracking => RequiresPointerHook;
            }

            [Serializable]
            public class PointerTrigger
            {
                public bool enabled = false;
                public bool playOnPointerDown = false;
                public EffectType pointerDownEffect = EffectType.UIClick;
                public bool playOnPointerUp = false;
                public EffectType pointerUpEffect = EffectType.UIConfirm;
                public bool requireValueChangeBeforePointerUp = false;
                public bool playOnDragEnd = false;
                public EffectType pointerDragEndEffect = EffectType.UISliderEnd;
                public bool requireValueChangeBeforeDragEnd = false;
                [NonSerialized] public UIPointerHook cachedHook;

                public UIPointerHook ResolveHook(GameObject owner)
                {
                    if (cachedHook != null) return cachedHook;
                    cachedHook = owner.GetComponent<UIPointerHook>();
                    if (cachedHook == null)
                    {
                        cachedHook = owner.AddComponent<UIPointerHook>();
                    }
                    return cachedHook;
                }

                public bool NeedsTracking => enabled && (playOnPointerDown || playOnPointerUp || playOnDragEnd || requireValueChangeBeforePointerUp || requireValueChangeBeforeDragEnd);
            }
        }

        private static void PlayOverrides(in EffectPayload payload, in EffectContext ctx) { }

        private EffectContext ApplySfxConfig(EffectSet.Entry entry, PlacementConfig config, in EffectContext ctx)
        {
            if (entry == null || entry.sfx == null)
            {
                return ctx;
            }

            if (ctx.sfx.HasValue)
            {
                var existing = ctx.sfx.Value;
                if (existing.anchor != null || existing.position.HasValue)
                {
                    return ctx;
                }
            }

            var target = config.GetPrimaryTarget(transform);
            if (target == null)
            {
                return ctx;
            }

            return config.follow
                ? OverrideSfxContext(ctx, target, null)
                : OverrideSfxContext(ctx, null, target.position);
        }

        private void PlayVfxWithConfig(VFXAsset vfx, in EffectContext ctx, PlacementConfig config)
        {
            if (ctx.vfx.HasValue && (ctx.vfx.Value.anchor != null || ctx.vfx.Value.position.HasValue))
            {
                Effects.Play(vfx, ctx);
                return;
            }

            var targets = config.ResolveTargets(transform);
            int played = 0;
            if (targets != null && targets.Length > 0)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    if (target == null) continue;
                    played++;
                    var playCtx = config.follow
                        ? OverrideVfxContext(ctx, target, null)
                        : OverrideVfxContext(ctx, null, target.position);
                    Effects.Play(vfx, playCtx);
                }
            }

            if (played == 0 && config.mode != AttachmentMode.PayloadOnly)
            {
                var fallback = transform;
                var playCtx = config.follow
                    ? OverrideVfxContext(ctx, fallback, null)
                    : OverrideVfxContext(ctx, null, fallback.position);
                Effects.Play(vfx, playCtx);
            }
        }

        private static EffectContext OverrideSfxContext(in EffectContext baseCtx, Transform anchor, Vector3? position)
        {
            if (baseCtx.sfx.HasValue)
            {
                var existing = baseCtx.sfx.Value;
                return new EffectContext(
                    new EffectContext.SFXContext(anchor ?? existing.anchor, position ?? existing.position, existing.groupOverride, existing.volumeMul, existing.pitchMul),
                    baseCtx.vfx);
            }

            return new EffectContext(new EffectContext.SFXContext(anchor, position), baseCtx.vfx);
        }

        private static EffectContext OverrideVfxContext(in EffectContext baseCtx, Transform anchor, Vector3? position)
        {
            if (baseCtx.vfx.HasValue)
            {
                var existing = baseCtx.vfx.Value;
                return new EffectContext(
                    baseCtx.sfx,
                    new EffectContext.VFXContext(anchor ?? existing.anchor, position ?? existing.position));
            }

            return new EffectContext(baseCtx.sfx, new EffectContext.VFXContext(anchor, position));
        }

        [Serializable]
        public class EffectEntryConfig
        {
            public EffectType key = EffectType.Success;
            public SFXAsset sfx;
            public VFXAsset vfx;
            public HapticPreset haptics;
            public PlacementConfig placement = PlacementConfig.DefaultSelf;

            public EffectSet.Entry ToEntry()
            {
                return new EffectSet.Entry
                {
                    key = key,
                    sfx = sfx,
                    vfx = vfx,
                    haptics = haptics
                };
            }

            public bool HasAnyAssets => sfx != null || vfx != null || haptics != null;
        }

        [Serializable]
        public struct PlacementConfig
        {
            public AttachmentMode mode;
            public Transform[] targets;
            public bool follow;

            public static PlacementConfig DefaultSelf => new PlacementConfig
            {
                mode = AttachmentMode.Self,
                targets = Array.Empty<Transform>(),
                follow = true
            };

            public Transform GetPrimaryTarget(Transform owner)
            {
                switch (mode)
                {
                    case AttachmentMode.Custom:
                        if (targets != null)
                        {
                            for (int i = 0; i < targets.Length; i++)
                            {
                                if (targets[i] != null) return targets[i];
                            }
                        }
                        return owner;
                    case AttachmentMode.PayloadOnly:
                        return null;
                    default:
                        return owner;
                }
            }

            public Transform[] ResolveTargets(Transform owner)
            {
                if (mode == AttachmentMode.PayloadOnly)
                {
                    return Array.Empty<Transform>();
                }

                if (mode == AttachmentMode.Custom)
                {
                    return targets ?? Array.Empty<Transform>();
                }

                return new[] { owner };
            }
        }

        public enum AttachmentMode
        {
            Self,
            Custom,
            PayloadOnly
        }
    }
}
