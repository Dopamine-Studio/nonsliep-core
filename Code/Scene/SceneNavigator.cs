using System.Collections;
using System.Collections.Generic;
using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nonsliep.Core.Scene
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9990)]
    public class SceneNavigator : SceneSingleton<SceneNavigator>
    {
        public enum TransitionMode
        {
            None,
            Fade,
            EasyTransitions
        }

        private readonly Stack<string> _history = new Stack<string>();
        private bool _navigatingBack;
        private bool _resetOnNextLoad;
        private bool _isLoading;
        private Coroutine _loadRoutine;

        [Header("Loading Behaviour")]
        [SerializeField] private bool allowConcurrentLoads;

        [Header("Transition Mode")]
        [SerializeField] private TransitionMode transitionMode = TransitionMode.EasyTransitions;

        [Header("Fade Settings")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeOutDuration = 0.35f;
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private bool fadeIgnoreTimeScale = true;

        [Header("Easy Transition Settings")]
        [SerializeField] private TransitionManager transitionManager;
        [SerializeField] private TransitionSettings defaultTransitionSettings;
        [SerializeField] private float transitionStartDelay;

        public event System.Action<string> SceneLoadStarted;
        public event System.Action<string, float> SceneLoadProgress;
        public event System.Action<string> SceneLoadCompleted;

        public bool CanGoBack => _history.Count > 1;
        public bool IsLoading => _isLoading;

        protected override bool ShouldForceToRoot => true;

        private bool _transitionSubscribed;
        private bool _transitionRequested;
        private string _pendingSceneName;

        protected override void OnSingletonReady()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            var active = SceneManager.GetActiveScene();
            if (active.IsValid())
            {
                _history.Clear();
                _history.Push(active.name);
            }

            switch (transitionMode)
            {
                case TransitionMode.Fade:
                    EnsureFadeCanvas();
                    break;
                case TransitionMode.EasyTransitions:
                    EnsureTransitionManager();
                    break;
            }
        }

        protected override void OnSingletonDestroyed()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _history.Clear();
            _navigatingBack = false;
            _resetOnNextLoad = false;
            _isLoading = false;
            _loadRoutine = null;

            if (_transitionSubscribed && transitionManager != null)
            {
                transitionManager.onTransitionBegin -= HandleTransitionBegin;
                transitionManager.onTransitionCutPointReached -= HandleTransitionCutPoint;
                transitionManager.onTransitionEnd -= HandleTransitionEnd;
                _transitionSubscribed = false;
            }
        }

        public void LoadScene(string sceneName, bool resetHistory = false, TransitionSettings transitionOverride = null, float? transitionDelayOverride = null)
        {
            LoadScene(sceneName, LoadSceneMode.Single, resetHistory, transitionOverride, transitionDelayOverride);
        }

        public void LoadScene(string sceneName, LoadSceneMode mode, bool resetHistory = false, TransitionSettings transitionOverride = null, float? transitionDelayOverride = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (_isLoading && !allowConcurrentLoads)
            {
                Debug.LogWarning("[SceneNavigator] Load requested while another load is in progress.");
                return;
            }

            if (resetHistory && mode == LoadSceneMode.Single)
            {
                _history.Clear();
                _resetOnNextLoad = true;
            }

            _navigatingBack = false;
            BeginSceneLoad(sceneName, mode, transitionOverride, transitionDelayOverride);
        }

        public void GoBack(TransitionSettings transitionOverride = null, float? transitionDelayOverride = null)
        {
            if (!CanGoBack)
            {
                return;
            }

            if (_isLoading && !allowConcurrentLoads)
            {
                Debug.LogWarning("[SceneNavigator] GoBack requested while a load is in progress.");
                return;
            }

            _history.Pop();
            string target = _history.Peek();
            _navigatingBack = true;
            BeginSceneLoad(target, LoadSceneMode.Single, transitionOverride, transitionDelayOverride);
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            if (_resetOnNextLoad)
            {
                _history.Clear();
                _resetOnNextLoad = false;
            }

            if (mode == LoadSceneMode.Single)
            {
                if (_navigatingBack)
                {
                    _navigatingBack = false;
                }
                else if (_history.Count == 0 || _history.Peek() != scene.name)
                {
                    _history.Push(scene.name);
                }
            }

            _isLoading = false;
            _loadRoutine = null;
            _transitionRequested = false;
            _pendingSceneName = null;

            SceneLoadProgress?.Invoke(scene.name, 1f);
            SceneLoadCompleted?.Invoke(scene.name);
        }

        private void BeginSceneLoad(string sceneName, LoadSceneMode mode, TransitionSettings transitionOverride, float? transitionDelayOverride)
        {
            if (transitionMode == TransitionMode.EasyTransitions && mode == LoadSceneMode.Single && TryStartEasyTransition(sceneName, transitionOverride, transitionDelayOverride))
            {
                return;
            }

            bool useFade = transitionMode == TransitionMode.Fade;
            StartCoroutineLoad(sceneName, mode, useFade);
        }

        private bool TryStartEasyTransition(string sceneName, TransitionSettings transitionOverride, float? transitionDelayOverride)
        {
            EnsureTransitionManager();
            if (transitionManager == null)
            {
                return false;
            }

            TransitionSettings settings = transitionOverride != null ? transitionOverride : defaultTransitionSettings;
            if (settings == null)
            {
                Debug.LogWarning("[SceneNavigator] Transition requested but no TransitionSettings assigned.");
                return false;
            }

            if (!allowConcurrentLoads && _transitionRequested)
            {
                Debug.LogWarning("[SceneNavigator] Transition already in progress.");
                return false;
            }

            _isLoading = true;
            _transitionRequested = true;
            _pendingSceneName = sceneName;
            float delay = transitionDelayOverride ?? transitionStartDelay;

            transitionManager.Transition(sceneName, settings, delay);
            return true;
        }

        private void StartCoroutineLoad(string sceneName, LoadSceneMode mode, bool useFade)
        {
            if (!allowConcurrentLoads && _loadRoutine != null)
            {
                StopCoroutine(_loadRoutine);
            }

            _loadRoutine = StartCoroutine(LoadSceneRoutine(sceneName, mode, useFade));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, bool useFade)
        {
            _isLoading = true;

            if (useFade)
            {
                EnsureFadeCanvas();
                yield return FadeOut();
            }

            SceneLoadStarted?.Invoke(sceneName);

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, mode);
            if (loadOp == null)
            {
                _isLoading = false;
                _loadRoutine = null;
                yield break;
            }

            while (!loadOp.isDone)
            {
                SceneLoadProgress?.Invoke(sceneName, loadOp.progress);
                yield return null;
            }

            if (useFade)
            {
                yield return FadeIn();
            }

            _isLoading = false;
            _loadRoutine = null;
        }

        private void EnsureFadeCanvas()
        {
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                fadeCanvasGroup.interactable = false;
            }
        }

        private IEnumerator FadeOut()
        {
            if (fadeCanvasGroup == null || fadeOutDuration <= 0f)
            {
                yield break;
            }

            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.interactable = false;

            float elapsed = 0f;
            float startAlpha = fadeCanvasGroup.alpha;
            while (elapsed < fadeOutDuration)
            {
                elapsed += fadeIgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float eval = fadeCurve != null ? fadeCurve.Evaluate(t) : t;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, eval);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeIn()
        {
            if (fadeCanvasGroup == null || fadeInDuration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = fadeCanvasGroup.alpha;
            while (elapsed < fadeInDuration)
            {
                elapsed += fadeIgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float eval = fadeCurve != null ? fadeCurve.Evaluate(t) : t;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eval);
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }

        private void EnsureTransitionManager()
        {
            if (transitionManager == null)
            {
                transitionManager = FindFirstObjectByType<TransitionManager>(FindObjectsInactive.Include);
            }

            if (transitionManager != null && !_transitionSubscribed)
            {
                transitionManager.onTransitionBegin += HandleTransitionBegin;
                transitionManager.onTransitionCutPointReached += HandleTransitionCutPoint;
                transitionManager.onTransitionEnd += HandleTransitionEnd;
                _transitionSubscribed = true;

                if (transitionManager.gameObject.scene.rootCount != 0)
                {
                    DontDestroyOnLoad(transitionManager.gameObject);
                }
            }
        }

        private void HandleTransitionBegin()
        {
            if (_transitionRequested && !string.IsNullOrEmpty(_pendingSceneName))
            {
                SceneLoadStarted?.Invoke(_pendingSceneName);
            }
        }

        private void HandleTransitionCutPoint()
        {
            if (_transitionRequested && !string.IsNullOrEmpty(_pendingSceneName))
            {
                SceneLoadProgress?.Invoke(_pendingSceneName, 0.5f);
            }
        }

        private void HandleTransitionEnd()
        {
            if (!_transitionRequested)
            {
                return;
            }

            _transitionRequested = false;
            _pendingSceneName = null;
            _isLoading = false;
        }
    }
}