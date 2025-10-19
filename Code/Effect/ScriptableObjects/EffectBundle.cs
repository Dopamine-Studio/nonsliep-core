using UnityEngine;
using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;


namespace Nonsliep.Core.Effect
{
    [CreateAssetMenu(fileName = "EffectBundle", menuName = "FX/Effect Bundle", order = 4)]
    public class EffectBundle : ScriptableObject
    {
        public SFXAsset audio;
        public VFXAsset vfx;
        public HapticPreset haptics;

        [Header("Overrides (optional)")]
        public bool overrideVolume = false;
        [Range(0f, 1f)] public float volume = 1f;
    }
}

