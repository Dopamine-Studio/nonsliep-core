using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Nonsliep.Core.Boot
{
    /// <summary>
    /// Instantiates shared service prefabs once per session and coordinates optional startup tasks
    /// (authentication, settings, etc.) before navigating to the next scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class Bootstrapper : MonoBehaviour
    {
        [Serializable]
        public class FloatEvent : UnityEvent<float> { }

        private static readonly HashSet<GameObject> SpawnedPrefabs = new();
        private static bool s_StartupCompleted;

        [Header("Prefab Spawning")]
        [Tooltip("Spawned once per app session. Order matters if prefabs depend on each other.")]
        [SerializeField] private List<GameObject> prefabs = new();

        [Header("Startup Flow")]
        [Tooltip("Automatically execute tasks on this GameObject and its children once the scene begins.")]
        [SerializeField] private bool runStartupFlowOnStart = true;
        [Tooltip("Wait one frame before executing tasks to give newly-spawned singletons time to initialize.")]
        [SerializeField] private bool waitForNextFrameBeforeTasks = true;

        [Header("Scene Navigation")]
        [Tooltip("Scene name to load after tasks finish.")]
        [SerializeField] private string nextSceneName;
        [Tooltip("Seconds to wait before loading the next scene after tasks wrap up (success or failure).")]
        [SerializeField] private float nextSceneDelaySeconds = 1f;
        [Tooltip("Use unscaled time for the load delay.")]
        [SerializeField] private bool useRealtimeDelay = true;
        [Tooltip("When using SceneNavigator, reset navigation history before loading.")]
        [SerializeField] private bool loadAsNewRoot = true;

        [Header("TargetFPS")]
        [SerializeField] int targetFps = 120;

    private readonly List<Component> _componentBuffer = new();
    private readonly List<IBootstrapTask> _taskBuffer = new();
    private readonly HashSet<IBootstrapTask> _taskSet = new();
        private Coroutine _startupRoutine;
        private Coroutine _pendingLoadRoutine;

        private void Awake()
        {
            SpawnPrefabsOnce();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;
        }

        private void Start()
        {
            if (!runStartupFlowOnStart)
            {
                return;
            }

            RunStartupOnce();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            // 일부 SDK가 재설정하는 경우가 있어 복귀 시 다시 적용
            if (hasFocus) Application.targetFrameRate = targetFps;
        }

        private void SpawnPrefabsOnce()
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (prefab == null || SpawnedPrefabs.Contains(prefab))
                {
                    continue;
                }

                Instantiate(prefab);
                SpawnedPrefabs.Add(prefab);
            }
        }

        private void RunStartupOnce()
        {
            if (s_StartupCompleted)
            {
                BeginSceneDelay();
                return;
            }

            if (_startupRoutine == null)
            {
                _startupRoutine = StartCoroutine(RunStartupFlow());
            }
        }

        private IEnumerator RunStartupFlow()
        {
            if (waitForNextFrameBeforeTasks)
            {
                yield return null;
            }

            CollectTasksFromThisObject();

            for (int i = 0; i < _taskBuffer.Count; i++)
            {
                var task = _taskBuffer[i];
                if (task == null)
                {
                    continue;
                }

                task.ResetState();

                IEnumerator routine = null;
                try
                {
                    routine = task.Execute();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                    continue;
                }

                if (routine == null)
                {
                    continue;
                }

                while (true)
                {
                    bool moveNext;
                    try
                    {
                        moveNext = routine.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, this);
                        break;
                    }

                    if (!moveNext)
                    {
                        break;
                    }

                    yield return routine.Current;
                }
            }

            _startupRoutine = null;
            _componentBuffer.Clear();
            _taskBuffer.Clear();
            _taskSet.Clear();
            s_StartupCompleted = true;

            BeginSceneDelay();
        }

        private void CollectTasksFromThisObject()
        {
            _componentBuffer.Clear();
            _taskBuffer.Clear();
            _taskSet.Clear();

            if (nextSceneDelaySeconds < 0f)
            {
                nextSceneDelaySeconds = 0f;
            }

            GetComponentsInChildren(true, _componentBuffer);

            for (int i = 0; i < _componentBuffer.Count; i++)
            {
                if (_componentBuffer[i] is not IBootstrapTask task)
                {
                    continue;
                }

                if (_taskSet.Add(task))
                {
                    _taskBuffer.Add(task);
                }
            }
        }

        private void BeginSceneDelay()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                return;
            }

            if (_pendingLoadRoutine == null)
            {
                _pendingLoadRoutine = StartCoroutine(LoadNextSceneAfterDelay());
            }
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            if (nextSceneDelaySeconds > 0f)
            {
                if (useRealtimeDelay)
                {
                    yield return new WaitForSecondsRealtime(nextSceneDelaySeconds);
                }
                else
                {
                    yield return new WaitForSeconds(nextSceneDelaySeconds);
                }
            }

            if (Scene.SceneNavigator.Exists)
            {
                Scene.SceneNavigator.Instance.LoadScene(nextSceneName, loadAsNewRoot);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
            }

            _pendingLoadRoutine = null;
        }
    }
}
