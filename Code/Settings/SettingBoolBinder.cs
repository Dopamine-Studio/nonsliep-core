using UISwitcher;
using UnityEngine;
using UnityEngine.UI;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Settings
{
    /// <summary>
    /// Binds either a Unity Toggle or UISwitcher toggle to a bool setting.
    /// Drop it on the same GameObject as the UI control and pick the SettingId.
    /// </summary>
    public class SettingBoolBinder : MonoBehaviour
    {
        [SerializeField] private SettingId settingId = SettingId.Haptics;
        [SerializeField] private bool invert;
        [SerializeField] private Toggle toggle;
        [SerializeField] private UINullableToggle nullableToggle;

        private bool UsesNullableToggle => nullableToggle != null;

        private void Reset()
        {
            nullableToggle = GetComponent<UINullableToggle>();
            if (nullableToggle == null)
            {
                toggle = GetComponent<Toggle>();
            }
        }

        private void Awake()
        {
            if (nullableToggle == null)
            {
                nullableToggle = GetComponent<UINullableToggle>();
            }

            if (nullableToggle == null && toggle == null)
            {
                toggle = GetComponent<Toggle>();
            }
        }

        private void OnEnable()
        {
            SettingsHub.OnBoolChangedId += OnSettingValueChanged;

            if (UsesNullableToggle)
            {
                nullableToggle.onValueChanged.AddListener(OnUiValueChanged);
            }
            else if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnUiValueChanged);
            }

            ApplyCurrentValue();
        }

        private void OnDisable()
        {
            SettingsHub.OnBoolChangedId -= OnSettingValueChanged;

            if (UsesNullableToggle)
            {
                nullableToggle.onValueChanged.RemoveListener(OnUiValueChanged);
            }
            else if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnUiValueChanged);
            }
        }

        private void ApplyCurrentValue()
        {
            bool current = SettingsHub.GetBool(settingId);
            bool display = invert ? !current : current;

            if (UsesNullableToggle)
            {
                nullableToggle.SetWithoutNotify(display);
            }
            else if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(display);
            }
        }

        private void OnUiValueChanged(bool value)
        {
            bool toStore = invert ? !value : value;
            SettingsHub.SetBool(settingId, toStore);
        }

        private void OnSettingValueChanged(SettingId id, bool value)
        {
            if (id != settingId)
            {
                return;
            }

            bool display = invert ? !value : value;

            if (UsesNullableToggle)
            {
                nullableToggle.SetWithoutNotify(display);
            }
            else if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(display);
            }
        }
    }
}
