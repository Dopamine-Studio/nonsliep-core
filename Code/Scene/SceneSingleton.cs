using UnityEngine;

namespace Nonsliep.Core.Scene
{
    /// <summary>
    /// Base class for MonoBehaviours that should persist across scenes while keeping only one live instance.
    /// </summary>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        public static T Instance { get; private set; }
        public static bool Exists => Instance != null;

        private void Awake()
        {
            var self = (T)this;

            if (Instance == null)
            {
                RegisterSingleton(self);
                return;
            }

            if (ReferenceEquals(Instance, this))
            {
                return;
            }

            HandleDuplicate();
        }

        private void RegisterSingleton(T self)
        {
            Instance = self;

            if (ShouldForceToRoot && transform.parent != null)
            {
                transform.SetParent(null);
            }

            if (ShouldPersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonReady();
        }

        private void HandleDuplicate()
        {
            if (ShouldLogDuplicates)
            {
                Debug.Log($"[{typeof(T).Name}] Duplicate instance on '{gameObject.name}' (scene '{gameObject.scene.name}') destroyed.");
            }

            OnDuplicateFound();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
            {
                OnSingletonDestroyed();
                Instance = null;
            }
        }

        protected virtual void OnSingletonReady() { }
        protected virtual void OnSingletonDestroyed() { }
        protected virtual void OnDuplicateFound() { }
        protected virtual bool ShouldPersistAcrossScenes => true;
        protected virtual bool ShouldForceToRoot => false;
        protected virtual bool ShouldLogDuplicates => false;
    }
}