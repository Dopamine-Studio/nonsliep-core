using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Settings
{
    /// <summary>
    /// Drop this on any Slider to keep it in sync with a float setting.
    /// - Initializes the slider from saved settings on enable
    /// - Applies slider edits back into the settings hub
    /// - Optionally enforces the SettingFloatAttribute range on the slider
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SettingFloatBinder : MonoBehaviour
    {
        [SerializeField] private SettingId settingId = SettingId.MasterVolume;
        [SerializeField] private Slider slider;
        [SerializeField] private bool applyRangeFromMetadata = true;

        private void Reset()
        {
            slider = GetComponent<Slider>();
        }

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (applyRangeFromMetadata && slider != null && TryGetFloatMeta(settingId, out var meta))
            {
                slider.minValue = meta.Min;
                slider.maxValue = meta.Max;
            }
        }

        private void OnEnable()
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.AddListener(OnSliderValueChanged);
            SettingsHub.OnFloatChangedId += OnSettingValueChanged;

            slider.SetValueWithoutNotify(SettingsHub.GetFloat(settingId));
        }

        private void OnDisable()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            SettingsHub.OnFloatChangedId -= OnSettingValueChanged;
        }

        private void OnSliderValueChanged(float value)
        {
            SettingsHub.SetFloat(settingId, value);
        }

        private void OnSettingValueChanged(SettingId id, float value)
        {
            if (slider == null || id != settingId)
            {
                return;
            }

            slider.SetValueWithoutNotify(value);
        }

        private static bool TryGetFloatMeta(SettingId id, out SettingFloatAttribute meta)
        {
            meta = null;
            var members = typeof(SettingId).GetMember(id.ToString(), BindingFlags.Public | BindingFlags.Static);
            if (members == null || members.Length == 0)
            {
                return false;
            }

            meta = members[0].GetCustomAttribute<SettingFloatAttribute>();
            return meta != null;
        }
    }
}
