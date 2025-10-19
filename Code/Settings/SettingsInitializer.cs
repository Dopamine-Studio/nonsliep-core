using System;
using System.Collections.Generic;
using Nonsliep.Core.Settings;
using UnityEngine;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Settings
{
    // Settings 오케스트레이터: 로드/모듈 초기화/적용/정리 전담
    [DisallowMultipleComponent]
    public class SettingsInitializer : Scene.SceneSingleton<SettingsInitializer>
    {
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool includeChildren = true;

    private readonly List<ISettingsApplier> _modules = new();
    private bool _initialApplyCompleted;

    public bool IsReady => _initialApplyCompleted;
    public static bool HasAppliedInitial => Exists && Instance._initialApplyCompleted;

    public event Action InitialApplicationCompleted;

        protected override bool ShouldPersistAcrossScenes => dontDestroyOnLoad;

        protected override void OnSingletonReady()
        {
            SettingsHub.Load();
            _initialApplyCompleted = false;
            CollectModules();
            InitializeModules();
        }

        private void Start()
        {
            ApplyInitial();
        }

        protected override void OnSingletonDestroyed()
        {
            CleanupModules();
            _initialApplyCompleted = false;
            base.OnSingletonDestroyed();
        }

        private void CollectModules()
        {
            _modules.Clear();

            if (includeChildren)
            {
                GetComponentsInChildren(true, _modules);
            }
            else
            {
                GetComponents(_modules);
            }
        }

        private void InitializeModules()
        {
            foreach (var module in _modules)
            {
                module?.Initialize();
            }
        }

        private void ApplyInitial()
        {
            foreach (var module in _modules)
            {
                module?.ApplyInitial();
            }

            _initialApplyCompleted = true;
            InitialApplicationCompleted?.Invoke();
        }

        private void CleanupModules()
        {
            foreach (var module in _modules)
            {
                module?.Cleanup();
            }

            _modules.Clear();
            _initialApplyCompleted = false;
        }
    }
}
