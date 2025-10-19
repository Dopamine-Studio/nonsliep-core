using System.Collections.Generic;
using EasyTransition;
using Nonsliep.Core.Effect;
using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using Nonsliep.Core.Scene;

namespace Nonsliep.Core.UI
{
    /// <summary>
    /// Configurable click actions for UI buttons: toggling active states, spawning prefabs,
    /// navigating scenes, and triggering UnityEvents. Designed for quick setup in the Inspector.
    /// </summary>

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIButtonActions : MonoBehaviour
    {
        [SerializeField] private ActivationOptions activation = new();
        [SerializeField] private SpawnOptions spawnOptions = new();
        [SerializeField] private CloseDestroyOptions closeOptions = new();
        [SerializeField] private NavigationOptions navigation = new();
        [SerializeField] private EffectPayloadOptions effectPayload = new();

        [Header("Events")]
        [SerializeField] public UnityEvent onClick;

        private Button _button;
        private EffectPayload _pendingEffectPayload;
        private bool _hasPendingEffectPayload;
        private EffectPayload _defaultEffectPayload;
        private bool _hasDefaultEffectPayload;
        private bool _navigationEnabled = true;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
            RefreshInteractable();
            RefreshEffectPayloadFromConfig();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshInteractable();
            RefreshEffectPayloadFromConfig();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnValidate()
        {
            RefreshEffectPayloadFromConfig();
        }

        private void HandleClick()
        {
            ApplyActivation(activation.activateTargets, true);
            ApplyActivation(activation.deactivateTargets, false);
            ApplyToggle(activation.toggleTargets);
            ApplySpawns(spawnOptions.prefabs);
            ApplyDestroy(closeOptions.destroyTargets);
            HandleSelf();

            bool navigated = false;

            if (_navigationEnabled)
            {
                navigated = TryNavigate();
            }

            onClick?.Invoke();
            RefreshInteractable();
            if (_hasPendingEffectPayload)
            {
                ClearEffectPayload();
            }
        }

        public void SetNavigationEnabled(bool enabled)
        {
            _navigationEnabled = enabled;
            RefreshInteractable();
        }

        public bool ExecuteNavigation()
        {
            return TryNavigate();
        }

        public void SetNavigationScene(string sceneName)
        {
            navigation.loadSceneName = sceneName;
            navigation.loadSceneAsNewRoot = false;
        }

        public void ResetNavigationScene()
        {
            navigation.loadSceneName = string.Empty;
        }

        public void ConfigureGoBack(bool enabled)
        {
            navigation.goBack = enabled;
            RefreshInteractable();
        }

        public bool HasPendingEffectPayload => _hasPendingEffectPayload;

        public void SetEffectPayload(in EffectPayload payload)
        {
            _pendingEffectPayload = payload;
            _hasPendingEffectPayload = true;
        }

        public void ClearEffectPayload()
        {
            _pendingEffectPayload = default;
            _hasPendingEffectPayload = false;
        }

        private EffectPayload GetPayloadForDispatch()
        {
            if (_hasPendingEffectPayload)
            {
                return _pendingEffectPayload;
            }

            return _hasDefaultEffectPayload ? _defaultEffectPayload : EffectPayload.Empty;
        }

        private bool TryNavigate()
        {
            bool navigated = false;

            if (navigation.goBack && SceneNavigator.Exists && SceneNavigator.Instance.CanGoBack)
            {
                SceneNavigator.Instance.GoBack(navigation.goBackTransition, navigation.goBackTransitionDelay);
                navigated = true;
            }

            if (!navigated && !string.IsNullOrEmpty(navigation.loadSceneName))
            {
                if (SceneNavigator.Exists)
                {
                    SceneNavigator.Instance.LoadScene(navigation.loadSceneName, navigation.loadSceneAsNewRoot, navigation.loadSceneTransition, navigation.loadSceneTransitionDelay);
                }
                else
                {
                    SceneManager.LoadScene(navigation.loadSceneName);
                }

                navigated = true;
            }

            return navigated;
        }

        private void RefreshInteractable()
        {
            if (!navigation.goBack || !navigation.disableIfCantGoBack || _button == null)
            {
                return;
            }

            if (SceneNavigator.Exists)
            {
                _button.interactable = SceneNavigator.Instance.CanGoBack;
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            RefreshInteractable();
        }

        private void ApplyActivation(List<GameObject> targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            var payload = GetPayloadForDispatch();

            for (int i = 0; i < targets.Count; i++)
            {
                GameObject target = targets[i];
                if (target == null)
                {
                    continue;
                }

                if (active)
                {
                    bool wasInactive = !target.activeSelf;
                    EffectDispatcher.Dispatch(target, payload, activateTarget: wasInactive);
                }
                else if (target.activeSelf)
                {
                    target.SetActive(false);
                }
            }
        }

        private void ApplyToggle(List<GameObject> targets)
        {
            if (targets == null)
            {
                return;
            }

            var payload = GetPayloadForDispatch();

            for (int i = 0; i < targets.Count; i++)
            {
                GameObject target = targets[i];
                if (target == null)
                {
                    continue;
                }

                bool newState = !target.activeSelf;
                if (newState)
                {
                    EffectDispatcher.Dispatch(target, payload, activateTarget: true);
                }
                else
                {
                    target.SetActive(false);
                }
            }
        }

        private void ApplySpawns(List<PrefabSpawn> prefabs)
        {
            if (prefabs == null)
            {
                return;
            }

            var payload = GetPayloadForDispatch();

            for (int i = 0; i < prefabs.Count; i++)
            {
                var instance = prefabs[i]?.Spawn();
                if (instance != null)
                {
                    EffectDispatcher.Dispatch(instance, payload, activateTarget: false);
                }
            }
        }

        private void RefreshEffectPayloadFromConfig()
        {
            if (effectPayload == null || !effectPayload.enabled || !effectPayload.HasAnyData)
            {
                _defaultEffectPayload = default;
                _hasDefaultEffectPayload = false;
                return;
            }

            var built = effectPayload.Build(transform);
            _defaultEffectPayload = built;
            _hasDefaultEffectPayload = built.HasAnyData;
        }

        private static void ApplyDestroy(List<GameObject> targets)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                GameObject target = targets[i];
                if (target != null)
                {
                    Object.Destroy(target);
                }
            }
        }

        private void HandleSelf()
        {
            GameObject self = closeOptions.selfOverride != null ? closeOptions.selfOverride : gameObject;

            if (closeOptions.destroySelf && self != null)
            {
                Object.Destroy(self);
                return;
            }

            if (closeOptions.deactivateSelf && self != null)
            {
                self.SetActive(false);
            }
        }

        [System.Serializable]
        private class PrefabSpawn
        {
            [SerializeField] private GameObject prefab;
            [SerializeField] private Transform parent;

            [System.NonSerialized] private GameObject _runtimeInstance;

            public GameObject Spawn()
            {
                if (prefab == null)
                {
                    return null;
                }

                if (_runtimeInstance == null)
                {
                    _runtimeInstance = InstantiatePrefab();
                }
                else
                {
                    ReviveExisting();
                }

                return _runtimeInstance;
            }

            private GameObject InstantiatePrefab()
            {
                var instance = Object.Instantiate(prefab);

                if (parent != null)
                {
                    instance.transform.SetParent(parent, false);
                }

                instance.SetActive(true);

                return instance;
            }

            private void ReviveExisting()
            {
                if (_runtimeInstance == null)
                {
                    return;
                }

                if (parent != null && _runtimeInstance.transform.parent != parent)
                {
                    _runtimeInstance.transform.SetParent(parent, false);
                }

                if (!_runtimeInstance.activeSelf)
                {
                    _runtimeInstance.SetActive(true);
                }
            }
        }

        [System.Serializable]
        private class ActivationOptions
        {
            public List<GameObject> activateTargets = new();
            public List<GameObject> deactivateTargets = new();
            public List<GameObject> toggleTargets = new();
        }

        [System.Serializable]
        private class SpawnOptions
        {
            public List<PrefabSpawn> prefabs = new();
        }

        [System.Serializable]
        private class CloseDestroyOptions
        {
            public bool deactivateSelf;
            public bool destroySelf;
            public GameObject selfOverride;
            public List<GameObject> destroyTargets = new();
        }

        [System.Serializable]
        private class NavigationOptions
        {
            public bool goBack;
            public bool disableIfCantGoBack = true;
            public string loadSceneName;
            public bool loadSceneAsNewRoot;
            public TransitionSettings loadSceneTransition;
            public float loadSceneTransitionDelay;
            public TransitionSettings goBackTransition;
            public float goBackTransitionDelay;
        }

        [System.Serializable]
        private class EffectPayloadOptions
        {
            public bool enabled = false;
            public KeyOptions key = new();
            public ContextOptions context = new();
            public OverrideOptions overrides = new();

            public bool HasAnyData => true;

            public EffectPayload Build(Transform owner)
            {
                var ctxNullable = context.Build(owner);
                var keys = BuildKeysArray();
                var item = new EffectPayloadItem(
                    keys,
                    overrides.sfxOverride,
                    overrides.vfxOverride,
                    overrides.hapticsOverride,
                    overrides.bundleOverride);
                return new EffectPayload(new[] { item }, ctxNullable);
            }

            private EffectType[] BuildKeysArray()
            {
                if (key.additionalKeys == null || key.additionalKeys.Count == 0)
                {
                    return new[] { key.key };
                }

                var list = new List<EffectType>(1 + key.additionalKeys.Count);
                list.Add(key.key);
                list.AddRange(key.additionalKeys);
                return list.ToArray();
            }

            [System.Serializable]
            public class KeyOptions
            {
                public EffectType key = EffectType.Success;
                public List<EffectType> additionalKeys = new();
                public bool HasData => true;
            }

            [System.Serializable]
            public class ContextOptions
            {
                public SfxOptions sfx = new();
                public VfxOptions vfx = new();

                public bool HasData => sfx.HasData || vfx.HasData;

                public EffectContext? Build(Transform owner)
                {
                    bool hasCtx = false;
                    EffectContext ctx = default;

                    if (sfx.HasData)
                    {
                        Vector3? pos = sfx.overridePosition ? (Vector3?)sfx.position : null;
                        float? vol = sfx.overrideVolume ? (float?)sfx.volumeMul : null;
                        float? pitch = sfx.overridePitch ? (float?)sfx.pitchMul : null;
                        ctx = EffectContext.ForSFX(
                            sfx.anchor != null ? sfx.anchor : owner,
                            pos,
                            sfx.groupOverride,
                            vol,
                            pitch);
                        hasCtx = true;
                    }

                    if (vfx.HasData)
                    {
                        Vector3? pos = vfx.overridePosition ? (Vector3?)vfx.position : null;
                        if (hasCtx)
                        {
                            ctx = ctx.WithVFX(vfx.anchor != null ? vfx.anchor : owner, pos);
                        }
                        else
                        {
                            ctx = EffectContext.ForVFX(vfx.anchor != null ? vfx.anchor : owner, pos);
                            hasCtx = true;
                        }
                    }

                    return hasCtx ? ctx : (EffectContext?)null;
                }

                [System.Serializable]
                public class SfxOptions
                {
                    public Transform anchor;
                    public bool overridePosition = false;
                    public Vector3 position = Vector3.zero;
                    public AudioMixerGroup groupOverride;
                    public bool overrideVolume = false;
                    [Range(0f, 2f)] public float volumeMul = 1f;
                    public bool overridePitch = false;
                    [Range(0.1f, 3f)] public float pitchMul = 1f;

                    public bool HasData => anchor != null || overridePosition || groupOverride != null || overrideVolume || overridePitch;
                }

                [System.Serializable]
                public class VfxOptions
                {
                    public Transform anchor;
                    public bool overridePosition = false;
                    public Vector3 position = Vector3.zero;

                    public bool HasData => anchor != null || overridePosition;
                }
            }

            [System.Serializable]
            public class OverrideOptions
            {
                public SFXAsset sfxOverride;
                public VFXAsset vfxOverride;
                public HapticPreset hapticsOverride;
                public EffectBundle bundleOverride;

                public bool HasData => sfxOverride != null || vfxOverride != null || hapticsOverride != null || bundleOverride != null;
            }
        }
    }
}