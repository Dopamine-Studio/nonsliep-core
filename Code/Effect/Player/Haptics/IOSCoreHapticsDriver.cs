#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Nonsliep.Core.Settings;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Effect.Haptics
{
    internal sealed class IOSCoreHapticsDriver : IHapticsDriver
    {
        private static bool _initAttempted;
        private static bool _available;
        private static bool _settingErrorLogged;

        static IOSCoreHapticsDriver()
        {
            Application.quitting += OnApplicationQuitting;
        }

        public void Play(HapticPreset preset, float scale = 1f)
        {
            if (preset == null)
            {
                return;
            }

            bool enabled = true;
            try
            {
                enabled = SettingsHub.GetBool(SettingId.Haptics);
            }
            catch (Exception ex)
            {
                if (!_settingErrorLogged)
                {
                    Debug.LogWarning($"[Haptics] Failed to read setting: {ex.Message}");
                    _settingErrorLogged = true;
                }
            }

            if (!enabled)
            {
                return;
            }

            float intensity = Mathf.Clamp01(preset.intensity * scale);
            if (intensity <= 0f)
            {
                return;
            }

            float duration = Mathf.Max(0.02f, preset.duration);

            if (!EnsureInitialized())
            {
                Handheld.Vibrate();
                return;
            }

            if (!PlayNative(preset.kind, intensity, duration))
            {
                Handheld.Vibrate();
            }
        }

        private static bool EnsureInitialized()
        {
            if (_initAttempted)
            {
                return _available;
            }

            _initAttempted = true;

            try
            {
                _available = D1Haptics_Initialize();
            }
            catch (DllNotFoundException ex)
            {
                Debug.LogWarning($"[Haptics] Native plugin not found: {ex.Message}");
                _available = false;
            }
            catch (EntryPointNotFoundException ex)
            {
                Debug.LogWarning($"[Haptics] Native plugin entry point missing: {ex.Message}");
                _available = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptics] Native init failed: {ex.Message}");
                _available = false;
            }

            return _available;
        }

        private static bool PlayNative(HapticKind kind, float intensity, float duration)
        {
            if (!_available)
            {
                return false;
            }

            try
            {
                return D1Haptics_Play((int)kind, intensity, duration);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptics] Native play failed: {ex.Message}");
                return false;
            }
        }

        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool D1Haptics_Initialize();

        [DllImport("__Internal")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool D1Haptics_Play(int kind, float intensity, float duration);

        [DllImport("__Internal")]
        private static extern void D1Haptics_Shutdown();

        private static void OnApplicationQuitting()
        {
            if (!_initAttempted || !_available)
            {
                return;
            }

            try
            {
                D1Haptics_Shutdown();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptics] Native shutdown failed: {ex.Message}");
            }
        }
    }
}
#endif
