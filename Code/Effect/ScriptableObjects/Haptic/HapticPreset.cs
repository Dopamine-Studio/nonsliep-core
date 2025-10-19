using UnityEngine;

namespace Nonsliep.Core.Effect
{
    public enum HapticKind
    {
        Click,
        Success,
        Warning,
        Error,
        ImpactLight,
        ImpactMedium,
        ImpactHeavy,
        LongLow,
        LongHigh,
    }

    [CreateAssetMenu(fileName = "HapticPreset", menuName = "FX/Haptic Preset", order = 3)]
    public class HapticPreset : ScriptableObject
    {
        public HapticKind kind = HapticKind.Click;
        [Range(0f, 1f)] public float intensity = 1f; // scaled by Settings
        public float duration = 0.02f; // seconds; may be approximated on mobile
    }
}

