using UnityEngine;
using UnityEngine.Audio;

namespace Nonsliep.Core.Effect
{
    // Runtime context passed when playing FX. Contains only
    // channel-specific sub-contexts to keep intent explicit.
    public readonly struct EffectContext
    {
        public readonly SFXContext? sfx; // audio-specific
        public readonly VFXContext? vfx; // vfx-specific

        public EffectContext(SFXContext? sfx = null, VFXContext? vfx = null)
        {
            this.sfx = sfx;
            this.vfx = vfx;
        }

        // Builders
        public static EffectContext ForSFX(Transform anchor = null, Vector3? position = null, AudioMixerGroup group = null, float? volumeMul = null, float? pitchMul = null)
            => new EffectContext(new SFXContext(anchor, position, group, volumeMul, pitchMul), null);
        public static EffectContext ForVFX(Transform anchor = null, Vector3? position = null)
            => new EffectContext(null, new VFXContext(anchor, position));
        public EffectContext WithSFX(Transform anchor = null, Vector3? pos = null, AudioMixerGroup group = null, float? volumeMul = null, float? pitchMul = null)
            => new EffectContext(new SFXContext(anchor, pos, group, volumeMul, pitchMul), this.vfx);
        public EffectContext WithVFX(Transform anchor = null, Vector3? pos = null)
            => new EffectContext(this.sfx, new VFXContext(anchor, pos));

        // Per-channel contexts
        public readonly struct SFXContext
        {
            public readonly Transform anchor;
            public readonly Vector3? position;
            public readonly AudioMixerGroup groupOverride;
            public readonly float? volumeMul; // if null -> 1
            public readonly float? pitchMul;  // if null -> 1

            public SFXContext(Transform anchor = null, Vector3? position = null, AudioMixerGroup groupOverride = null, float? volumeMul = null, float? pitchMul = null)
            {
                this.anchor = anchor;
                this.position = position;
                this.groupOverride = groupOverride;
                this.volumeMul = volumeMul;
                this.pitchMul = pitchMul;
            }
        }

        public readonly struct VFXContext
        {
            public readonly Transform anchor;
            public readonly Vector3? position;

            public VFXContext(Transform anchor = null, Vector3? position = null)
            {
                this.anchor = anchor;
                this.position = position;
            }
        }
    }
}
