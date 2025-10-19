using UnityEngine;
using Nonsliep.Core.Boot;
using SettingsHub = Nonsliep.Core.Settings.Settings;

namespace Nonsliep.Core.Settings
{
    // 햅틱 설정 모듈: Settings 변경을 구독하고 런타임 SDK 토글만 담당(효과 재생은 별도 클래스).
    public class HapticsSettingApplier : MonoBehaviour, ISettingsApplier
    {
        private bool _enabled = true;

        public void Initialize()
        {
            SettingsHub.OnBoolChangedId += OnBoolChanged;
        }

        public void ApplyInitial()
        {
            bool enabled = SettingsHub.GetBool(SettingId.Haptics);
            Apply(enabled);
        }

        public void Cleanup()
        {
            SettingsHub.OnBoolChangedId -= OnBoolChanged;
        }

        private void OnBoolChanged(SettingId id, bool value)
        {
            if (id != SettingId.Haptics)
            {
                return;
            }
            Apply(value);
        }

        private void Apply(bool enabled)
        {
            _enabled = enabled;
            // 실제 SDK 연동 시 여기서 on/off
        }
    }
}
