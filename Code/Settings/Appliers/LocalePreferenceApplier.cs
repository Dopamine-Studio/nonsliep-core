using System.Collections;
using Nonsliep.Core.Boot;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Nonsliep.Core.Settings
{
    /// <summary>
    /// Applies the persisted locale preference (if any) during boot and keeps PlayerPrefs in sync
    /// with the currently selected locale.
    /// </summary>
    public class LocalePreferenceApplier : MonoBehaviour, ISettingsApplier
    {
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private string defaultLocaleCode = string.Empty;

        Coroutine _applyRoutine;

        public void Initialize()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public void ApplyInitial()
        {
            if (!loadOnStart)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                // Defer until enabled; SettingsInitializer will call again if needed.
                return;
            }

            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
            }

            _applyRoutine = StartCoroutine(ApplyLocaleFromPreference());
        }

        public void Cleanup()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
                _applyRoutine = null;
            }
        }

        private IEnumerator ApplyLocaleFromPreference()
        {
            yield return LocalizationSettings.InitializationOperation;

            string targetCode = PlayerPrefs.GetString(LocaleSelectAndLoadButton.PlayerPrefsLocaleKey, string.Empty);
            if (string.IsNullOrEmpty(targetCode))
            {
                targetCode = defaultLocaleCode;
            }

            Locale targetLocale = null;
            if (!string.IsNullOrEmpty(targetCode))
            {
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(targetCode));
            }

            if (targetLocale == null && !string.IsNullOrEmpty(defaultLocaleCode))
            {
                targetLocale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(defaultLocaleCode));
                if (targetLocale != null)
                {
                    targetCode = defaultLocaleCode;
                }
            }

            if (targetLocale != null && LocalizationSettings.SelectedLocale != targetLocale)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
            }

            if (targetLocale != null && !string.IsNullOrEmpty(targetCode))
            {
                PlayerPrefs.SetString(LocaleSelectAndLoadButton.PlayerPrefsLocaleKey, targetCode);
                PlayerPrefs.Save();
            }

            _applyRoutine = null;
        }

        private static void OnLocaleChanged(Locale locale)
        {
            if (locale == null)
            {
                return;
            }

            PlayerPrefs.SetString(LocaleSelectAndLoadButton.PlayerPrefsLocaleKey, locale.Identifier.Code);
            PlayerPrefs.Save();
        }
    }
}

