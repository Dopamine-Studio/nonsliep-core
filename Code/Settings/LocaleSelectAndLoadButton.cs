using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Nonsliep.Core.UI;

namespace Nonsliep.Core.Settings
{
    [RequireComponent(typeof(Button))]
    public class LocaleSelectAndLoadButton : MonoBehaviour
    {
        public const string PlayerPrefsLocaleKey = "selected_locale_code";

        [Tooltip("Locale code such as ko, en, en-US, ja")]
        public string localeCode = "ko";

        [Tooltip("Store the selected locale code in PlayerPrefs")]
        public bool savePreference = true;

        Button _btn;
        UIButtonActions _buttonActions;
        bool _busy;

        void Awake()
        {
            _btn = GetComponent<Button>();
            _buttonActions = _btn.GetComponent<UIButtonActions>();

            if (_buttonActions == null)
            {
                Debug.LogError("LocaleSelectAndLoadButton requires a UIButtonActions component on the same GameObject.");
                enabled = false;
                return;
            }

            _buttonActions.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (_buttonActions != null)
            {
                _buttonActions.onClick.RemoveListener(OnClick);
            }
        }

        void OnClick()
        {
            if (_busy)
            {
                return;
            }

            StartCoroutine(SwitchLocaleAndLoad());
        }

        IEnumerator SwitchLocaleAndLoad()
        {
            _busy = true;
            if (_btn != null)
            {
                _btn.interactable = false;
            }

            yield return LocalizationSettings.InitializationOperation;

            var identifier = new LocaleIdentifier(localeCode);
            var targetLocale = LocalizationSettings.AvailableLocales.GetLocale(identifier);
            if (targetLocale == null)
            {
                Debug.LogWarning($"LocaleSelectAndLoadButton: Locale '{localeCode}' was not found in AvailableLocales.");
                RestoreButtonState();
                yield break;
            }

            if (LocalizationSettings.SelectedLocale != targetLocale)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
            }

            if (savePreference)
            {
                PlayerPrefs.SetString(PlayerPrefsLocaleKey, localeCode);
                PlayerPrefs.Save();
            }

            RestoreButtonState();
        }

        void RestoreButtonState()
        {
            _busy = false;

            if (_btn != null)
            {
                _btn.interactable = true;
            }
        }
    }
}