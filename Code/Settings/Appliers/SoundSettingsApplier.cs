using UnityEngine;
using UnityEngine.Audio;
using Nonsliep.Core.Settings;
using SettingsHub = Nonsliep.Core.Settings.Settings;
using System.Reflection;

namespace Nonsliep.Core.Boot
{
    public class SoundSettingsApplier : MonoBehaviour, ISettingsApplier
    {
        [Header("Audio Mixer Wiring")]
        [SerializeField] private AudioMixer mixer;

        public void Initialize()
        {
            if (mixer != null)
            {
                SettingsHub.OnFloatChangedId += OnFloatChanged;
            }
        }

        public void ApplyInitial()
        {
            if (mixer == null)
            {
                return;
            }

            foreach (SettingId id in System.Enum.GetValues(typeof(SettingId)))
            {
                if (HasFloat(id))
                {
                    OnFloatChanged(id, SettingsHub.GetFloat(id));
                }
            }
        }

        public void Cleanup()
        {
            if (mixer != null)
            {
                SettingsHub.OnFloatChangedId -= OnFloatChanged;
            }
        }

        private void OnFloatChanged(SettingId id, float v)
        {
            if (mixer == null)
            {
                return;
            }

            if (GetAffectsListenerFromAttribute(id))
            {
                AudioListener.volume = Mathf.Clamp01(v);
            }

            string param = ResolveMixerParam(id);
            SetMixerLinear(param, v);
        }

        private void SetMixerLinear(string exposedParam, float linear)
        {
            if (string.IsNullOrEmpty(exposedParam))
            {
                return;
            }
            float db = LinearToDb(linear);
            mixer.SetFloat(exposedParam, db);
        }

        private static float LinearToDb(float linear)
        {
            if (linear <= 0.0001f)
            {
                return -80f; // mute
            }
            return Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        }

        private string ResolveMixerParam(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                var attr = mem[0].GetCustomAttribute<MixerParamAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.Name))
                {
                    return attr.Name;
                }
            }
            return id.ToString();
        }

        private bool GetAffectsListenerFromAttribute(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                var attr = mem[0].GetCustomAttribute<MixerParamAttribute>();
                if (attr != null)
                {
                    return attr.AffectsListener;
                }
            }
            return false;
        }

        private bool HasFloat(SettingId id)
        {
            var mem = typeof(SettingId).GetMember(id.ToString());
            if (mem != null && mem.Length > 0)
            {
                return mem[0].GetCustomAttribute<SettingFloatAttribute>() != null;
            }
            return false;
        }
    }
}

