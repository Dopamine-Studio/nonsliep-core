using System;
using System.Collections;
using UnityEngine;
using Nonsliep.Core.Settings;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Effect.Haptics
{
    public interface IHapticsDriver
    {
        void Play(HapticPreset preset, float scale = 1f);
    }

    internal sealed class NoopHapticsDriver : IHapticsDriver
    {
        public void Play(HapticPreset preset, float scale = 1f) { }
    }

    // Very lightweight mobile driver that uses Handheld.Vibrate as approximation.
    internal sealed class MobileHapticsDriver : IHapticsDriver
    {
        private readonly MonoBehaviour _coroutineHost;
        private static bool _settingErrorLogged;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _androidVibrator;
        private AndroidJavaObject _androidActivity;
        private AndroidJavaClass _androidVibrationEffectClass;
        private int _androidSdkLevel = -1;
        private bool _androidInitAttempted;
#endif

        public MobileHapticsDriver(MonoBehaviour host) { _coroutineHost = host; }

        public void Play(HapticPreset preset, float scale = 1f)
        {
            if (preset == null) return;

            // Respect Settings.Haptics if available
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
            if (!enabled) return;

            float intensity = Mathf.Clamp01(preset.intensity * scale);
            if (intensity <= 0f) return;

            float duration = Mathf.Max(0.02f, preset.duration);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (TryPlayAdvancedAndroid(preset, intensity, duration))
            {
                return;
            }
#endif

            // On other platforms (or if Android advanced haptics failed), approximate via pulses.
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_coroutineHost != null && _coroutineHost.isActiveAndEnabled && _coroutineHost.gameObject.activeInHierarchy)
            {
                _coroutineHost.StartCoroutine(FallbackPattern(preset, intensity, duration));
            }
            else
            {
                Handheld.Vibrate();
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool TryPlayAdvancedAndroid(HapticPreset preset, float intensity, float durationSeconds)
        {
            if (!EnsureAndroidVibrator()) return false;
            if (_androidVibrator == null) return false;

            try
            {
                long durationMs = Math.Max(20, Mathf.RoundToInt(durationSeconds * 1000f));

                if (preset.kind == HapticKind.LongLow || preset.kind == HapticKind.LongHigh)
                {
                    // Build a waveform with repeated pulses to approximate a long buzz.
                    int pulseCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds / 0.12f));
                    long onMs = Math.Max(30, Mathf.RoundToInt(durationMs / (pulseCount * 1.8f)));
                    long offMs = Math.Max(20, Mathf.RoundToInt(onMs * 0.6f));

                    if (_androidSdkLevel >= 26 && _androidVibrationEffectClass != null)
                    {
                        var timings = new long[pulseCount * 2 + 1];
                        var amplitudes = new int[timings.Length];
                        timings[0] = 0;
                        amplitudes[0] = 0;

                        int amplitude = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(40f, 255f, intensity)), 1, 255);
                        for (int i = 0; i < pulseCount; i++)
                        {
                            int onIndex = i * 2 + 1;
                            int offIndex = onIndex + 1;
                            timings[onIndex] = onMs;
                            amplitudes[onIndex] = amplitude;
                            timings[offIndex] = offMs;
                            amplitudes[offIndex] = 0;
                        }

                        var effect = _androidVibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", new object[] { timings, amplitudes, -1 });
                        _androidVibrator.Call("vibrate", effect);
                        return true;
                    }

                    // Legacy devices: fall back to a single vibrate scaled by total duration.
                    long legacyDuration = Math.Max(40, onMs * pulseCount);
                    _androidVibrator.Call("vibrate", legacyDuration);
                    return true;
                }
                else
                {
                    if (_androidSdkLevel >= 26 && _androidVibrationEffectClass != null)
                    {
                        int amplitude = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(30f, 255f, intensity)), 1, 255);
                        var effect = _androidVibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude);
                        _androidVibrator.Call("vibrate", effect);
                        return true;
                    }

                    long legacyDuration = Math.Max(15, Mathf.RoundToInt(durationMs * Mathf.Lerp(0.5f, 1.4f, intensity)));
                    _androidVibrator.Call("vibrate", legacyDuration);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptics] Android vibrate failed: {ex.Message}");
            }

            return false;
        }

        private bool EnsureAndroidVibrator()
        {
            if (_androidInitAttempted) return _androidVibrator != null;
            _androidInitAttempted = true;

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _androidActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (_androidActivity == null) return false;

                _androidVibrator = _androidActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (_androidVibrator == null) return false;

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _androidSdkLevel = version.GetStatic<int>("SDK_INT");

                if (_androidSdkLevel >= 26)
                {
                    _androidVibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Haptics] Android init failed: {ex.Message}");
                _androidVibrator = null;
            }

            return _androidVibrator != null;
        }
#endif

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private IEnumerator FallbackPattern(HapticPreset preset, float intensity, float durationSeconds)
        {
            int pulses = preset.kind switch
            {
                HapticKind.Click => 1,
                HapticKind.Success => Mathf.Clamp(Mathf.RoundToInt(1f + intensity), 1, 2),
                HapticKind.Warning => Mathf.Clamp(Mathf.RoundToInt(1f + intensity), 2, 3),
                HapticKind.Error => Mathf.Clamp(Mathf.RoundToInt(2f + intensity), 2, 4),
                HapticKind.ImpactLight => 1,
                HapticKind.ImpactMedium => 2,
                HapticKind.ImpactHeavy => 3,
                HapticKind.LongLow => Mathf.Max(2, Mathf.RoundToInt(2f + intensity * 2f)),
                HapticKind.LongHigh => Mathf.Max(3, Mathf.RoundToInt(2f + intensity * 3f)),
                _ => 1
            };

            float on = Mathf.Clamp(durationSeconds / Mathf.Max(1f, pulses) * 0.7f, 0.02f, 0.15f);
            float off = Mathf.Clamp(on * 0.6f, 0.01f, 0.12f);

            for (int i = 0; i < pulses; i++)
            {
                Handheld.Vibrate();
                if (i < pulses - 1)
                {
                    yield return new WaitForSecondsRealtime(on + off);
                }
            }
        }
#endif
    }
}
